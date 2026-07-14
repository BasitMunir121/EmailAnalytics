using ExSignAnalytics.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace ExSignAnalytics.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StatsController : ControllerBase
{
	private readonly IFileTrackingService _trackingService;

	public StatsController(IFileTrackingService trackingService)
	{
		_trackingService = trackingService;
	}

	[HttpGet("all-tracking-data")]
	public IActionResult GetAllTrackingData()
	{
		var data = _trackingService.GetAllTrackingData();
		return Ok(data);
	}

	[HttpPost("clear-all-data")]
	public IActionResult ClearAllData()
	{
		_trackingService.ClearAllData();
		return Ok(new { message = "All data cleared successfully" });
	}
}
