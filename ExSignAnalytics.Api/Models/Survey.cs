namespace ExSignAnalytics.Api.Models;

public class Survey
{
	public int Id { get; set; }
	public string Name { get; set; } = string.Empty;
	public string SurveyType { get; set; } = string.Empty;
	public int MinValue { get; set; } = 1;
	public int MaxValue { get; set; } = 5;
	public bool IsActive { get; set; } = true;
	public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

	public ICollection<Tracking> Trackings { get; set; } = new List<Tracking>();
}
