using ExSignAnalytics.Api.Data;
using ExSignAnalytics.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace ExSignAnalytics.Api.Services;

public interface ISurveyService
{
	Task<SurveyClickResult> RecordSurveyClickAsync(
		string trackingKey,
		string surveyType,
		string value,
		HttpRequest request,
		string senderEmail = "",
		string recipientEmail = "");

	Task<SurveyResponseDto?> GetByTokenAsync(Guid token);
	Task<bool> SaveCommentAsync(Guid token, string comment);
}

public class SurveyClickResult
{
	public bool Success { get; set; }
	public string? Error { get; set; }
	public Guid? ResponseToken { get; set; }
	public string? FeedbackUrl { get; set; }
}

public class SurveyResponseDto
{
	public Guid ResponseToken { get; set; }
	public string TrackingKey { get; set; } = string.Empty;
	public string SurveyType { get; set; } = string.Empty;
	public int? Score { get; set; }
	public string? ChoiceKey { get; set; }
	public string? Comment { get; set; }
	public DateTime Timestamp { get; set; }
}

public class SurveyService : ISurveyService
{
	private static readonly Dictionary<string, int> EmojiScores = new(StringComparer.OrdinalIgnoreCase)
	{
		["angry"] = 1,
		["sad"] = 2,
		["neutral"] = 3,
		["happy"] = 4,
		["love"] = 5
	};

	private readonly TrackingDbContext _db;
	private readonly IConfiguration _configuration;

	public SurveyService(TrackingDbContext db, IConfiguration configuration)
	{
		_db = db;
		_configuration = configuration;
	}

	public async Task<SurveyClickResult> RecordSurveyClickAsync(
		string trackingKey,
		string surveyType,
		string value,
		HttpRequest request,
		string senderEmail = "",
		string recipientEmail = "")
	{
		try
		{
			surveyType = (surveyType ?? "").Trim().ToLowerInvariant();
			value = (value ?? "").Trim();

			if (string.IsNullOrWhiteSpace(trackingKey))
				return new SurveyClickResult { Success = false, Error = "Missing tracking key" };

			if (!TryParseSurveyValue(surveyType, value, out var score, out var choiceKey, out var error))
				return new SurveyClickResult { Success = false, Error = error };

			var survey = await _db.Surveys.FirstOrDefaultAsync(s => s.SurveyType == surveyType && s.IsActive);
			if (survey == null)
				return new SurveyClickResult { Success = false, Error = $"Unknown survey type '{surveyType}'" };

			var tracking = await GetOrCreateTrackingForSurveyAsync(trackingKey, senderEmail, recipientEmail, survey.Id);
			var userAgent = request.Headers["User-Agent"].ToString();
			var (browser, deviceType, os) = ParseBasicUserAgent(userAgent);

			var response = new SurveyResponse
			{
				TrackingId = tracking.Id,
				SurveyType = surveyType,
				Score = score,
				ChoiceKey = choiceKey,
				ResponseToken = Guid.NewGuid(),
				Timestamp = DateTime.UtcNow,
				UserAgent = userAgent,
				IpAddress = GetClientIp(request),
				Browser = browser,
				DeviceType = deviceType,
				OperatingSystem = os,
				SenderEmail = senderEmail ?? string.Empty,
				RecipientEmail = string.IsNullOrWhiteSpace(recipientEmail) ? null : recipientEmail
			};

			_db.SurveyResponses.Add(response);
			await _db.SaveChangesAsync();

			var feedbackBase = (_configuration["FeedbackBaseUrl"] ?? "https://localhost:7263").TrimEnd('/');
			var feedbackUrl = $"{feedbackBase}/Feedback?token={response.ResponseToken:D}";

			Console.WriteLine($"[SURVEY] {trackingKey} | {surveyType}={choiceKey ?? score?.ToString()} | Token={response.ResponseToken}");

			return new SurveyClickResult
			{
				Success = true,
				ResponseToken = response.ResponseToken,
				FeedbackUrl = feedbackUrl
			};
		}
		catch (Exception ex)
		{
			Console.WriteLine($"[SURVEY ERROR] {ex.Message}");
			return new SurveyClickResult { Success = false, Error = ex.Message };
		}
	}

	public async Task<SurveyResponseDto?> GetByTokenAsync(Guid token)
	{
		var response = await _db.SurveyResponses
			.AsNoTracking()
			.Include(r => r.Tracking)
			.FirstOrDefaultAsync(r => r.ResponseToken == token);

		if (response == null) return null;

		return new SurveyResponseDto
		{
			ResponseToken = response.ResponseToken,
			TrackingKey = response.Tracking.TrackingKey,
			SurveyType = response.SurveyType,
			Score = response.Score,
			ChoiceKey = response.ChoiceKey,
			Comment = response.Comment,
			Timestamp = response.Timestamp
		};
	}

