using ExSignTracker.Api.Models;
using System.Text.Json;

namespace ExSignTracker.Api.Services;

public interface IFileTrackingService
{
	Task<TrackingResult> TrackOpenAsync(string trackingId, HttpRequest request, string senderEmail = "");
	Task<TrackingResult> TrackClickAsync(string trackingId, string linkType, string destinationUrl, HttpRequest request, string senderEmail = "");
	List<GroupedTrackingData> GetAllTrackingData();
	void ClearAllData();
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
	public List<OpenDetail> Opens { get; set; } = new();
	public List<ClickDetail> Clicks { get; set; } = new();
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

public class FileTrackingService : IFileTrackingService
{
	private readonly string _logDirectory;
	private static readonly object _lockObject = new();

	public FileTrackingService()
	{
		_logDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TrackingLogs");
		if (!Directory.Exists(_logDirectory)) Directory.CreateDirectory(_logDirectory);
	}

	public async Task<TrackingResult> TrackOpenAsync(string trackingId, HttpRequest request, string senderEmail = "")
	{
		try
		{
			var userAgent = request.Headers["User-Agent"].ToString();
			var (emailClient, deviceType, os) = ParseUserAgent(userAgent, request);

			var trackingEvent = new TrackingEvent
			{
				EmailId = trackingId,
				EventType = "open",
				Timestamp = DateTime.UtcNow,
				UserAgent = userAgent,
				IpAddress = GetClientIp(request),
				EmailClient = emailClient,
				DeviceType = deviceType,
				OperatingSystem = os,
				SenderEmail = senderEmail
			};

			await WriteToLogFile(trackingEvent);
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
			var sourceClient = DetectSourceClient(request); // Pass the full request, not just userAgent and referer

			var trackingEvent = new TrackingEvent
			{
				EmailId = trackingId,
				EventType = "click",
				LinkType = linkType,
				Timestamp = DateTime.UtcNow,
				UserAgent = userAgent,
				IpAddress = GetClientIp(request),
				EmailClient = emailClient,
				SourceEmailClient = sourceClient,
				DeviceType = deviceType,
				OperatingSystem = os,
				SenderEmail = senderEmail
			};

			await WriteToLogFile(trackingEvent);
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
		var allEvents = new List<TrackingEvent>();

		if (Directory.Exists(_logDirectory))
		{
			var logFiles = Directory.GetFiles(_logDirectory, "tracking_*.log");
			foreach (var logFile in logFiles)
			{
				var lines = File.ReadAllLines(logFile);
				foreach (var line in lines)
				{
					try
					{
						var eventData = JsonSerializer.Deserialize<TrackingEvent>(line);
						if (eventData != null) allEvents.Add(eventData);
					}
					catch { }
				}
			}
		}

		var result = allEvents
			.GroupBy(e => e.EmailId)
			.Select(g => new GroupedTrackingData
			{
				TrackingId = g.Key,
				Opens = g.Where(e => e.EventType == "open").Select(o => new OpenDetail
				{
					Timestamp = o.Timestamp,
					EmailClient = o.EmailClient,
					DeviceType = o.DeviceType
				}).ToList(),
				Clicks = g.Where(e => e.EventType == "click").Select(c => new ClickDetail
				{
					Timestamp = c.Timestamp,
					LinkType = c.LinkType,
					Browser = c.EmailClient,
					SourceClient = c.SourceEmailClient
				}).ToList()
			})
			.OrderByDescending(g => g.Opens.Count + g.Clicks.Count)
			.ToList();

		return result;
	}

	public void ClearAllData()
	{
		if (Directory.Exists(_logDirectory))
		{
			var logFiles = Directory.GetFiles(_logDirectory, "*.log");
			foreach (var file in logFiles) File.Delete(file);
		}
	}

	// Debug method for testing User-Agent parsing
	public (string EmailClient, string DeviceType, string OS) ParseUserAgentForDebug(string userAgent, HttpRequest request)
	{
		return ParseUserAgent(userAgent, request);
	}
	public string DetectSourceClientForDebug(HttpRequest request)
	{
		return DetectSourceClient(request);
	}
	private async Task WriteToLogFile(TrackingEvent trackingEvent)
	{
		var logFileName = $"tracking_{DateTime.UtcNow:yyyyMMdd}.log";
		var logFilePath = Path.Combine(_logDirectory, logFileName);
		var jsonLine = JsonSerializer.Serialize(trackingEvent);

		lock (_lockObject)
		{
			File.AppendAllText(logFilePath, jsonLine + Environment.NewLine);
		}
		await Task.CompletedTask;
	}

	private (string EmailClient, string DeviceType, string OS) ParseUserAgent(string ua, HttpRequest? request = null)
	{
		string client = "Unknown", device = "Unknown", os = "Unknown";

		if (string.IsNullOrEmpty(ua)) return (client, device, os);

		var uaLower = ua.ToLower();

		// ========================================
		// FIRST: Check for Outlook New via x-native-host header
		// This is the most reliable way to detect Outlook New
		// ========================================
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

		// ========================================
		// DETECT OUTLOOK CLASSIC
		// ========================================
		if (uaLower.Contains("ms-office") ||
			uaLower.Contains("msoffice") ||
			uaLower.Contains("compatible; ms-office"))
		{
			client = "Outlook Classic";
			device = "Windows PC";
			os = "Windows";
			return (client, device, os);
		}

		// ========================================
		// DETECT OTHER OUTLOOK VARIANTS
		// ========================================
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

		// ========================================
		// DETECT OTHER EMAIL CLIENTS
		// ========================================
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
		// DETECT BROWSERS (for clicks)
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

		// ========================================
		// DETECT DEVICE
		// ========================================
		if (device == "Unknown")
		{
			if (uaLower.Contains("iphone")) device = "iPhone";
			else if (uaLower.Contains("ipad")) device = "iPad";
			else if (uaLower.Contains("android")) device = "Android";
			else if (uaLower.Contains("windows")) device = "Windows PC";
			else if (uaLower.Contains("macintosh") || uaLower.Contains("mac os")) device = "Mac";
		}

		// ========================================
		// DETECT OS
		// ========================================
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

		// Method 1: Check x-native-host header (Outlook New)
		var nativeHost = request.Headers["x-native-host"].ToString();
		if (!string.IsNullOrEmpty(nativeHost))
		{
			if (nativeHost.Contains("Outlook", StringComparison.OrdinalIgnoreCase))
				return "Outlook New (Microsoft 365)";
		}

		// Method 2: Check User-Agent
		var userAgent = request.Headers["User-Agent"].ToString();
		if (string.IsNullOrEmpty(userAgent)) return "Unknown";

		var ua = userAgent.ToLower();

		// Outlook Classic
		if (ua.Contains("ms-office") || ua.Contains("msoffice") || ua.Contains("compatible; ms-office"))
		{
			return "Outlook Classic";
		}

		// Outlook for Mac
		if (ua.Contains("outlook for mac"))
		{
			return "Outlook for Mac";
		}

		// Outlook Mobile
		if (ua.Contains("outlook-ios"))
		{
			return "Outlook Mobile (iOS)";
		}
		if (ua.Contains("outlook-android"))
		{
			return "Outlook Mobile (Android)";
		}

		// Method 3: Check Referer for webmail
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