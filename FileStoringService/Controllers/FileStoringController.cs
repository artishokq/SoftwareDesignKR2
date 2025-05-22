using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Mvc;
using FileStoringService.Services;

namespace FileStoringService.Controllers;

[ApiController]
[Route("api/files")]
public class FilesController : ControllerBase
{
    private readonly FileService _svc;
    public FilesController(FileService svc) => _svc = svc;

    [HttpPost("upload")]
    public async Task<IActionResult> Upload(IFormFile file)
    {
        try
        {
            var id = await _svc.SaveAsync(file);
            return Created("", new { fileId = id });
        }
        catch (InvalidOperationException)
        {
            return Conflict("Already exists");
        }
    }

    [HttpGet("{id:guid}/download")]
    public IActionResult Download(Guid id)
    {
        try
        {
            var stream = _svc.GetStream(id);
            return File(stream, "text/plain", $"{id}.txt");
        }
        catch
        {
            return NotFound();
        }
    }

    [HttpGet("{id:guid}/metadata")]
    public IActionResult Metadata(Guid id)
    {
        if (!_svc.TryGetMetadata(id, out var m))
            return NotFound();
        return Ok(m);
    }
}