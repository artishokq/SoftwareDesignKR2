using System.Net.Http.Json;
using System.Text.RegularExpressions;
using FileAnalisysService.Models;

namespace FileAnalisysService.Services;

public class AnalysisService
{
    private readonly IHttpClientFactory _http;
    private readonly Dictionary<Guid, AnalysisResult> _cache = new();

    public AnalysisService(IHttpClientFactory http) => _http = http;

    public async Task<AnalysisResult> AnalyzeAsync(Guid fileId)
    {
        if (_cache.TryGetValue(fileId, out var res)) return res;

        var client = _http.CreateClient("store");
        var text   = await client.GetStringAsync($"/api/files/{fileId}/download");

        res = new AnalysisResult {
            FileId     = fileId,
            Paragraphs = text.Split("\r\n\r\n").Length,
            Words      = Regex.Matches(text, @"\b\w+\b").Count,
            Characters = text.Length
        };
        
        foreach (var other in _cache.Values)
        {
            var sim = 0.0;
            res.Similar.Add(new Similarity { OtherFileId = other.FileId, Score = sim });
        }

        _cache[fileId] = res;
        return res;
    }

    public AnalysisResult Get(Guid fileId)
    {
        if (!_cache.TryGetValue(fileId, out var r))
            throw new KeyNotFoundException();
        return r;
    }
}
