using System.Net;
using System.Text.Json;
using Board.Contracts;
using ModelContextProtocol.Protocol;

namespace Board.Mcp.Tests;

public sealed class Harness(HttpClient http, string apiUrl = "http://board.test")
{
    public const string CardJson =
        """{"key":"BRD-7","title":"T","description":"D","status":"ToDo","phase":null,"pendingGate":null,"createdAt":"2026-11-03T10:00:00+00:00","updatedAt":"2026-11-03T10:00:00+00:00"}""";

    private readonly BoardClient _client = new(http, () => apiUrl);

    public CardReadTools Read => new(_client);

    public CardWriteTools Write => new(_client, new CardSyncService(_client));

    public static Harness For(FakeHandler handler, string apiUrl = "http://board.test") =>
        new(new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(10) }, apiUrl);

    public static FakeHandler Always(HttpStatusCode status, string json) => new(_ => FakeHandler.Json(status, json));

    public static string TextOf(CallToolResult result) => ((TextContentBlock)result.Content[0]).Text;

    public static CardDto CardOf(CallToolResult result) =>
        JsonSerializer.Deserialize<CardDto>(TextOf(result), BoardJson.Options)!;
}
