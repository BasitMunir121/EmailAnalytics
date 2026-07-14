namespace ExSignTracker.Api.Models;

public class TrackingEvent
{
	public string Id { get; set; } = Guid.NewGuid().ToString();
	public string EmailId { get; set; } = string.Empty;
	public string EventType { get; set; } = string.Empty;
	public string? LinkType { get; set; }
	public DateTime Timestamp { get; set; } = DateTime.UtcNow;
	public string UserAgent { get; set; } = string.Empty;
	public string IpAddress { get; set; } = string.Empty;
	public string? EmailClient { get; set; }
	public string? SourceEmailClient { get; set; }
	public string? DeviceType { get; set; }
	public string? OperatingSystem { get; set; }
	public string? Country { get; set; }
	public string? City { get; set; }
	public string SenderEmail { get; set; } = string.Empty;
	public string? RecipientEmail { get; set; }
}