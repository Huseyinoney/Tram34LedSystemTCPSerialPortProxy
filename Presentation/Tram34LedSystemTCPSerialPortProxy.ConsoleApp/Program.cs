using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Tram34LedSystemTCPSerialPortProxy.Application.Abstractions.SerialPortService;
using Tram34LedSystemTCPSerialPortProxy.Application.Abstractions.Tcp;
using Tram34LedSystemTCPSerialPortProxy.Infrastructure.Services.SerialPortServices;
using Tram34LedSystemTCPSerialPortProxy.Infrastructure.Services.TcpSerialPortBackgroundService;
using Tram34LedSystemTCPSerialPortProxy.Infrastructure.Services.TcpServices;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);
builder.Services.AddWindowsService();

builder.Configuration
    .SetBasePath(AppContext.BaseDirectory) // 🔥 servis uyumlu
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddEnvironmentVariables();

builder.Services.AddSingleton<ISerialPortService, SerialPortService>();
builder.Services.AddSingleton<ITcpService, TcpService>();

builder.Services.AddHostedService<TcpSerialPortBackgroundService>();

using IHost host = builder.Build();
await host.RunAsync();

