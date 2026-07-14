using ExSignTracker.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace ExSignTracker.Api.Controllers;

[ApiController]
public class TrackingController : ControllerBase
{
	private readonly IFileTrackingService _trackingService;

	public TrackingController(IFileTrackingService trackingService)
	{
		_trackingService = trackingService;
	}

	[HttpGet("/t/{trackingId}.gif")]
	public async Task<IActionResult> TrackOpen(string trackingId, [FromQuery] string? sender = null)
	{
		await _trackingService.TrackOpenAsync(trackingId, Request, sender ?? "");
		var pixel = Convert.FromBase64String("R0lGODlhAQABAIAAAAAAAP///yH5BAEAAAAALAAAAAABAAEAAAIBRAA7");
		return File(pixel, "image/gif");
	}

	[HttpGet("/r/{trackingId}/{linkType}")]
	public async Task<IActionResult> TrackClick(string trackingId, string linkType, [FromQuery] string url, [FromQuery] string? sender = null)
	{
		if (string.IsNullOrEmpty(url)) return BadRequest("Missing URL");
		var decodedUrl = Uri.UnescapeDataString(url);
		var result = await _trackingService.TrackClickAsync(trackingId, linkType, decodedUrl, Request, sender ?? "");
		return Redirect(result.RedirectUrl ?? decodedUrl);
	}

	// DEBUG ENDPOINT - Add this to see what User-Agent Outlook is sending
	[HttpGet("/api/Tracking/debug-useragent")]
	public IActionResult DebugOpenUserAgent()
	{
		var userAgent = Request.Headers["User-Agent"].ToString();
		var nativeHost = Request.Headers["x-native-host"].ToString();
		var referer = Request.Headers["Referer"].ToString();
		var allHeaders = Request.Headers.ToDictionary(h => h.Key, h => h.Value.ToString());

		string detectedClient = "Unknown";
		string detectedDevice = "Unknown";
		string detectedOS = "Unknown";

		try
		{
			var service = new FileTrackingService();
			var result = service.ParseUserAgentForDebug(userAgent, Request);
			detectedClient = result.EmailClient;
			detectedDevice = result.DeviceType;
			detectedOS = result.OS;
		}
		catch (Exception ex)
		{
			detectedClient = $"Error: {ex.Message}";
		}

		Console.WriteLine($"=== OPEN DEBUG ===");
		Console.WriteLine($"User-Agent: {userAgent}");
		Console.WriteLine($"x-native-host: {nativeHost}");
		Console.WriteLine($"Detected: {detectedClient}");

		var pixel = Convert.FromBase64String("R0lGODlhAQABAIAAAAAAAP///yH5BAEAAAAALAAAAAABAAEAAAIBRAA7");
		return File(pixel, "image/gif");
	}
	[HttpGet("/api/debug/source-detection")]
	public IActionResult DebugSourceDetection()
	{
		var nativeHost = Request.Headers["x-native-host"].ToString();
		var userAgent = Request.Headers["User-Agent"].ToString();
		var referer = Request.Headers["Referer"].ToString();

		string detectedSource = "Unknown";
		try
		{
			var service = new FileTrackingService();
			detectedSource = service.DetectSourceClientForDebug(Request);
		}
		catch (Exception ex)
		{
			detectedSource = $"Error: {ex.Message}";
		}

		return Ok(new
		{
			timestamp = DateTime.UtcNow,
			nativeHost = nativeHost,
			userAgent = userAgent,
			referer = referer,
			detectedSource = detectedSource
		});
	}
}