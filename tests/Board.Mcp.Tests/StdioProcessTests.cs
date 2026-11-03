using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace Board.Mcp.Tests;

public class StdioProcessTests
{
    [Fact]
    public async Task Server_StartsWithTheApiDownAndAnswersToolCallsLoudly()
    {
        var url = ClosedPort.Url();
        var transport = new StdioClientTransport(new StdioClientTransportOptions
        {
            Name = "board",
            Command = "dotnet",
            Arguments = [Path.Combine(AppContext.BaseDirectory, "Board.Mcp.dll")],
            EnvironmentVariables = new Dictionary<string, string?> { ["BOARD_API_URL"] = url }
        });

        await using var client = await McpClient.CreateAsync(transport);

        var tools = await client.ListToolsAsync();
        Assert.Equal(
            ["create_card", "get_card", "list_cards", "move_card", "sync_card", "update_card"],
            tools.Select(t => t.Name).Order());

        var result = await client.CallToolAsync("get_card", new Dictionary<string, object?> { ["key"] = "BRD-1" });

        Assert.True(result.IsError);
        var text = ((TextContentBlock)result.Content[0]).Text;
        Assert.Contains("[unreachable] get_card", text);
        Assert.Contains(url, text);
    }
}
