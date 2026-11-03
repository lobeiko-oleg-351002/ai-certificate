using System.Net;
using Board.Contracts;

namespace Board.Api.Tests;

public sealed class CreateCardTests : IDisposable
{
    private readonly ApiFactory _factory = new();
    private readonly HttpClient _client;

    public CreateCardTests() => _client = _factory.CreateClient();

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
        _factory.DeleteDatabase();
    }

    [Fact]
    public async Task Create_WithoutKey_AllocatesSequentialKeysStartingAtOne()
    {
        var first = await _client.CreateOkAsync(new { title = "One" });
        var second = await _client.CreateOkAsync(new { title = "Two" });

        Assert.Equal("BRD-1", first.Key);
        Assert.Equal("BRD-2", second.Key);
    }

    [Fact]
    public async Task Create_Minimal_DefaultsToToDoWithEmptyDescription()
    {
        var card = await _client.CreateOkAsync(new { title = "One" });

        Assert.Equal(CardStatus.ToDo, card.Status);
        Assert.Equal("", card.Description);
        Assert.Null(card.Phase);
        Assert.Null(card.PendingGate);
    }

    [Fact]
    public async Task Create_ReturnsCreatedWithLocation()
    {
        var response = await _client.CreateAsync(new { title = "One" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal("/api/cards/BRD-1", response.Headers.Location!.OriginalString);
    }

    [Fact]
    public async Task Create_WithExplicitKey_UsesKeyAndContinuesAfterIt()
    {
        var explicitCard = await _client.CreateOkAsync(new { title = "Old", key = "BRD-7", status = "Done", phase = "Phase 8" });
        var next = await _client.CreateOkAsync(new { title = "New" });

        Assert.Equal("BRD-7", explicitCard.Key);
        Assert.Equal(CardStatus.Done, explicitCard.Status);
        Assert.Equal("Phase 8", explicitCard.Phase);
        Assert.Equal("BRD-8", next.Key);
    }

    [Fact]
    public async Task Create_WithDuplicateKey_ReturnsConflict()
    {
        await _client.CreateOkAsync(new { title = "One", key = "BRD-3" });

        var response = await _client.CreateAsync(new { title = "Again", key = "BRD-3" });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Theory]
    [InlineData("BRD-001")]
    [InlineData("BRD-0")]
    [InlineData("brd-1")]
    [InlineData("PRJ-1")]
    [InlineData("BRD-")]
    [InlineData("BRD-1234567890")]
    public async Task Create_WithMalformedKey_ReturnsBadRequest(string key)
    {
        var response = await _client.CreateAsync(new { title = "One", key });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Create_WithBlankTitle_ReturnsBadRequest(string title)
    {
        var response = await _client.CreateAsync(new { title });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_WithTooLongTitle_ReturnsBadRequest()
    {
        var response = await _client.CreateAsync(new { title = new string('x', 201) });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData("Blocked")]
    [InlineData("1")]
    public async Task Create_WithUnknownStatus_ReturnsBadRequestInsteadOfDefaulting(string status)
    {
        var response = await _client.CreateAsync(new { title = "One", status });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Create_WithMalformedJson_ReturnsBadRequest()
    {
        var response = await _client.PostAsync("/api/cards", new StringContent("{not json", System.Text.Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
