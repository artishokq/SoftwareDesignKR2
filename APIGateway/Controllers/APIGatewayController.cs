using System.Net.Http.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace ApiGateway.Controllers;

[ApiController]
[Route("api/gateway")]
public class GatewayController : ControllerBase
{
    private readonly IHttpClientFactory _http;

    public GatewayController(IHttpClientFactory http) => _http = http;

    [HttpPost("files")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadFile(IFormFile file)
    {
        var client = _http.CreateClient();
        using var content = new MultipartFormDataContent
        {
            { new StreamContent(file.OpenReadStream()), "file", file.FileName }
        };
        var resp = await client.PostAsync("http://file-storage/api/files/upload", content);
        var body = await resp.Content.ReadAsStringAsync();
        return StatusCode((int)resp.StatusCode, body);
    }

    [HttpGet("files/{id:guid}")]
    public async Task<IActionResult> Download(Guid id)
    {
        var client = _http.CreateClient();
        var resp = await client.GetAsync($"http://file-storage/api/files/{id}/download");
        if (!resp.IsSuccessStatusCode) return NotFound();
        var bytes = await resp.Content.ReadAsByteArrayAsync();
        return File(bytes, "text/plain", $"{id}.txt");
    }

    [HttpPost("analysis")]
    public async Task<IActionResult> Analyze([FromBody] AnalysisRequest req)
    {
        var client = _http.CreateClient();
        var resp = await client.PostAsJsonAsync("http://file-analysis/api/analysis", req);
        var body = await resp.Content.ReadAsStringAsync();
        return StatusCode((int)resp.StatusCode, body);
    }

    [HttpGet("analysis/{id:guid}")]
    public async Task<IActionResult> GetAnalysis(Guid id)
    {
        var client = _http.CreateClient();
        var resp = await client.GetAsync($"http://file-analysis/api/analysis/{id}");
        var body = await resp.Content.ReadAsStringAsync();
        return StatusCode((int)resp.StatusCode, body);
    }

    public record AnalysisRequest(Guid FileId);
}