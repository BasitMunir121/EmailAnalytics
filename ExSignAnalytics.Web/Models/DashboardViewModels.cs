namespace ExSignAnalytics.Web.Models;

public class TrackingSummaryViewModel
{
	public string TrackingId { get; set; } = string.Empty;
	public int OpenCount { get; set; }
	public int ClickCount { get; set; }
}

public class TrackingDetailsViewModel
{
	public string TrackingId { get; set; } = string.Empty;
	public List<OpenDetailViewModel> Opens { get; set; } = new();
	public List<ClickDetailViewModel> Clicks { get; set; } = new();
}

public class OpenDetailViewModel
{
	public DateTime Timestamp { get; set; }
	public string? EmailClient { get; set; }
	public string? DeviceType { get; set; }
}

public class ClickDetailViewModel
{
	public DateTime Timestamp { get; set; }
	public string? LinkType { get; set; }
	public string? Browser { get; set; }
	public string? SourceClient { get; set; }
}

public class TrackingApiItem
{
	public string TrackingId { get; set; } = string.Empty;
	public List<OpenDetailViewModel> Opens { get; set; } = new();
	public List<ClickDetailViewModel> Clicks { get; set; } = new();
}
