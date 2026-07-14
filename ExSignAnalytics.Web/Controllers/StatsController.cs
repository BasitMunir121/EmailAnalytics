using ExSignAnalytics.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace ExSignAnalytics.Web.Controllers;

[Route("api/[controller]")]
public class StatsController : ControllerBase
{
	private readonly IAnalyticsApiClient _apiClient;

	public StatsController(IAnalyticsApiClient apiClient)
	{
		_apiClient = apiClient;
	}

	[HttpGet("all-tracking-data")]
	public async Task<IActionResult> GetAllTrackingData(CancellationToken cancellationToken)
	{
		try
		{
			var json = await _apiClient.GetAllTrackingDataAsync(cancellationToken);
			return Content(json, "application/json");
		}
		catch (Exception ex)
		{
			return StatusCode(StatusCodes.Status502BadGateway, new { error = "Failed to reach Api", detail = ex.Message });
		}
	}

	[HttpPost("clear-all-data")]
	public async Task<IActionResult> ClearAllData(CancellationToken cancellationToken)
	{
		try
		{
			var json = await _apiClient.ClearAllDataAsync(cancellationToken);
			return Content(json, "application/json");
		}
		catch (Exception ex)
		{
			return StatusCode(StatusCodes.Status502BadGateway, new { error = "Failed to reach Api", detail = ex.Message });
		}
	}

	[HttpPost("generate-test-data")]
	public async Task<IActionResult> GenerateTestData(CancellationToken cancellationToken)
	{
		try
		{
			var json = await _apiClient.GenerateTestDataAsync(cancellationToken);
			return Content(json, "application/json");
		}
		catch (Exception ex)
		{
			return StatusCode(StatusCodes.Status502BadGateway, new { error = "Failed to reach Api", detail = ex.Message });
		}
	}
}
