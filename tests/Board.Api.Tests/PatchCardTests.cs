using System.Net;
using Board.Contracts;

namespace Board.Api.Tests;

public sealed class PatchCardTests : IDisposable
{
    private readonly ApiFactory _factory = new();
    private readonly HttpClient _client;

    public PatchCardTests() => _client = _factory.CreateClient();

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
        _factory.DeleteDatabase();
    }

    [Theory]
    [InlineData("InProgress", CardStatus.InProgress)]
    [InlineData("Done", CardStatus.Done)]
    [InlineData("ToDo", CardStatus.ToDo)]
    public async Task Patch_Status_ChangesOnlyTheStatus(string status, CardStatus expected)
    {
        var created = await _client.CreateOkAsync(new { title = "One", description = "d", phase = "p" });

        var response = await _client.PatchAsync("BRD-1", $$"""{"status":"{{status}}"}""");
        var patched = await response.ReadCardAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(expected, patched.Status);
        Assert.Equal("One", patched.Title);
        Assert.Equal("d", patched.Description);
        Assert.Equal("p", patched.Phase);
        Assert.True(patched.UpdatedAt >= created.UpdatedAt);
    }

    [Fact]
    public async Task Patch_Done_ThenBackToToDo_IsAllowed()
    {
        await _client.CreateOkAsync(new { title = "One", status = "Done" });

        var patched = await (await _client.PatchAsync("BRD-1", """{"status":"ToDo"}""")).ReadCardAsync();

        Assert.Equal(CardStatus.ToDo, patched.Status);
    }

    [Fact]
    public async Task Patch_PhaseAndGate_AreSet()
    {
        await _client.CreateOkAsync(new { title = "One" });

        var patched = await (await _client.PatchAsync("BRD-1", """{"phase":"Phase 4.5","pendingGate":"plan"}""")).ReadCardAsync();

        Assert.Equal("Phase 4.5", patched.Phase);
        Assert.Equal("plan", patched.PendingGate);
    }

    [Fact]
    public async Task Patch_ExplicitNull_ClearsPhaseAndGate()
    {
        await _client.CreateOkAsync(new { title = "One", phase = "p", pendingGate = "g" });

        var patched = await (await _client.PatchAsync("BRD-1", """{"phase":null,"pendingGate":null}""")).ReadCardAsync();

        Assert.Null(patched.Phase);
        Assert.Null(patched.PendingGate);
    }

    [Fact]
    public async Task Patch_AbsentFields_AreLeftUnchanged()
    {
        await _client.CreateOkAsync(new { title = "One", phase = "p", pendingGate = "g" });

        var patched = await (await _client.PatchAsync("BRD-1", """{"title":"Renamed"}""")).ReadCardAsync();

        Assert.Equal("Renamed", patched.Title);
        Assert.Equal("p", patched.Phase);
        Assert.Equal("g", patched.PendingGate);
    }

    [Fact]
    public async Task Patch_TitleAndDescription_AreEdited()
    {
        await _client.CreateOkAsync(new { title = "One", description = "old" });

        var patched = await (await _client.PatchAsync("BRD-1", """{"title":"Two","description":"new"}""")).ReadCardAsync();

        Assert.Equal("Two", patched.Title);
        Assert.Equal("new", patched.Description);
    }

    [Fact]
    public async Task Patch_IsVisibleToALaterGet()
    {
        await _client.CreateOkAsync(new { title = "One" });
        await _client.PatchAsync("BRD-1", """{"status":"InProgress"}""");

        var response = await _client.GetAsync("/api/cards/BRD-1");

        Assert.Equal(CardStatus.InProgress, (await response.ReadCardAsync()).Status);
    }

    [Fact]
    public async Task Patch_UnknownKey_ReturnsNotFound()
    {
        var response = await _client.PatchAsync("BRD-9", """{"status":"Done"}""");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Theory]
    [InlineData("""{}""")]
    [InlineData("""{"unknown":1}""")]
    [InlineData("""{"status":"Blocked"}""")]
    [InlineData("""{"status":1}""")]
    [InlineData("""{"status":null}""")]
    [InlineData("""{"title":null}""")]
    [InlineData("""{"title":"  "}""")]
    [InlineData("""{"description":null}""")]
    [InlineData("""{"phase":5}""")]
    public async Task Patch_InvalidBody_ReturnsBadRequestAndChangesNothing(string body)
    {
        await _client.CreateOkAsync(new { title = "One", phase = "p" });

        var response = await _client.PatchAsync("BRD-1", body);
        var after = await (await _client.GetAsync("/api/cards/BRD-1")).ReadCardAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(CardStatus.ToDo, after.Status);
        Assert.Equal("One", after.Title);
        Assert.Equal("p", after.Phase);
    }

    [Theory]
    [InlineData("[]")]
    [InlineData("null")]
    [InlineData("\"text\"")]
    public async Task Patch_NonObjectBody_ReturnsBadRequest(string body)
    {
        await _client.CreateOkAsync(new { title = "One" });

        var response = await _client.PatchAsync("BRD-1", body);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
