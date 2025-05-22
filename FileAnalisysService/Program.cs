using FileAnalisysService.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "File Analysis", Version = "v1" }));

builder.Services.AddHttpClient("store", c =>
    c.BaseAddress = new Uri(builder.Configuration["FileStoring:Url"]!));

builder.Services.AddSingleton<AnalysisService>();
builder.Services.AddControllers();

var app = builder.Build();
app.UseSwagger();
app.UseSwaggerUI();
app.MapControllers();
app.Run();