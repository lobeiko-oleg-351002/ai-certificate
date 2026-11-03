using Board.Contracts;

namespace Board.Mcp.Tests;

public class LiveApiTests
{
    [Fact]
    public async Task CreateGetListMoveUpdate_RoundTripThroughTheRealApi()
    {
        using var api = new LiveApi();
        var tools = api.Tools;

        var created = Harness.CardOf(await tools.Write.CreateCard("First card", "About it", ct: default));
        Assert.Equal("BRD-1", created.Key);
        Assert.Equal(CardStatus.ToDo, created.Status);

        var fetched = Harness.CardOf(await tools.Read.GetCard("BRD-1", default));
        Assert.Equal("First card", fetched.Title);

        var moved = Harness.CardOf(await tools.Write.MoveCard("BRD-1", "InProgress", default));
        Assert.Equal(CardStatus.InProgress, moved.Status);

        var updated = Harness.CardOf(await tools.Write.UpdateCard("BRD-1", phase: "Phase 5", pendingGate: "plan", ct: default));
        Assert.Equal("Phase 5", updated.Phase);
        Assert.Equal("plan", updated.PendingGate);
        Assert.Equal("First card", updated.Title);

        var cleared = Harness.CardOf(await tools.Write.UpdateCard("BRD-1", phase: "", ct: default));
        Assert.Null(cleared.Phase);
        Assert.Equal("plan", cleared.PendingGate);

        var list = Harness.TextOf(await tools.Read.ListCards(default));
        Assert.Contains("\"key\":\"BRD-1\"", list);
    }

    [Fact]
    public async Task SyncCard_CreatesAMissingCardThenUpdatesIt()
    {
        using var api = new LiveApi();
        var tools = api.Tools;

        var first = await tools.Write.SyncCard("BRD-3", "MCP server", "InProgress", phase: "Phase 5", ct: default);
        Assert.Contains("\"action\":\"created\"", Harness.TextOf(first));

        var second = await tools.Write.SyncCard("BRD-3", "MCP server", "Done", phase: "", ct: default);
        Assert.Contains("\"action\":\"updated\"", Harness.TextOf(second));

        var card = Harness.CardOf(await tools.Read.GetCard("BRD-3", default));
        Assert.Equal(CardStatus.Done, card.Status);
        Assert.Null(card.Phase);
    }

    [Fact]
    public async Task UnknownCard_IsNotFound()
    {
        using var api = new LiveApi();

        var result = await api.Tools.Read.GetCard("BRD-99", default);

        Assert.True(result.IsError);
        Assert.Contains("[not_found] get_card", Harness.TextOf(result));
    }

    [Fact]
    public async Task DuplicateExplicitKey_IsAConflict()
    {
        using var api = new LiveApi();
        var tools = api.Tools;
        await tools.Write.CreateCard("A", key: "BRD-5", ct: default);

        var result = await tools.Write.CreateCard("B", key: "BRD-5", ct: default);

        Assert.Contains("[conflict] create_card", Harness.TextOf(result));
    }

    [Fact]
    public async Task BadKey_IsAValidationErrorWithTheApiMessage()
    {
        using var api = new LiveApi();

        var result = await api.Tools.Write.CreateCard("A", key: "TASK-1", ct: default);

        var text = Harness.TextOf(result);
        Assert.Contains("[validation] create_card", text);
        Assert.Contains("status: 400", text);
    }
}
