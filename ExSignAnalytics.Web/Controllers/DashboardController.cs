using System.Text.Json;
using ExSignAnalytics.Web.Models;
using ExSignAnalytics.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace ExSignAnalytics.Web.Controllers;

public class DashboardController : Controller
{
	private static readonly JsonSerializerOptions JsonOptions = new()
	{
		PropertyNameCaseInsensitive = true
	};

	private readonly IAnalyticsApiClient _apiClient;

	public DashboardController(IAnalyticsApiClient apiClient)
	{
		_apiClient = apiClient;
	}

	public async Task<IActionResult> Index(CancellationToken cancellationToken)
	{
		ViewData["Title"] = "Dashboard";
		ViewData["Nav"] = "Dashboard";

		try
		{
			var json = await _apiClient.GetAllTrackingDataAsync(cancellationToken);
			var items = JsonSerializer.Deserialize<List<TrackingApiItem>>(json, JsonOptions) ?? new();

			var model = items
				.Select(i => new TrackingSummaryViewModel
				{
					TrackingId = i.TrackingId,
					OpenCount = i.Opens?.Count ?? 0,
					ClickCount = i.Clicks?.Count ?? 0
				})
				.OrderByDescending(i => i.OpenCount + i.ClickCount)
				.ToList();

			return View(model);
		}
		catch
		{
			ViewBag.Error = "Unable to load tracking data from the API.";
			return View(new List<TrackingSummaryViewModel>());
		}
	}

	public async Task<IActionResult> Details(string id, CancellationToken cancellationToken)
	{
		if (string.IsNullOrWhiteSpace(id))
			return RedirectToAction(nameof(Index));

		ViewData["Title"] = "Tracking Details";
		ViewData["Nav"] = "Dashboard";

		try
		{
			var json = await _apiClient.GetAllTrackingDataAsync(cancellationToken);
			var items = JsonSerializer.Deserialize<List<TrackingApiItem>>(json, JsonOptions) ?? new();
			var item = items.FirstOrDefault(i => string.Equals(i.TrackingId, id, StringComparison.OrdinalIgnoreCase));

			if (item == null)
			{
				ViewBag.Error = $"Tracking ID '{id}' was not found.";
				return View(new TrackingDetailsViewModel { TrackingId = id });
			}

			return View(new TrackingDetailsViewModel
			{
				TrackingId = item.TrackingId,
				Opens = item.Opens ?? new(),
				Clicks = item.Clicks ?? new()
			});
		}
		catch
		{
			ViewBag.Error = "Unable to load tracking details from the API.";
			return View(new TrackingDetailsViewModel { TrackingId = id });
		}
	}

	[HttpPost]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> ClearAll(CancellationToken cancellationToken)
	{
		try
		{
			await _apiClient.ClearAllDataAsync(cancellationToken);
		}
		catch
		{
			TempData["Error"] = "Failed to clear tracking data.";
		}

		return RedirectToAction(nameof(Index));
	}
}
