using ExSignAnalytics.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace ExSignAnalytics.Web.Controllers;

public class FeedbackController : Controller
{
	private readonly IAnalyticsApiClient _apiClient;

	public FeedbackController(IAnalyticsApiClient apiClient)
	{
		_apiClient = apiClient;
	}

	[HttpGet]
	public async Task<IActionResult> Index(Guid token, CancellationToken cancellationToken)
	{
		ViewData["Title"] = "Feedback";
		ViewData["Nav"] = "Dashboard";

		if (token == Guid.Empty)
		{
			ViewBag.Error = "Invalid feedback link.";
			return View(new FeedbackPageModel());
		}

		try
		{
			var response = await _apiClient.GetSurveyByTokenAsync(token, cancellationToken);
			if (response == null)
			{
				ViewBag.Error = "This feedback link is invalid or has expired.";
				return View(new FeedbackPageModel { Token = token });
			}

			return View(new FeedbackPageModel
			{
				Token = response.ResponseToken,
				TrackingKey = response.TrackingKey,
				SurveyType = response.SurveyType,
				Score = response.Score,
				ChoiceKey = response.ChoiceKey,
				Comment = response.Comment ?? string.Empty,
				AlreadyCommented = !string.IsNullOrWhiteSpace(response.Comment)
			});
		}
		catch
		{
			ViewBag.Error = "Unable to load feedback form.";
			return View(new FeedbackPageModel { Token = token });
		}
	}

	[HttpPost]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> Index(FeedbackPageModel model, CancellationToken cancellationToken)
	{
		ViewData["Title"] = "Feedback";
		ViewData["Nav"] = "Dashboard";

		if (model.Token == Guid.Empty)
		{
			ViewBag.Error = "Invalid feedback link.";
			return View(model);
		}

		try
		{
			var saved = await _apiClient.SaveSurveyFeedbackAsync(model.Token, model.Comment ?? "", cancellationToken);
			if (!saved)
			{
				ViewBag.Error = "Unable to save feedback.";
				return View(model);
			}

			ViewBag.Success = "Thank you! Your feedback was saved.";
			model.AlreadyCommented = true;
			return View(model);
		}
		catch
		{
			ViewBag.Error = "Unable to save feedback.";
			return View(model);
		}
	}
}

public class FeedbackPageModel
{
	public Guid Token { get; set; }
	public string TrackingKey { get; set; } = string.Empty;
	public string SurveyType { get; set; } = string.Empty;
	public int? Score { get; set; }
	public string? ChoiceKey { get; set; }
	public string Comment { get; set; } = string.Empty;
	public bool AlreadyCommented { get; set; }
}
