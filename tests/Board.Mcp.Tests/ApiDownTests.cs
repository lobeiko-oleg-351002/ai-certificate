using ModelContextProtocol.Protocol;

namespace Board.Mcp.Tests;

public class ApiDownTests
{
    [Theory]
    [InlineData("list_cards", false)]
    [InlineData("get_card", false)]
    [InlineData("create_card", true)]
    [InlineData("move_card", true)]
    [InlineData("update_card", true)]
    [InlineData("sync_card", true)]
    public async Task EveryTool_ReturnsIsErrorNamingTheUrlWhenTheApiIsDown(string tool, bool write)
    {
        var url = ClosedPort.Url();
        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
        var harness = new Harness(http, url);

        var result = await Call(harness, tool);

        var text = Harness.TextOf(result);
        Assert.True(result.IsError);
        Assert.StartsWith($"BOARD ERROR [unreachable] {tool}:", text);
        Assert.Contains($"api: {url}", text);
        Assert.Equal(write, text.Contains("Update NOT applied."));
    }

    private static Task<CallToolResult> Call(Harness h, string tool) => tool switch
    {
        "list_cards" => h.Read.ListCards(default),
        "get_card" => h.Read.GetCard("BRD-1", default),
        "create_card" => h.Write.CreateCard("T", ct: default),
        "move_card" => h.Write.MoveCard("BRD-1", "Done", default),
        "update_card" => h.Write.UpdateCard("BRD-1", title: "T", ct: default),
        _ => h.Write.SyncCard("BRD-1", "T", "ToDo", ct: default)
    };
}
