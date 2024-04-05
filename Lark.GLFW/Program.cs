using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Silk.NET.GLFW;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSingleton<Window>();
builder.Services.AddSingleton<Engine>();
builder.Services.AddHostedService<Game>();

var host = builder.Build();

host.Run();