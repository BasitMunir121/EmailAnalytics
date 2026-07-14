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

	[HttpPost("generate-test-data")]
	public IActionResult GenerateTestData()
	{
		var logDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TrackingLogs");
		if (!Directory.Exists(logDirectory)) Directory.CreateDirectory(logDirectory);

		var logFile = Path.Combine(logDirectory, $"tracking_{DateTime.UtcNow:yyyyMMdd}.log");


		var testEvents = new[]
		{
            // TEST001
            new { Id = Guid.NewGuid().ToString(), EmailId = "TEST001", EventType = "open", LinkType = "", Timestamp = DateTime.UtcNow.AddHours(-2), EmailClient = "Gmail", SourceClient = "", Device = "Windows PC" },
			new { Id = Guid.NewGuid().ToString(), EmailId = "TEST001", EventType = "click", LinkType = "facebook", Timestamp = DateTime.UtcNow.AddHours(-1.9), EmailClient = "Chrome", SourceClient = "Gmail", Device = "Windows PC" },
			new { Id = Guid.NewGuid().ToString(), EmailId = "TEST001", EventType = "click", LinkType = "facebook", Timestamp = DateTime.UtcNow.AddHours(-1.8), EmailClient = "Chrome", SourceClient = "Gmail", Device = "Windows PC" },
            // TEST002
            new { Id = Guid.NewGuid().ToString(), EmailId = "TEST002", EventType = "open", LinkType = "", Timestamp = DateTime.UtcNow.AddHours(-5), EmailClient = "Outlook Classic", SourceClient = "", Device = "Windows PC" },
			new { Id = Guid.NewGuid().ToString(), EmailId = "TEST002", EventType = "open", LinkType = "", Timestamp = DateTime.UtcNow.AddHours(-4), EmailClient = "Outlook Classic", SourceClient = "", Device = "Windows PC" },
			new { Id = Guid.NewGuid().ToString(), EmailId = "TEST002", EventType = "click", LinkType = "linkedin", Timestamp = DateTime.UtcNow.AddHours(-3.5), EmailClient = "Edge", SourceClient = "Outlook Classic", Device = "Windows PC" },
            // TEST003
            new { Id = Guid.NewGuid().ToString(), EmailId = "TEST003", EventType = "open", LinkType = "", Timestamp = DateTime.UtcNow.AddHours(-1), EmailClient = "Apple Mail", SourceClient = "", Device = "Mac" },
			new { Id = Guid.NewGuid().ToString(), EmailId = "TEST003", EventType = "click", LinkType = "instagram", Timestamp = DateTime.UtcNow.AddHours(-0.5), EmailClient = "Safari", SourceClient = "Apple Mail", Device = "Mac" },
		};


		foreach (var evt in testEvents)
		{
			var entry = $"{{\"Id\":\"{evt.Id}\",\"EmailId\":\"{evt.EmailId}\",\"EventType\":\"{evt.EventType}\",\"LinkType\":\"{evt.LinkType}\",\"Timestamp\":\"{evt.Timestamp:yyyy-MM-ddTHH:mm:ssZ}\",\"UserAgent\":\"\",\"IpAddress\":\"127.0.0.1\",\"EmailClient\":\"{evt.EmailClient}\",\"SourceEmailClient\":\"{evt.SourceClient}\",\"DeviceType\":\"{evt.Device}\",\"OperatingSystem\":\"\",\"Country\":\"\",\"City\":\"\",\"SenderEmail\":\"test@example.com\"}}";
			System.IO.File.AppendAllText(logFile, entry + Environment.NewLine);
		}

		return Ok(new { message = "Test data generated with TEST001, TEST002, TEST003" });
	}
}