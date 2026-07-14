using ExSignAnalytics.Api.Data;
using ExSignAnalytics.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace ExSignAnalytics.Api.Services;

public interface IFileTrackingService
{
	Task<TrackingResult> TrackOpenAsync(string trackingId, HttpRequest request, string senderEmail = "");
	Task<TrackingResult> TrackClickAsync(string trackingId, string linkType, string destinationUrl, HttpRequest request, string senderEmail = "");
	List<GroupedTrackingData> GetAllTrackingData();
	void ClearAllData();
	(string EmailClient, string DeviceType, string OS) ParseUserAgentForDebug(string userAgent, HttpRequest request);
	string DetectSourceClientForDebug(HttpRequest request);
}

public class TrackingResult
{
	public bool Success { get; set; }
	public string? RedirectUrl { get; set; }
	public string? Error { get; set; }
}

public class GroupedTrackingData
{
	public string TrackingId { get; set; } = string.Empty;
	public bool HasSignature { get; set; }
	public bool HasSurvey { get; set; }
	public List<OpenDetail> Opens { get; set; } = new();
	public List<ClickDetail> Clicks { get; set; } = new();
	public List<SurveyDetail> Surveys { get; set; } = new();
}

public class OpenDetail
{
	public DateTime Timestamp { get; set; }
	public string? EmailClient { get; set; }
	public string? DeviceType { get; set; }
}

public class ClickDetail
{
	public DateTime Timestamp { get; set; }
	public string? LinkType { get; set; }
	public string? Browser { get; set; }
	public string? SourceClient { get; set; }
}

public class SurveyDetail
{
	public DateTime Timestamp { get; set; }
	public string SurveyType { get; set; } = string.Empty;
	public int? Score { get; set; }
	public string? ChoiceKey { get; set; }
	public string? Comment { get; set; }
}

public class FileTrackingService : IFileTrackingService
{
	private readonly TrackingDbContext _db;

	public FileTrackingService(TrackingDbContext db)
	{
		_db = db;
	}

	public async Task<TrackingResult> TrackOpenAsync(string trackingId, HttpRequest request, string senderEmail = "")
	{
		try
		{
			var userAgent = request.Headers["User-Agent"].ToString();
			var (emailClient, deviceType, os) = ParseUserAgent(userAgent, request);
			var tracking = await GetOrCreateTrackingAsync(trackingId, senderEmail);

			_db.Opens.Add(new Open
			{
				TrackingId = tracking.Id,
				Timestamp = DateTime.UtcNow,
				UserAgent = userAgent,
				IpAddress = GetClientIp(request),
				EmailClient = emailClient,
				DeviceType = deviceType,
				OperatingSystem = os
			});
			await _db.SaveChangesAsync();

			Console.WriteLine($"[OPEN] {trackingId} | Client: {emailClient} | Device: {deviceType}");
			return new TrackingResult { Success = true };
		}
		catch (Exception ex)
		{
			Console.WriteLine($"[ERROR] {ex.Message}");
			return new TrackingResult { Success = false, Error = ex.Message };
		}
	}

	public async Task<TrackingResult> TrackClickAsync(string trackingId, string linkType, string destinationUrl, HttpRequest request, string senderEmail = "")
	{
		try
		{
			var userAgent = request.Headers["User-Agent"].ToString();
			var (emailClient, deviceType, os) = ParseUserAgent(userAgent, request);
			var sourceClient = DetectSourceClient(request);
			var tracking = await GetOrCreateTrackingAsync(trackingId, senderEmail);

			_db.Clicks.Add(new Click
			{
				TrackingId = tracking.Id,
				LinkType = linkType,
				Timestamp = DateTime.UtcNow,
				UserAgent = userAgent,
				IpAddress = GetClientIp(request),
				Browser = emailClient,
				SourceEmailClient = sourceClient,
				DeviceType = deviceType,
				OperatingSystem = os
			});
			await _db.SaveChangesAsync();

			Console.WriteLine($"[CLICK] {trackingId} | Link: {linkType} | Browser: {emailClient} | From: {sourceClient}");
			return new TrackingResult { Success = true, RedirectUrl = destinationUrl };
		}
		catch (Exception ex)
		{
			return new TrackingResult { Success = false, Error = ex.Message, RedirectUrl = destinationUrl };
		}
	}

