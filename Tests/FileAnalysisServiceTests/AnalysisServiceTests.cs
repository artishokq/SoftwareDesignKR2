using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using FileAnalisysService.Models;
using FileAnalisysService.Services;
using Xunit;

namespace Tests.FileAnalysisServiceTests;

public class AnalysisServiceAdditionalTests
{
    private AnalysisService CreateServiceReturning(string text)
    {
        var handler = new DelegatingHandlerStub(text);
        var client = new HttpClient(handler) { BaseAddress = new Uri("http://dummy") };
        var factory = new StubHttpClientFactory(client);
        return new AnalysisService(factory);
    }
    
    [Theory]
    [InlineData("One", 1, 1)]
    [InlineData("One Two Three", 1, 3)]
    [InlineData("Para1\r\n\r\nPara2", 2, 2)]
    [InlineData("A B\r\n\r\nC D E\r\n\r\nF", 3, 6)]
    public async Task AnalyzeAsync_ParagraphAndWordCounts_Correct(string text, int paras, int words)
    {
        var svc = CreateServiceReturning(text);
        var id = Guid.NewGuid();

        var result = await svc.AnalyzeAsync(id);

        Assert.Equal(paras, result.Paragraphs);
        Assert.Equal(words, result.Words);
        Assert.Equal(text.Length, result.Characters);
    }

    [Fact]
    public async Task AnalyzeAsync_CachesResults_SimilarityListPopulated()
    {
        var svc = CreateServiceReturning("X");

        var id1 = Guid.NewGuid();
        var r1 = await svc.AnalyzeAsync(id1);
        Assert.Empty(r1.Similar);

        var id2 = Guid.NewGuid();
        var r2 = await svc.AnalyzeAsync(id2);
        Assert.Single(r2.Similar);
        Assert.Equal(id1, r2.Similar[0].OtherFileId);
        
        var cached1 = svc.Get(id1);
        var cached2 = svc.Get(id2);
        Assert.Equal(r1, cached1);
        Assert.Equal(r2, cached2);
    }

    [Fact]
    public void AnalyzeAsync_EmptyContent_ReturnsZeroCounts()
    {
        var svc = CreateServiceReturning("");
        var id = Guid.NewGuid();

        var r = svc.AnalyzeAsync(id).Result;
        Assert.Equal(1, r.Paragraphs);
        Assert.Equal(0, r.Words);
        Assert.Equal(0, r.Characters);
    }

    [Fact]
    public void Get_KnownAndUnknown_ThrowsForUnknown()
    {
        var svc = CreateServiceReturning("X");
        var idKnown = Guid.NewGuid();
        var _ = svc.AnalyzeAsync(idKnown).Result;
        
        var ok = svc.Get(idKnown);
        Assert.Equal(idKnown, ok.FileId);
        
        Assert.Throws<KeyNotFoundException>(() => svc.Get(Guid.NewGuid()));
    }

    // Вспомогательные классы
    class StubHttpClientFactory : IHttpClientFactory
    {
        private readonly HttpClient _client;
        public StubHttpClientFactory(HttpClient client) => _client = client;
        public HttpClient CreateClient(string name) => _client;
    }

    class DelegatingHandlerStub : DelegatingHandler
    {
        private readonly string _text;
        public DelegatingHandlerStub(string text) => _text = text;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage req, CancellationToken ct)
        {
            var resp = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(_text)
                {
                    Headers = { ContentType = new MediaTypeHeaderValue("text/plain") }
                }
            };
            return Task.FromResult(resp);
        }
    }
}