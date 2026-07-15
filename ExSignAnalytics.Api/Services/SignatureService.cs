using ExSignAnalytics.Api.Data;
using ExSignAnalytics.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace ExSignAnalytics.Api.Services;

public interface ISignatureService
{
	Task<List<EmailSignatureDto>> GetAllAsync();
	Task<EmailSignatureDto?> GetByIdAsync(int id);
	Task<EmailSignatureDto> CreateAsync(UpsertSignatureRequest request);
	Task<EmailSignatureDto?> UpdateAsync(int id, UpsertSignatureRequest request);
	Task<bool> SetEnabledAsync(int id, bool isEnabled);
	Task<bool> DeleteAsync(int id);
}

public class EmailSignatureDto
{
	public int Id { get; set; }
	public string Name { get; set; } = string.Empty;
	public string TrackingKey { get; set; } = string.Empty;
	public string HtmlBody { get; set; } = string.Empty;
	public bool EnableTracking { get; set; }
	public bool IsEnabled { get; set; }
	public DateTime CreatedAt { get; set; }
	public DateTime UpdatedAt { get; set; }
}

public class UpsertSignatureRequest
{
	public string Name { get; set; } = string.Empty;
	public string TrackingKey { get; set; } = string.Empty;
	public string HtmlBody { get; set; } = string.Empty;
	public bool EnableTracking { get; set; } = true;
	public bool IsEnabled { get; set; } = true;
}

public class SignatureService : ISignatureService
{
	private readonly TrackingDbContext _db;
	private readonly IConfiguration _configuration;

	public SignatureService(TrackingDbContext db, IConfiguration configuration)
	{
		_db = db;
		_configuration = configuration;
	}

	public async Task<List<EmailSignatureDto>> GetAllAsync()
	{
		return await _db.EmailSignatures
			.AsNoTracking()
			.OrderByDescending(s => s.CreatedAt)
			.Select(s => ToDto(s))
			.ToListAsync();
	}

	public async Task<EmailSignatureDto?> GetByIdAsync(int id)
	{
		var entity = await _db.EmailSignatures.AsNoTracking().FirstOrDefaultAsync(s => s.Id == id);
		return entity == null ? null : ToDto(entity);
	}

	public async Task<EmailSignatureDto> CreateAsync(UpsertSignatureRequest request)
	{
		Validate(request);

		var trackingKey = string.IsNullOrWhiteSpace(request.TrackingKey)
			? $"sig_{Guid.NewGuid():N}"[..20]
			: request.TrackingKey.Trim();

		if (await _db.EmailSignatures.AnyAsync(s => s.TrackingKey == trackingKey))
			throw new InvalidOperationException("Tracking key already exists.");

		var html = request.HtmlBody ?? string.Empty;
		if (request.EnableTracking)
			html = EnsureTrackingPixel(html, trackingKey);

		var entity = new EmailSignature
		{
			Name = request.Name.Trim(),
			TrackingKey = trackingKey,
			HtmlBody = html,
			EnableTracking = request.EnableTracking,
			IsEnabled = request.IsEnabled,
			CreatedAt = DateTime.UtcNow,
			UpdatedAt = DateTime.UtcNow
		};

		_db.EmailSignatures.Add(entity);
		await _db.SaveChangesAsync();
		return ToDto(entity);
	}

	public async Task<EmailSignatureDto?> UpdateAsync(int id, UpsertSignatureRequest request)
	{
		Validate(request);

		var entity = await _db.EmailSignatures.FirstOrDefaultAsync(s => s.Id == id);
		if (entity == null) return null;

		var trackingKey = string.IsNullOrWhiteSpace(request.TrackingKey)
			? entity.TrackingKey
			: request.TrackingKey.Trim();

		if (await _db.EmailSignatures.AnyAsync(s => s.TrackingKey == trackingKey && s.Id != id))
			throw new InvalidOperationException("Tracking key already exists.");

		var html = request.HtmlBody ?? string.Empty;
		if (request.EnableTracking)
			html = EnsureTrackingPixel(html, trackingKey);

		entity.Name = request.Name.Trim();
		entity.TrackingKey = trackingKey;
		entity.HtmlBody = html;
		entity.EnableTracking = request.EnableTracking;
		entity.IsEnabled = request.IsEnabled;
		entity.UpdatedAt = DateTime.UtcNow;

		await _db.SaveChangesAsync();
		return ToDto(entity);
	}

	public async Task<bool> SetEnabledAsync(int id, bool isEnabled)
	{
		var entity = await _db.EmailSignatures.FirstOrDefaultAsync(s => s.Id == id);
		if (entity == null) return false;
		entity.IsEnabled = isEnabled;
		entity.UpdatedAt = DateTime.UtcNow;
		await _db.SaveChangesAsync();
		return true;
	}

	public async Task<bool> DeleteAsync(int id)
	{
		var entity = await _db.EmailSignatures.FirstOrDefaultAsync(s => s.Id == id);
		if (entity == null) return false;
		_db.EmailSignatures.Remove(entity);
		await _db.SaveChangesAsync();
		return true;
	}

	private string EnsureTrackingPixel(string html, string trackingKey)
	{
		var apiBase = (_configuration["PublicApiBaseUrl"] ?? "https://localhost:7084").TrimEnd('/');
		var pixel = $"<img src=\"{apiBase}/t/{trackingKey}.gif?sender={{{{Email}}}}\" width=\"1\" height=\"1\" style=\"display:none!important;width:1px;height:1px;\" alt=\"\" />";

		if (html.Contains($"/t/{trackingKey}.gif", StringComparison.OrdinalIgnoreCase))
			return html;

		return html + Environment.NewLine + pixel;
	}

	private static void Validate(UpsertSignatureRequest request)
	{
		if (string.IsNullOrWhiteSpace(request.Name))
			throw new InvalidOperationException("Signature name is required.");
	}

	private static EmailSignatureDto ToDto(EmailSignature s) => new()
	{
		Id = s.Id,
		Name = s.Name,
		TrackingKey = s.TrackingKey,
		HtmlBody = s.HtmlBody,
		EnableTracking = s.EnableTracking,
		IsEnabled = s.IsEnabled,
		CreatedAt = s.CreatedAt,
		UpdatedAt = s.UpdatedAt
	};
}