	public List<GroupedTrackingData> GetAllTrackingData()
	{
		return _db.Trackings
			.AsNoTracking()
			.Include(t => t.Opens)
			.Include(t => t.Clicks)
			.Include(t => t.SurveyResponses)
			.AsEnumerable()
			.Select(t => new GroupedTrackingData
			{
				TrackingId = t.TrackingKey,
				HasSignature = t.HasSignature,
				HasSurvey = t.HasSurvey,
				Opens = t.Opens
					.OrderBy(o => o.Timestamp)
					.Select(o => new OpenDetail
					{
						Timestamp = o.Timestamp,
						EmailClient = o.EmailClient,
						DeviceType = o.DeviceType
					}).ToList(),
				Clicks = t.Clicks
					.OrderBy(c => c.Timestamp)
					.Select(c => new ClickDetail
					{
						Timestamp = c.Timestamp,
						LinkType = c.LinkType,
						Browser = c.Browser,
						SourceClient = c.SourceEmailClient
					}).ToList(),
				Surveys = t.SurveyResponses
					.OrderBy(s => s.Timestamp)
					.Select(s => new SurveyDetail
					{
						Timestamp = s.Timestamp,
						SurveyType = s.SurveyType,
						Score = s.Score,
						ChoiceKey = s.ChoiceKey,
						Comment = s.Comment
					}).ToList()
			})
			.OrderByDescending(g => g.Opens.Count + g.Clicks.Count + g.Surveys.Count)
			.ToList();
	}

	public void ClearAllData()
	{
		_db.SurveyResponses.ExecuteDelete();
		_db.Clicks.ExecuteDelete();
		_db.Opens.ExecuteDelete();
		_db.Trackings.ExecuteDelete();
	}

	public (string EmailClient, string DeviceType, string OS) ParseUserAgentForDebug(string userAgent, HttpRequest request)
	{
		return ParseUserAgent(userAgent, request);
	}

	public string DetectSourceClientForDebug(HttpRequest request)
	{
		return DetectSourceClient(request);
	}

	private async Task<Tracking> GetOrCreateTrackingAsync(string trackingKey, string senderEmail)
	{
		var tracking = await _db.Trackings.FirstOrDefaultAsync(t => t.TrackingKey == trackingKey);
		if (tracking != null)
		{
			if (!string.IsNullOrWhiteSpace(senderEmail) && string.IsNullOrWhiteSpace(tracking.SenderEmail))
			{
				tracking.SenderEmail = senderEmail;
				await _db.SaveChangesAsync();
			}
			return tracking;
		}

		tracking = new Tracking
		{
			TrackingKey = trackingKey,
			SenderEmail = senderEmail ?? string.Empty,
			CreatedAt = DateTime.UtcNow
		};
		_db.Trackings.Add(tracking);
		await _db.SaveChangesAsync();
		return tracking;
	}

