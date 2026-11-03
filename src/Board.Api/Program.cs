using System.Text.Json.Serialization;
using Board.Api.Cards;
using Board.Api.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var dbPath = Path.GetFullPath(builder.Configuration["Board:DbPath"] ?? "board.db");
Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);
var connectionString = new SqliteConnectionStringBuilder { DataSource = dbPath }.ToString();

builder.Services.AddDbContext<BoardDbContext>(options => options.UseSqlite(connectionString));
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<CardService>();
builder.Services.AddProblemDetails();
builder.Services.Configure<RouteHandlerOptions>(options => options.ThrowOnBadRequest = false);
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter(allowIntegerValues: false)));

var app = builder.Build();

using (var scope = app.Services.CreateScope())
    scope.ServiceProvider.GetRequiredService<BoardDbContext>().Database.Migrate();

app.UseExceptionHandler();
app.UseStatusCodePages();
app.MapCardEndpoints();

app.Run();

public partial class Program;
