namespace ExSignAnalytics.Web.Services;

public class AnalyticsApiClient : IAnalyticsApiClient
{
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
}
