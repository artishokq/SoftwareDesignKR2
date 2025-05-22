using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Xunit;
using ApiGateway.Controllers;
using Microsoft.AspNetCore.Http;

namespace Tests.ApiGatewayTests;

class TestHttpClientFactory : IHttpClientFactory
{
    private readonly HttpClient _client;
    public TestHttpClientFactory(HttpClient client) => _client = client;
    public HttpClient CreateClient(string name) => _client;
}

public class GatewayControllerTests
{
    // Собираем HttpClient с заранее заданным ответом
    private TestHttpClientFactory MakeFactory(HttpResponseMessage response)
    {
        var handler = new FakeHandler(response);
        var client  = new HttpClient(handler);
        return new TestHttpClientFactory(client);
    }

    // Отдельный FakeHandler, который отдаёт всегда response
    class FakeHandler : HttpMessageHandler
    {
        private readonly HttpResponseMessage _resp;
        public FakeHandler(HttpResponseMessage resp) => _resp = resp;
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, System.Threading.CancellationToken ct)
            => Task.FromResult(_resp);
    }

    [Fact]
    public async Task UploadFile_ProxiesResponse()
    {
        // Arrange
        var msg = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"fileId\":\"00000000-0000-0000-0000-000000000000\"}")
            { Headers = { ContentType = new MediaTypeHeaderValue("application/json") } }
        };
        var ctrl = new GatewayController(MakeFactory(msg));
        var file = new FormFile(new MemoryStream(new byte[]{1}), 0, 1, "f", "t.txt");

        // Act
        var result = await ctrl.UploadFile(file) as ObjectResult;

        // Assert
        Assert.Equal((int)HttpStatusCode.OK, result.StatusCode);
        Assert.Contains("fileId", result.Value.ToString());
    }

    [Fact]
    public async Task Download_ReturnsFileContent()
    {
        // Arrange
        var data = new byte[]{ 10,20,30 };
        var content = new ByteArrayContent(data);
        content.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
        var msg = new HttpResponseMessage(HttpStatusCode.OK) { Content = content };
        var ctrl = new GatewayController(MakeFactory(msg));

        // Act
        var result = await ctrl.Download(Guid.NewGuid()) as FileContentResult;

        // Assert
        Assert.Equal("text/plain", result.ContentType);
        Assert.Equal(data, result.FileContents);
    }

    [Fact]
    public async Task AnalyzeFile_ReturnsStatusCode()
    {
        // Arrange
        var msg = new HttpResponseMessage(HttpStatusCode.NotFound);
        var ctrl = new GatewayController(MakeFactory(msg));
        var req = new GatewayController.AnalysisRequest(Guid.Empty);

        // Act
        var result = await ctrl.Analyze(req) as ObjectResult;

        // Assert
        Assert.Equal((int)HttpStatusCode.NotFound, result.StatusCode);
    }

    [Fact]
    public async Task GetAnalysis_ProxiesResponse()
    {
        // Arrange
        var msg = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"foo\":1}")
            { Headers = { ContentType = new MediaTypeHeaderValue("application/json") } }
        };
        var ctrl = new GatewayController(MakeFactory(msg));

        // Act
        var result = await ctrl.GetAnalysis(Guid.Empty) as ObjectResult;

        // Assert
        Assert.Equal((int)HttpStatusCode.OK, result.StatusCode);
        Assert.Contains("foo", result.Value.ToString());
    }
}
