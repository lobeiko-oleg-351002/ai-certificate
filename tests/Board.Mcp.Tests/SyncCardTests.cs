using System.Net;
using System.Text.Json.Nodes;

namespace Board.Mcp.Tests;

public class SyncCardTests
{
    private static string ActionOf(ModelContextProtocol.Protocol.CallToolResult result) =>
        JsonNode.Parse(Harness.TextOf(result))!["action"]!.GetValue<string>();

    [Fact]
    public async Task SyncCard_IsOneWriteAndNeverReadsFirst()
    {
        var handler = Harness.Always(HttpStatusCode.OK, Harness.CardJson);

        await Harness.For(handler).Write.SyncCard("BRD-7", "T", "Done", "D", "verify", "plan", default);

        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Put, request.Method);
        Assert.Equal("/api/cards/BRD-7", request.Uri.AbsolutePath);
    }

    [Fact]
    public async Task SyncCard_SendsEveryGivenFieldInTheBody()
    {
        var handler = Harness.Always(HttpStatusCode.OK, Harness.CardJson);

        await Harness.For(handler).Write.SyncCard("BRD-7", "T", "Done", "D", "verify", "plan", default);

        Assert.Equal("""{"title":"T","status":"Done","description":"D","phase":"verify","pendingGate":"plan"}""", handler.Requests[0].Body);
    }

    [Fact]
    public async Task SyncCard_BlankPhaseAndGate_AreSentAsNothing()
    {
        var handler = Harness.Always(HttpStatusCode.OK, Harness.CardJson);

        await Harness.For(handler).Write.SyncCard("BRD-7", "T", "ToDo", phase: "", pendingGate: "  ", ct: default);

        Assert.Equal("""{"title":"T","status":"ToDo"}""", handler.Requests[0].Body);
    }

    [Fact]
    public async Task SyncCard_Answer201_IsCreated()
    {
        var handler = Harness.Always(HttpStatusCode.Created, Harness.CardJson);

        var result = await Harness.For(handler).Write.SyncCard("BRD-7", "T", "ToDo", ct: default);

        Assert.NotEqual(true, result.IsError);
        Assert.Equal("created", ActionOf(result));
    }

    [Fact]
    public async Task SyncCard_Answer200_IsUpdated()
    {
        var handler = Harness.Always(HttpStatusCode.OK, Harness.CardJson);

        var result = await Harness.For(handler).Write.SyncCard("BRD-7", "T", "ToDo", ct: default);

        Assert.NotEqual(true, result.IsError);
        Assert.Equal("updated", ActionOf(result));
    }
}
