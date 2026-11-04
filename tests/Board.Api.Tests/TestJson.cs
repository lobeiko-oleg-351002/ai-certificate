using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Board.Contracts;

namespace Board.Api.Tests;

public static class TestJson
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public static async Task<HttpResponseMessage> CreateAsync(this HttpClient client, object body) =>
        await client.PostAsJsonAsync("/api/cards", body, Options);

    public static async Task<CardDto> CreateOkAsync(this HttpClient client, object body)
    {
        var response = await client.CreateAsync(body);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CardDto>(Options))!;
    }

    public static async Task<HttpResponseMessage> UpsertAsync(this HttpClient client, string key, object body) =>
        await client.PutAsJsonAsync($"/api/cards/{key}", body, Options);

    public static async Task<HttpResponseMessage> PatchAsync(this HttpClient client, string key, string rawJson) =>
        await client.PatchAsync($"/api/cards/{key}", new StringContent(rawJson, Encoding.UTF8, "application/json"));

    public static async Task<CardDto> ReadCardAsync(this HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<CardDto>(Options))!;
}
