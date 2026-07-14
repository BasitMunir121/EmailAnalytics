namespace ExSignAnalytics.Api.Models;

public class Open
{
	public int Id { get; set; }
	public int TrackingId { get; set; }
	public DateTime Timestamp { get; set; } = DateTime.UtcNow;
	public string UserAgent { get; set; } = string.Empty;
	public string IpAddress { get; set; } = string.Empty;
	public string? EmailClient { get; set; }
	public string? DeviceType { get; set; }
	public string? OperatingSystem { get; set; }
	public string? Country { get; set; }
	public string? City { get; set; }

	public Tracking Tracking { get; set; } = null!;
}