	private (string EmailClient, string DeviceType, string OS) ParseUserAgent(string ua, HttpRequest? request = null)
	{
		string client = "Unknown", device = "Unknown", os = "Unknown";

		if (string.IsNullOrEmpty(ua)) return (client, device, os);

		var uaLower = ua.ToLower();

		if (request != null)
		{
			var nativeHost = request.Headers["x-native-host"].ToString();
			if (!string.IsNullOrEmpty(nativeHost) && nativeHost.Contains("Outlook", StringComparison.OrdinalIgnoreCase))
			{
				client = "Outlook New (Microsoft 365)";
				device = "Windows PC";
				os = "Windows 10/11";
				return (client, device, os);
			}
		}

		if (uaLower.Contains("ms-office") ||
			uaLower.Contains("msoffice") ||
			uaLower.Contains("compatible; ms-office"))
		{
			client = "Outlook Classic";
			device = "Windows PC";
			os = "Windows";
			return (client, device, os);
		}

		if (uaLower.Contains("outlook for mac"))
		{
			client = "Outlook for Mac";
			device = "Mac";
			os = "macOS";
			return (client, device, os);
		}

		if (uaLower.Contains("outlook-ios"))
		{
			client = "Outlook Mobile (iOS)";
			device = "iPhone";
			os = "iOS";
			return (client, device, os);
		}

		if (uaLower.Contains("outlook-android"))
		{
			client = "Outlook Mobile (Android)";
			device = "Android";
			os = "Android";
			return (client, device, os);
		}

		if (uaLower.Contains("gmail"))
		{
			client = "Gmail";
		}
		else if (uaLower.Contains("applemail") || uaLower.Contains("mac os x mail"))
		{
			client = "Apple Mail";
		}
		else if (uaLower.Contains("thunderbird"))
		{
			client = "Thunderbird";
		}
		else if (uaLower.Contains("chrome/") && !uaLower.Contains("edg/"))
		{
			client = "Chrome Browser";
		}
		else if (uaLower.Contains("firefox/"))
		{
			client = "Firefox Browser";
		}
		else if (uaLower.Contains("safari/") && !uaLower.Contains("chrome/"))
		{
			client = "Safari Browser";
		}
		else if (uaLower.Contains("edg/"))
		{
			client = "Edge Browser";
		}

		if (device == "Unknown")
		{
			if (uaLower.Contains("iphone")) device = "iPhone";
			else if (uaLower.Contains("ipad")) device = "iPad";
			else if (uaLower.Contains("android")) device = "Android";
			else if (uaLower.Contains("windows")) device = "Windows PC";
			else if (uaLower.Contains("macintosh") || uaLower.Contains("mac os")) device = "Mac";
		}

		if (os == "Unknown")
		{
			if (uaLower.Contains("windows nt 10.0")) os = "Windows 10/11";
			else if (uaLower.Contains("windows nt")) os = "Windows";
			else if (uaLower.Contains("mac os x")) os = "macOS";
			else if (uaLower.Contains("iphone os")) os = "iOS";
			else if (uaLower.Contains("android")) os = "Android";
		}

		return (client, device, os);
	}

	private string DetectSourceClient(HttpRequest request)
	{
		if (request == null) return "Unknown";

		var nativeHost = request.Headers["x-native-host"].ToString();
		if (!string.IsNullOrEmpty(nativeHost))
		{
			if (nativeHost.Contains("Outlook", StringComparison.OrdinalIgnoreCase))
				return "Outlook New (Microsoft 365)";
		}

		var userAgent = request.Headers["User-Agent"].ToString();
		if (string.IsNullOrEmpty(userAgent)) return "Unknown";

		var ua = userAgent.ToLower();

		if (ua.Contains("ms-office") || ua.Contains("msoffice") || ua.Contains("compatible; ms-office"))
		{
			return "Outlook Classic";
		}

		if (ua.Contains("outlook for mac"))
		{
			return "Outlook for Mac";
		}

		if (ua.Contains("outlook-ios"))
		{
			return "Outlook Mobile (iOS)";
		}
		if (ua.Contains("outlook-android"))
		{
			return "Outlook Mobile (Android)";
		}

		var referer = request.Headers["Referer"].ToString();
		if (!string.IsNullOrEmpty(referer))
		{
			var refLower = referer.ToLower();
			if (refLower.Contains("outlook.office.com"))
				return "Outlook Web App";
			if (refLower.Contains("mail.google.com"))
				return "Gmail Web";
			if (refLower.Contains("yahoo.com"))
				return "Yahoo Mail";
			if (refLower.Contains("mail.proton.me"))
				return "Proton Mail";
		}

		return "Unknown";
	}

	private string GetClientIp(HttpRequest request)
	{
		var forwarded = request.Headers["X-Forwarded-For"].ToString();
		if (!string.IsNullOrEmpty(forwarded)) return forwarded.Split(',')[0].Trim();
		return request.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
	}
}
