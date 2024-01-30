using System.Security.Cryptography;
using Lark.Engine;
using Lark.Game;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;

var builder = Host.CreateApplicationBuilder(args);

builder.Services
  .AddLarkEngine(builder.Configuration)
  .AddVulkanPipeline()
  .AddLarkECS()
  .AddLarkSTD()
  .AddLarkPhysx(builder.Configuration)
  .AddGame()
  .AddGameSystems()
  .AddSerilog(new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger());

var host = builder.Build();
host.Run();
