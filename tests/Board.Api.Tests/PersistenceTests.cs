using System.Net.Http.Json;
using Board.Contracts;

namespace Board.Api.Tests;

public sealed class PersistenceTests
{
    [Fact]
    public async Task Cards_SurviveAHostRestartOnTheSameDatabaseFile()
    {
        var path = Path.Combine(Path.GetTempPath(), $"board-{Guid.NewGuid():N}.db");
        try
        {
            using (var first = new ApiFactory(path))
            using (var client = first.CreateClient())
            {
                await client.CreateOkAsync(new { title = "Survivor", description = "kept" });
                await client.PatchAsync("BRD-1", """{"status":"InProgress","phase":"Phase 5"}""");
            }

            using var second = new ApiFactory(path);
            using var restarted = second.CreateClient();
            var card = await restarted.GetFromJsonAsync<CardDto>("/api/cards/BRD-1", TestJson.Options);
            var next = await restarted.CreateOkAsync(new { title = "After restart" });

            Assert.Equal("Survivor", card!.Title);
            Assert.Equal(CardStatus.InProgress, card.Status);
            Assert.Equal("Phase 5", card.Phase);
            Assert.Equal("BRD-2", next.Key);
        }
        finally
        {
            Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
            File.Delete(path);
        }
    }
}
