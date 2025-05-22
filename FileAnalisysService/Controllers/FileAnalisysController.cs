using FileAnalisysService.Models;
using FileAnalisysService.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Mvc;

namespace FileAnalisysService.Controllers;

[ApiController]
[Route("api/analysis")]
public class AnalysisController : ControllerBase
{
    private readonly AnalysisService _svc;
    public AnalysisController(AnalysisService svc) => _svc = svc;

    [HttpPost]
    public async Task<IActionResult> Analyze([FromBody] AnalysisRequest req)
    {
        try
        {
            var r = await _svc.AnalyzeAsync(req.FileId);
            return Ok(r);
        }
        catch
        {
            return NotFound();
        }
    }

    [HttpGet("{id:guid}")]
    public IActionResult GetResult(Guid id)
    {
        try
        {
            var r = _svc.Get(id);
            return Ok(r);
        }
        catch
        {
            return NotFound();
        }
    }
}