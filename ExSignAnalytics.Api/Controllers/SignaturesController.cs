using ExSignAnalytics.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace ExSignAnalytics.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SignaturesController : ControllerBase
{
	private readonly ISignatureService _signatureService;

	public SignaturesController(ISignatureService signatureService)
	{
		_signatureService = signatureService;
	}

	[HttpGet]
	public async Task<IActionResult> GetAll()
	{
		var items = await _signatureService.GetAllAsync();
		return Ok(items);
	}

	[HttpGet("{id:int}")]
	public async Task<IActionResult> GetById(int id)
	{
		var item = await _signatureService.GetByIdAsync(id);
		if (item == null) return NotFound();
		return Ok(item);
	}

	[HttpPost]
	public async Task<IActionResult> Create([FromBody] UpsertSignatureRequest request)
	{
		try
		{
			var created = await _signatureService.CreateAsync(request);
			return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
		}
		catch (InvalidOperationException ex)
		{
			return BadRequest(new { error = ex.Message });
		}
	}

	[HttpPut("{id:int}")]
	public async Task<IActionResult> Update(int id, [FromBody] UpsertSignatureRequest request)
	{
		try
		{
			var updated = await _signatureService.UpdateAsync(id, request);
			if (updated == null) return NotFound();
			return Ok(updated);
		}
		catch (InvalidOperationException ex)
		{
			return BadRequest(new { error = ex.Message });
		}
	}

	[HttpPost("{id:int}/enabled")]
	public async Task<IActionResult> SetEnabled(int id, [FromBody] SetEnabledRequest request)
	{
		var ok = await _signatureService.SetEnabledAsync(id, request.IsEnabled);
		if (!ok) return NotFound();
		return Ok(new { message = "Updated" });
	}

	[HttpDelete("{id:int}")]
	public async Task<IActionResult> Delete(int id)
	{
		var ok = await _signatureService.DeleteAsync(id);
		if (!ok) return NotFound();
		return Ok(new { message = "Deleted" });
	}
}

public class SetEnabledRequest
{
	public bool IsEnabled { get; set; }
}
