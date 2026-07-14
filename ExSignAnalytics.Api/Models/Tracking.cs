namespace ExSignAnalytics.Api.Models;

public class Tracking
{
	public int Id { get; set; }
	public string TrackingKey { get; set; } = string.Empty;
	public string SenderEmail { get; set; } = string.Empty;
	public string? RecipientEmail { get; set; }
	public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

	public ICollection<Open> Opens { get; set; } = new List<Open>();
	public ICollection<Click> Clicks { get; set; } = new List<Click>();
}
