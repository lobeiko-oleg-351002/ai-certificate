using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;

namespace Board.Mcp.Tests;

public sealed class LiveApi : WebApplicationFactory<Program>
{
    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"board-mcp-{Guid.NewGuid():N}.db");

    public Harness Tools => new(CreateClient(), "http://localhost");

    protected override void ConfigureWebHost(IWebHostBuilder builder) =>
        builder.UseSetting("Board:DbPath", _dbPath);

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        SqliteConnection.ClearAllPools();
        File.Delete(_dbPath);
    }
}
