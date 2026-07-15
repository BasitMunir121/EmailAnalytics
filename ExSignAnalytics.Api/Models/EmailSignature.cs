namespace ExSignAnalytics.Api.Models;

public class EmailSignature
{
	public int Id { get; set; }
	public string Name { get; set; } = string.Empty;
	public string TrackingKey { get; set; } = string.Empty;
	public string HtmlBody { get; set; } = string.Empty;
	public bool EnableTracking { get; set; } = true;
	public bool IsEnabled { get; set; } = true;
	public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
	public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
