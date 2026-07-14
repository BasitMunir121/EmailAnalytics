namespace ExSignAnalytics.Api.Models;

public class SurveyResponse
{
	public int Id { get; set; }
	public int TrackingId { get; set; }
	public string SurveyType { get; set; } = string.Empty;
	public int? Score { get; set; }
	public string? ChoiceKey { get; set; }
	public Guid ResponseToken { get; set; } = Guid.NewGuid();
	public string? Comment { get; set; }
	public DateTime? CommentedAt { get; set; }
	public DateTime Timestamp { get; set; } = DateTime.UtcNow;
	public string UserAgent { get; set; } = string.Empty;
	public string IpAddress { get; set; } = string.Empty;
	public string? Browser { get; set; }
	public string? DeviceType { get; set; }
	public string? OperatingSystem { get; set; }
	public string SenderEmail { get; set; } = string.Empty;
	public string? RecipientEmail { get; set; }

	public Tracking Tracking { get; set; } = null!;
}
