using ExSignAnalytics.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace ExSignAnalytics.Web.Controllers;

public class SignaturesController : Controller
{
	private readonly IAnalyticsApiClient _apiClient;
	private readonly IConfiguration _configuration;

	public SignaturesController(IAnalyticsApiClient apiClient, IConfiguration configuration)
	{
		_apiClient = apiClient;
		_configuration = configuration;
	}

	public async Task<IActionResult> Index(CancellationToken cancellationToken)
	{
		ViewData["Title"] = "Manage Signatures";
		ViewData["Nav"] = "Signatures";

		try
		{
			var items = await _apiClient.GetSignaturesAsync(cancellationToken);
			return View(items);
		}
		catch
		{
			ViewBag.Error = "Unable to load signatures from the API.";
			return View(new List<SignatureInfo>());
		}
	}

	[HttpGet]
	public IActionResult Create()
	{
		ViewData["Title"] = "New Signature";
		ViewData["Nav"] = "Signatures";
		ViewBag.ApiBaseUrl = (_configuration["ApiBaseUrl"] ?? "https://localhost:7084").TrimEnd('/');

		return View("Edit", new SignatureEditModel
		{
			TrackingKey = $"sig_{Guid.NewGuid():N}"[..16],
			EnableTracking = true,
			IsEnabled = true,
			HtmlBody = "<p>Best regards,<br/>{{DisplayName}}</p>"
		});
	}

	[HttpGet]
	public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
	{
		ViewData["Title"] = "Edit Signature";
		ViewData["Nav"] = "Signatures";
		ViewBag.ApiBaseUrl = (_configuration["ApiBaseUrl"] ?? "https://localhost:7084").TrimEnd('/');

		try
		{
			var item = await _apiClient.GetSignatureAsync(id, cancellationToken);
			if (item == null)
			{
				TempData["Error"] = "Signature not found.";
				return RedirectToAction(nameof(Index));
			}

			return View(new SignatureEditModel
			{
				Id = item.Id,
				Name = item.Name,
				TrackingKey = item.TrackingKey,
				HtmlBody = item.HtmlBody,
				EnableTracking = item.EnableTracking,
				IsEnabled = item.IsEnabled
			});
		}
		catch
		{
			TempData["Error"] = "Unable to load signature.";
			return RedirectToAction(nameof(Index));
		}
	}

	[HttpPost]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> Save(SignatureEditModel model, CancellationToken cancellationToken)
	{
		ViewData["Nav"] = "Signatures";
		ViewBag.ApiBaseUrl = (_configuration["ApiBaseUrl"] ?? "https://localhost:7084").TrimEnd('/');

		if (string.IsNullOrWhiteSpace(model.Name))
		{
			ModelState.AddModelError(nameof(model.Name), "Name is required.");
			ViewData["Title"] = model.Id == 0 ? "New Signature" : "Edit Signature";
			return View("Edit", model);
		}

		var request = new UpsertSignatureInfo
		{
			Name = model.Name,
			TrackingKey = model.TrackingKey,
			HtmlBody = model.HtmlBody ?? string.Empty,
			EnableTracking = model.EnableTracking,
			IsEnabled = model.IsEnabled
		};

		try
		{
			if (model.Id == 0)
			{
				var created = await _apiClient.CreateSignatureAsync(request, cancellationToken);
				if (created == null)
				{
					ViewBag.Error = "Failed to create signature.";
					ViewData["Title"] = "New Signature";
					return View("Edit", model);
				}
			}
			else
			{
				var updated = await _apiClient.UpdateSignatureAsync(model.Id, request, cancellationToken);
				if (updated == null)
				{
					ViewBag.Error = "Failed to update signature.";
					ViewData["Title"] = "Edit Signature";
					return View("Edit", model);
				}
			}

			return RedirectToAction(nameof(Index));
		}
		catch
		{
			ViewBag.Error = "Unable to save signature.";
			ViewData["Title"] = model.Id == 0 ? "New Signature" : "Edit Signature";
			return View("Edit", model);
		}
	}

	[HttpPost]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> Toggle(int id, bool isEnabled, CancellationToken cancellationToken)
	{
		await _apiClient.SetSignatureEnabledAsync(id, isEnabled, cancellationToken);
		return RedirectToAction(nameof(Index));
	}

	[HttpPost]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
	{
		await _apiClient.DeleteSignatureAsync(id, cancellationToken);
		return RedirectToAction(nameof(Index));
	}
}

public class SignatureEditModel
{
	public int Id { get; set; }
	public string Name { get; set; } = string.Empty;
	public string TrackingKey { get; set; } = string.Empty;
	public string HtmlBody { get; set; } = string.Empty;
	public bool EnableTracking { get; set; } = true;
	public bool IsEnabled { get; set; } = true;
}
