using System.Net.Http.Json;
using System.Text.Json;

namespace ExSignAnalytics.Web.Services;

public interface IAnalyticsApiClient
{
	Task<string> GetAllTrackingDataAsync(CancellationToken cancellationToken = default);
	Task<string> ClearAllDataAsync(CancellationToken cancellationToken = default);
	Task<SurveyResponseInfo?> GetSurveyByTokenAsync(Guid token, CancellationToken cancellationToken = default);
	Task<bool> SaveSurveyFeedbackAsync(Guid token, string comment, CancellationToken cancellationToken = default);
}

public class SurveyResponseInfo
{
	public Guid ResponseToken { get; set; }
	public string TrackingKey { get; set; } = string.Empty;
	public string SurveyType { get; set; } = string.Empty;
	public int? Score { get; set; }
	public string? ChoiceKey { get; set; }
	public string? Comment { get; set; }
	public DateTime Timestamp { get; set; }
}

public class AnalyticsApiClient : IAnalyticsApiClient
{
	private static readonly JsonSerializerOptions JsonOptions = new()
	{
		PropertyNameCaseInsensitive = true
	};

	private readonly HttpClient _httpClient;

	public AnalyticsApiClient(HttpClient httpClient)
	{
		_httpClient = httpClient;
	}

	public async Task<string> GetAllTrackingDataAsync(CancellationToken cancellationToken = default)
	{
		var response = await _httpClient.GetAsync("api/Stats/all-tracking-data", cancellationToken);
		response.EnsureSuccessStatusCode();
		return await response.Content.ReadAsStringAsync(cancellationToken);
	}

	public async Task<string> ClearAllDataAsync(CancellationToken cancellationToken = default)
	{
		var response = await _httpClient.PostAsync("api/Stats/clear-all-data", null, cancellationToken);
		response.EnsureSuccessStatusCode();
		return await response.Content.ReadAsStringAsync(cancellationToken);
	}

	public async Task<SurveyResponseInfo?> GetSurveyByTokenAsync(Guid token, CancellationToken cancellationToken = default)
	{
		var response = await _httpClient.GetAsync($"api/Survey/{token:D}", cancellationToken);
		if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
		response.EnsureSuccessStatusCode();
		var json = await response.Content.ReadAsStringAsync(cancellationToken);
		return JsonSerializer.Deserialize<SurveyResponseInfo>(json, JsonOptions);
	}

	public async Task<bool> SaveSurveyFeedbackAsync(Guid token, string comment, CancellationToken cancellationToken = default)
	{
		var response = await _httpClient.PostAsJsonAsync(
			"api/Survey/feedback",
			new { token, comment },
			cancellationToken);
		return response.IsSuccessStatusCode;
	}
}
