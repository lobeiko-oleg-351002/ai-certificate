using System.Net;
using System.Net.Http.Json;
using Board.Contracts;

namespace Board.Api.Tests;

public sealed class ConcurrencyTests : IDisposable
{
    private const int Writers = 8;

    private readonly ApiFactory _factory = new();
    private readonly HttpClient _client;

    public ConcurrencyTests() => _client = _factory.CreateClient();

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
        _factory.DeleteDatabase();
    }

    [Fact]
    public async Task ParallelCreatesWithoutKey_AllocateDistinctKeys()
    {
        var responses = await Task.WhenAll(Enumerable.Range(0, Writers)
            .Select(i => Task.Run(() => _client.CreateAsync(new { title = $"Card {i}" }))));

        Assert.All(responses, r => Assert.Equal(HttpStatusCode.Created, r.StatusCode));
        var keys = (await Task.WhenAll(responses.Select(r => r.ReadCardAsync()))).Select(c => c.Key).ToList();
        Assert.Equal(Writers, keys.Distinct().Count());
        var all = await _client.GetFromJsonAsync<List<CardDto>>("/api/cards", TestJson.Options);
        Assert.Equal(Writers, all!.Count);
    }

    [Fact]
    public async Task ParallelUpsertsOfOneKey_ConvergeOnOneCardWithOneCreate()
    {
        var responses = await Task.WhenAll(Enumerable.Range(0, Writers)
            .Select(i => Task.Run(() => _client.UpsertAsync("BRD-3", new { title = $"Writer {i}", status = "InProgress" }))));

        Assert.All(responses, r => Assert.True(r.StatusCode is HttpStatusCode.Created or HttpStatusCode.OK, r.StatusCode.ToString()));
        Assert.Equal(1, responses.Count(r => r.StatusCode == HttpStatusCode.Created));
        var all = await _client.GetFromJsonAsync<List<CardDto>>("/api/cards", TestJson.Options);
        Assert.Single(all!);
    }
}
