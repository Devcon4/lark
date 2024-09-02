using Lark.Engine;
using Lark.Game;
using Microsoft.Extensions.Hosting;
using Serilog;

var builder = Host.CreateApplicationBuilder(args);

builder.Services
  .AddLarkEngine(builder.Configuration)
  .AddVulkanPipeline()
  .AddLarkECS()
  .AddLarkSTD()
  .AddLarkJolt()
  // .AddLarkPhysx(builder.Configuration)
  .AddLarkUltralight(builder.Configuration)
  .AddGameSystems()
  .AddSerilog(new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .WriteTo.Trace()
    .CreateLogger());

var host = builder.Build();

host.RunEngine();
