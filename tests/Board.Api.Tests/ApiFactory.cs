using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;

namespace Board.Api.Tests;

public sealed class ApiFactory(string? dbPath = null) : WebApplicationFactory<Program>
{
    public string DbPath { get; } = dbPath ?? Path.Combine(Path.GetTempPath(), $"board-{Guid.NewGuid():N}.db");

    protected override void ConfigureWebHost(IWebHostBuilder builder) =>
        builder.UseSetting("Board:DbPath", DbPath);

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        SqliteConnection.ClearAllPools();
    }

    public void DeleteDatabase()
    {
        SqliteConnection.ClearAllPools();
        File.Delete(DbPath);
    }
}
