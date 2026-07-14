using Microsoft.AspNetCore.Mvc;

namespace ExSignAnalytics.Web.Controllers;

public class DashboardController : Controller
{
	public IActionResult Index()
	{
		return View();
	}
}
