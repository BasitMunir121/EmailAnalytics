using ExSignAnalytics.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace ExSignAnalytics.Api.Controllers;

[ApiController]
public class SurveyController : ControllerBase
{
	private readonly ISurveyService _surveyService;

	public SurveyController(ISurveyService surveyService)
	{
		_surveyService = surveyService;
	}

	/// <summary>
	/// Email survey option click. Example:
	/// /s/tracking_id_1?type=stars&amp;value=4&amp;sender=a@b.com
	/// /s/tracking_id_1?type=emoji&amp;value=happy
	/// /s/tracking_id_1?type=scale&amp;value=9
	/// </summary>
	[HttpGet("/s/{trackingKey}")]
	public async Task<IActionResult> RecordClick(
		string trackingKey,
		[FromQuery] string type,
		[FromQuery] string value,
		[FromQuery] string? sender = null,
		[FromQuery] string? recipient = null)
	{
		var result = await _surveyService.RecordSurveyClickAsync(
			trackingKey,
			type ?? "",
			value ?? "",
			Request,
			sender ?? "",
			recipient ?? "");

		if (!result.Success || string.IsNullOrWhiteSpace(result.FeedbackUrl))
			return BadRequest(new { error = result.Error ?? "Unable to record survey response" });

		return Redirect(result.FeedbackUrl);
	}

	[HttpGet("api/Survey/{token:guid}")]
	public async Task<IActionResult> GetByToken(Guid token)
	{
		var response = await _surveyService.GetByTokenAsync(token);
		if (response == null) return NotFound(new { error = "Survey response not found" });
		return Ok(response);
	}

	[HttpPost("api/Survey/feedback")]
	public async Task<IActionResult> SaveFeedback([FromBody] SurveyFeedbackRequest request)
	{
		if (request.Token == Guid.Empty)
			return BadRequest(new { error = "Missing token" });

		var saved = await _surveyService.SaveCommentAsync(request.Token, request.Comment ?? "");
		if (!saved) return NotFound(new { error = "Survey response not found" });

		return Ok(new { message = "Feedback saved" });
	}
}

public class SurveyFeedbackRequest
{
	public Guid Token { get; set; }
	public string? Comment { get; set; }
}
