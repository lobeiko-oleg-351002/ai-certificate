using System.Net;
using System.Net.Http.Json;
using Board.Contracts;

namespace Board.Api.Tests;

public sealed class ReadCardTests : IDisposable
{
    private readonly ApiFactory _factory = new();
    private readonly HttpClient _client;

    public ReadCardTests() => _client = _factory.CreateClient();

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
        _factory.DeleteDatabase();
    }

    [Fact]
    public async Task List_WhenEmpty_ReturnsEmptyArray()
    {
        var cards = await _client.GetFromJsonAsync<CardDto[]>("/api/cards", TestJson.Options);

        Assert.Empty(cards!);
    }

    [Fact]
    public async Task List_OrdersByKeyNumberNotAlphabetically()
    {
        await _client.CreateOkAsync(new { title = "Ten", key = "BRD-10" });
        await _client.CreateOkAsync(new { title = "Two", key = "BRD-2" });

        var cards = await _client.GetFromJsonAsync<CardDto[]>("/api/cards", TestJson.Options);

        Assert.Equal(["BRD-2", "BRD-10"], cards!.Select(c => c.Key));
    }

    [Fact]
    public async Task Get_ReturnsTheCardWithAllFields()
    {
        await _client.CreateOkAsync(new { title = "Board api", description = "Build it", phase = "Phase 5", pendingGate = "plan" });

        var card = await _client.GetFromJsonAsync<CardDto>("/api/cards/BRD-1", TestJson.Options);

        Assert.Equal("Board api", card!.Title);
        Assert.Equal("Build it", card.Description);
        Assert.Equal(CardStatus.ToDo, card.Status);
        Assert.Equal("Phase 5", card.Phase);
        Assert.Equal("plan", card.PendingGate);
    }

    [Fact]
    public async Task Get_Json_SerialisesStatusAsString()
    {
        await _client.CreateOkAsync(new { title = "One" });

        var json = await _client.GetStringAsync("/api/cards/BRD-1");

        Assert.Contains("\"status\":\"ToDo\"", json);
    }

    [Fact]
    public async Task Get_UnknownKey_ReturnsNotFoundProblem()
    {
        var response = await _client.GetAsync("/api/cards/BRD-99");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }
}
