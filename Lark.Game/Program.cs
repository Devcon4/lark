using Lark.Engine;
using Lark.Game;
using Microsoft.Extensions.Hosting;
using Serilog;

var builder = Host.CreateApplicationBuilder(args);

builder.Services
  .AddLarkEngine()
  .AddVulkanPipeline()
  .AddGame()
  .AddGameSystems()
  .AddSerilog(new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger());

var host = builder.Build();
host.Run();