namespace ExSignAnalytics.Web.Services;

public interface IAnalyticsApiClient
{
	Task<string> GetAllTrackingDataAsync(CancellationToken cancellationToken = default);
	Task<string> ClearAllDataAsync(CancellationToken cancellationToken = default);
	Task<string> GenerateTestDataAsync(CancellationToken cancellationToken = default);
}
