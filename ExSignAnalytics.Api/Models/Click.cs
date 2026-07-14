namespace ExSignAnalytics.Api.Models;

public class Click
{
	public int Id { get; set; }
	public int TrackingId { get; set; }
	public string LinkType { get; set; } = string.Empty;
	public DateTime Timestamp { get; set; } = DateTime.UtcNow;
	public string UserAgent { get; set; } = string.Empty;
	public string IpAddress { get; set; } = string.Empty;
	public string? Browser { get; set; }
	public string? SourceEmailClient { get; set; }
	public string? DeviceType { get; set; }
	public string? OperatingSystem { get; set; }
	public string? Country { get; set; }
	public string? City { get; set; }

	public Tracking Tracking { get; set; } = null!;
}