	public async Task<bool> SaveCommentAsync(Guid token, string comment)
	{
		var response = await _db.SurveyResponses.FirstOrDefaultAsync(r => r.ResponseToken == token);
		if (response == null) return false;

		response.Comment = string.IsNullOrWhiteSpace(comment) ? null : comment.Trim();
		response.CommentedAt = DateTime.UtcNow;
		await _db.SaveChangesAsync();
		return true;
	}

	private async Task<Tracking> GetOrCreateTrackingForSurveyAsync(
		string trackingKey,
		string senderEmail,
		string recipientEmail,
		int surveyId)
	{
		var tracking = await _db.Trackings.FirstOrDefaultAsync(t => t.TrackingKey == trackingKey);
		if (tracking != null)
		{
			tracking.HasSurvey = true;
			tracking.SurveyId ??= surveyId;
			if (!string.IsNullOrWhiteSpace(senderEmail) && string.IsNullOrWhiteSpace(tracking.SenderEmail))
				tracking.SenderEmail = senderEmail;
			if (!string.IsNullOrWhiteSpace(recipientEmail) && string.IsNullOrWhiteSpace(tracking.RecipientEmail))
				tracking.RecipientEmail = recipientEmail;
			await _db.SaveChangesAsync();
			return tracking;
		}

		tracking = new Tracking
		{
			TrackingKey = trackingKey,
			SenderEmail = senderEmail ?? string.Empty,
			RecipientEmail = string.IsNullOrWhiteSpace(recipientEmail) ? null : recipientEmail,
			CreatedAt = DateTime.UtcNow,
			HasSignature = false,
			HasSurvey = true,
			SurveyId = surveyId
		};
		_db.Trackings.Add(tracking);
		await _db.SaveChangesAsync();
		return tracking;
	}

	private static bool TryParseSurveyValue(
		string surveyType,
		string value,
		out int? score,
		out string? choiceKey,
		out string error)
	{
		score = null;
		choiceKey = null;
		error = string.Empty;

		if (string.IsNullOrWhiteSpace(value))
		{
			error = "Missing survey value";
			return false;
		}

		switch (surveyType)
		{
			case "stars":
				if (!int.TryParse(value, out var stars) || stars < 1 || stars > 5)
				{
					error = "Stars value must be 1-5";
					return false;
				}
				score = stars;
				choiceKey = stars.ToString();
				return true;

			case "scale":
				if (!int.TryParse(value, out var scale) || scale < 1 || scale > 10)
				{
					error = "Scale value must be 1-10";
					return false;
				}
				score = scale;
				choiceKey = scale.ToString();
				return true;

			case "emoji":
				if (!EmojiScores.TryGetValue(value, out var emojiScore))
				{
					error = "Emoji value must be angry, sad, neutral, happy, or love";
					return false;
				}
				score = emojiScore;
				choiceKey = value.ToLowerInvariant();
				return true;

			default:
				error = "Survey type must be stars, scale, or emoji";
				return false;
		}
	}

	private static (string Browser, string DeviceType, string OS) ParseBasicUserAgent(string ua)
	{
		string browser = "Unknown", device = "Unknown", os = "Unknown";
		if (string.IsNullOrEmpty(ua)) return (browser, device, os);

		var uaLower = ua.ToLowerInvariant();
		if (uaLower.Contains("edg/")) browser = "Edge";
		else if (uaLower.Contains("chrome/")) browser = "Chrome";
		else if (uaLower.Contains("firefox/")) browser = "Firefox";
		else if (uaLower.Contains("safari/")) browser = "Safari";

		if (uaLower.Contains("iphone")) device = "iPhone";
		else if (uaLower.Contains("ipad")) device = "iPad";
		else if (uaLower.Contains("android")) device = "Android";
		else if (uaLower.Contains("windows")) device = "Windows PC";
		else if (uaLower.Contains("mac")) device = "Mac";

		if (uaLower.Contains("windows")) os = "Windows";
		else if (uaLower.Contains("mac os")) os = "macOS";
		else if (uaLower.Contains("android")) os = "Android";
		else if (uaLower.Contains("iphone") || uaLower.Contains("ipad")) os = "iOS";

		return (browser, device, os);
	}

	private static string GetClientIp(HttpRequest request)
	{
		var forwarded = request.Headers["X-Forwarded-For"].ToString();
		if (!string.IsNullOrEmpty(forwarded)) return forwarded.Split(',')[0].Trim();
		return request.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
	}
}
