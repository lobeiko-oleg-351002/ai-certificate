using System.Net;
using System.Text.Json.Nodes;

namespace Board.Mcp.Tests;

public class ToolRequestShapeTests
{
    [Fact]
    public async Task ListCards_GetsCardsAndReturnsTheArray()
    {
        var handler = Harness.Always(HttpStatusCode.OK, $"[{Harness.CardJson}]");

        var result = await Harness.For(handler).Read.ListCards(default);

        Assert.True(result.IsError is not true);
        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Get, request.Method);
        Assert.Equal("http://board.test/api/cards", request.Uri.ToString());
        Assert.Equal("BRD-7", JsonNode.Parse(Harness.TextOf(result))![0]!["key"]!.GetValue<string>());
    }

    [Fact]
    public async Task GetCard_EscapesTheKeyInThePath()
    {
        var handler = Harness.Always(HttpStatusCode.NotFound, "{}");

        await Harness.For(handler).Read.GetCard("../secret", default);

        Assert.Equal("/api/cards/..%2Fsecret", Assert.Single(handler.Requests).Uri.AbsolutePath);
    }

    [Fact]
    public async Task MoveCard_PatchesOnlyTheStatus()
    {
        var handler = Harness.Always(HttpStatusCode.OK, Harness.CardJson);

        await Harness.For(handler).Write.MoveCard("BRD-7", "inprogress", default);

        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Patch, request.Method);
        Assert.Equal("/api/cards/BRD-7", request.Uri.AbsolutePath);
        Assert.Equal("""{"status":"InProgress"}""", request.Body);
    }

    [Fact]
    public async Task UpdateCard_SendsOnlyTheSuppliedFields()
    {
        var handler = Harness.Always(HttpStatusCode.OK, Harness.CardJson);

        await Harness.For(handler).Write.UpdateCard("BRD-7", title: "New", phase: "Phase 5", ct: default);

        Assert.Equal("""{"title":"New","phase":"Phase 5"}""", Assert.Single(handler.Requests).Body);
    }

    [Fact]
    public async Task UpdateCard_EmptyPhaseAndGateSendExplicitNull()
    {
        var handler = Harness.Always(HttpStatusCode.OK, Harness.CardJson);

        await Harness.For(handler).Write.UpdateCard("BRD-7", phase: "", pendingGate: "", ct: default);

        Assert.Equal("""{"phase":null,"pendingGate":null}""", Assert.Single(handler.Requests).Body);
    }

    [Fact]
    public async Task UpdateCard_EmptyDescriptionIsPassedThrough()
    {
        var handler = Harness.Always(HttpStatusCode.OK, Harness.CardJson);

        await Harness.For(handler).Write.UpdateCard("BRD-7", description: "", ct: default);

        Assert.Equal("""{"description":""}""", Assert.Single(handler.Requests).Body);
    }

    [Fact]
    public async Task UpdateCard_NoFields_FailsWithoutARequest()
    {
        var handler = Harness.Always(HttpStatusCode.OK, Harness.CardJson);

        var result = await Harness.For(handler).Write.UpdateCard("BRD-7", ct: default);

        Assert.True(result.IsError);
        Assert.Contains("[invalid_argument]", Harness.TextOf(result));
        Assert.Empty(handler.Requests);
    }

    [Fact]
    public async Task MoveCard_UnknownStatus_FailsWithoutARequest()
    {
        var handler = Harness.Always(HttpStatusCode.OK, Harness.CardJson);

        var result = await Harness.For(handler).Write.MoveCard("BRD-7", "Finished", default);

        Assert.True(result.IsError);
        Assert.Contains("ToDo, InProgress, Done", Harness.TextOf(result));
        Assert.Empty(handler.Requests);
    }

    [Fact]
    public async Task CreateCard_SendsTheExplicitKeyAndOmitsUnsetFields()
    {
        var handler = Harness.Always(HttpStatusCode.Created, Harness.CardJson);

        await Harness.For(handler).Write.CreateCard("Title", key: "BRD-3", status: "InProgress", ct: default);

        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal("""{"title":"Title","key":"BRD-3","status":"InProgress"}""", request.Body);
    }
}
