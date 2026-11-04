using System.Net;
using Board.Contracts;

namespace Board.Api.Tests;

public sealed class UpsertCardTests : IDisposable
{
    private readonly ApiFactory _factory = new();
    private readonly HttpClient _client;

    public UpsertCardTests() => _client = _factory.CreateClient();

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
        _factory.DeleteDatabase();
    }

    [Fact]
    public async Task Upsert_OfAnUnknownKey_CreatesWithLocation()
    {
        var response = await _client.UpsertAsync("BRD-4", new { title = "Old", status = "Done", phase = "handover", pendingGate = "plan" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal("/api/cards/BRD-4", response.Headers.Location!.OriginalString);
        var card = await response.ReadCardAsync();
        Assert.Equal("BRD-4", card.Key);
        Assert.Equal(CardStatus.Done, card.Status);
        Assert.Equal("handover", card.Phase);
        Assert.Equal("plan", card.PendingGate);
    }

    [Fact]
    public async Task Upsert_OfAnUnknownKey_DoesNotDisturbNextAllocation()
    {
        await _client.UpsertAsync("BRD-4", new { title = "Old", status = "Done" });

        var next = await _client.CreateOkAsync(new { title = "New" });

        Assert.Equal("BRD-5", next.Key);
    }

    [Fact]
    public async Task Upsert_OfAnExistingKey_ReplacesAndReturnsOk()
    {
        await _client.UpsertAsync("BRD-4", new { title = "Old", status = "ToDo", description = "d" });

        var response = await _client.UpsertAsync("BRD-4", new { title = "New", status = "InProgress", phase = "implement" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var card = await response.ReadCardAsync();
        Assert.Equal("New", card.Title);
        Assert.Equal(CardStatus.InProgress, card.Status);
        Assert.Equal("implement", card.Phase);
    }

    [Fact]
    public async Task Upsert_OfAnExistingKey_ClearsOmittedOptionalFieldsAndKeepsCreatedAt()
    {
        var created = await (await _client.UpsertAsync("BRD-4", new
        {
            title = "Old", status = "ToDo", description = "d", phase = "plan", pendingGate = "plan"
        })).ReadCardAsync();
        await Task.Delay(50);

        var replaced = await (await _client.UpsertAsync("BRD-4", new { title = "Old", status = "ToDo" })).ReadCardAsync();

        Assert.Equal("", replaced.Description);
        Assert.Null(replaced.Phase);
        Assert.Null(replaced.PendingGate);
        Assert.Equal(created.CreatedAt, replaced.CreatedAt);
        Assert.True(replaced.UpdatedAt > created.UpdatedAt);
    }

    [Fact]
    public async Task Upsert_Result_IsVisibleThroughGet()
    {
        await _client.UpsertAsync("BRD-9", new { title = "T", status = "InProgress", phase = "verify" });

        var fetched = await (await _client.GetAsync("/api/cards/BRD-9")).ReadCardAsync();

        Assert.Equal("verify", fetched.Phase);
    }

    [Theory]
    [InlineData("BRD-001")]
    [InlineData("BRD-0")]
    [InlineData("brd-1")]
    [InlineData("PRJ-1")]
    [InlineData("BRD-1234567890")]
    public async Task Upsert_WithMalformedKey_ReturnsBadRequest(string key)
    {
        var response = await _client.UpsertAsync(key, new { title = "T", status = "ToDo" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Upsert_WithBlankTitle_ReturnsBadRequest(string title)
    {
        var response = await _client.UpsertAsync("BRD-1", new { title, status = "ToDo" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Upsert_WithoutStatus_ReturnsProblemNamingStatus()
    {
        var response = await _client.UpsertAsync("BRD-1", new { title = "T" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.Contains("status", await response.Content.ReadAsStringAsync());
    }

    [Theory]
    [InlineData("Blocked")]
    [InlineData("1")]
    public async Task Upsert_WithUnknownStatus_ReturnsBadRequest(string status)
    {
        var response = await _client.UpsertAsync("BRD-1", new { title = "T", status });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Upsert_WithTooLongPhase_ReturnsBadRequest()
    {
        var response = await _client.UpsertAsync("BRD-1", new { title = "T", status = "ToDo", phase = new string('x', 101) });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Upsert_Rejected_StoresNothing()
    {
        await _client.UpsertAsync("BRD-1", new { title = "", status = "ToDo" });

        var response = await _client.GetAsync("/api/cards/BRD-1");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
