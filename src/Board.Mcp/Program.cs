using Board.Mcp;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = Host.CreateApplicationBuilder(args);

builder.Logging.AddConsole(options => options.LogToStandardErrorThreshold = LogLevel.Trace);

builder.Services.AddSingleton(_ => new BoardClient(
    new HttpClient { Timeout = TimeSpan.FromSeconds(10) },
    () => Environment.GetEnvironmentVariable("BOARD_API_URL")));
builder.Services.AddSingleton<CardSyncService>();
builder.Services
    .AddMcpServer(options => options.ServerInfo = new() { Name = "board", Version = "1.0.0" })
    .WithStdioServerTransport()
    .WithTools<CardReadTools>()
    .WithTools<CardWriteTools>();

await builder.Build().RunAsync();
