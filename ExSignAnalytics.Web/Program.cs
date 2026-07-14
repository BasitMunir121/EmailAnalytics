var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

var apiBaseUrl = builder.Configuration["ApiBaseUrl"]?.TrimEnd('/') ?? "https://localhost:7084";

builder.Services.AddHttpClient<ExSignAnalytics.Web.Services.IAnalyticsApiClient, ExSignAnalytics.Web.Services.AnalyticsApiClient>(client =>
{
	client.BaseAddress = new Uri(apiBaseUrl + "/");
})
.ConfigurePrimaryHttpMessageHandler(() =>
{
	var handler = new HttpClientHandler();
	if (builder.Environment.IsDevelopment())
	{
		handler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
	}
	return handler;
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
	app.UseExceptionHandler("/Home/Error");
	app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

app.MapControllers();
app.MapControllerRoute(
	name: "default",
	pattern: "{controller=Dashboard}/{action=Index}/{id?}");

app.Run();
