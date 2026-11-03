using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using Board.Contracts;

namespace Board.Mcp;

public sealed class BoardClient(HttpClient http, Func<string?> apiUrl)
{
    public const string DefaultUrl = "http://localhost:5080";
    private const int DetailLimit = 500;

    public Task<BoardResult<IReadOnlyList<CardDto>>> ListAsync(CancellationToken ct) =>
        SendAsync<IReadOnlyList<CardDto>>(HttpMethod.Get, "/api/cards", null, ct);

    public Task<BoardResult<CardDto>> GetAsync(string key, CancellationToken ct) =>
        SendAsync<CardDto>(HttpMethod.Get, CardPath(key), null, ct);

    public Task<BoardResult<CardDto>> CreateAsync(CreateCardRequest request, CancellationToken ct) =>
        SendAsync<CardDto>(HttpMethod.Post, "/api/cards", request, ct);

    public Task<BoardResult<CardDto>> PatchAsync(string key, JsonObject body, CancellationToken ct) =>
        SendAsync<CardDto>(HttpMethod.Patch, CardPath(key), body, ct);

    private static string CardPath(string key) => $"/api/cards/{Uri.EscapeDataString(key)}";

    private async Task<BoardResult<T>> SendAsync<T>(HttpMethod method, string path, object? body, CancellationToken ct)
    {
        var write = method != HttpMethod.Get;
        var raw = apiUrl();
        var api = string.IsNullOrWhiteSpace(raw) ? DefaultUrl : raw.Trim();
        if (!Uri.TryCreate(api, UriKind.Absolute, out var baseUri) || baseUri.Scheme is not ("http" or "https"))
            return Fail<T>(BoardErrorKind.Config, "BOARD_API_URL is not an absolute http or https URL.", api, null, null,
                write ? AppliedState.No : AppliedState.NotApplicable);

        api = Redact(baseUri);
        using var request = new HttpRequestMessage(method, new Uri(baseUri.AbsoluteUri.TrimEnd('/') + path));
        if (body is not null)
            request.Content = JsonContent.Create(body, body.GetType(), options: BoardJson.Options);

        var gotResponse = false;
        try
        {
            using var response = await http.SendAsync(request, ct);
            gotResponse = true;
            var text = await response.Content.ReadAsStringAsync(ct);
            return response.IsSuccessStatusCode
                ? ReadSuccess<T>(text, api, (int)response.StatusCode, write)
                : ReadFailure<T>(text, api, response.StatusCode, write);
        }
        catch (HttpRequestException ex) when (!gotResponse)
        {
            var neverSent = ex.HttpRequestError is HttpRequestError.ConnectionError
                or HttpRequestError.NameResolutionError or HttpRequestError.SecureConnectionError;
            return Fail<T>(BoardErrorKind.Unreachable, "Cannot reach the board API.", api, null, ex.Message,
                !write ? AppliedState.NotApplicable : neverSent ? AppliedState.No : AppliedState.Unknown);
        }
        catch (HttpRequestException ex)
        {
            return Fail<T>(BoardErrorKind.BadResponse, "The connection broke while reading the API response.", api, null,
                ex.Message, Uncertain(write));
        }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested)
        {
            return Fail<T>(BoardErrorKind.Timeout, $"The board API did not answer within {http.Timeout.TotalSeconds:0} s.", api,
                null, null, Uncertain(write));
        }
    }

    private static BoardResult<T> ReadSuccess<T>(string text, string api, int status, bool write)
    {
        try
        {
            var value = JsonSerializer.Deserialize<T>(text, BoardJson.Options);
            if (value is not null)
                return BoardResult<T>.Ok(value);
        }
        catch (JsonException ex)
        {
            return Fail<T>(BoardErrorKind.BadResponse, "The API answered 2xx with a body that is not the expected card JSON.", api,
                status, $"{ex.Message} Body: {Truncate(text)}", Uncertain(write));
        }
        return Fail<T>(BoardErrorKind.BadResponse, "The API answered 2xx with an empty JSON body.", api,
            status, Truncate(text), Uncertain(write));
    }

    private static BoardResult<T> ReadFailure<T>(string text, string api, HttpStatusCode status, bool write)
    {
        var kind = status switch
        {
            HttpStatusCode.BadRequest => BoardErrorKind.Validation,
            HttpStatusCode.NotFound => BoardErrorKind.NotFound,
            HttpStatusCode.Conflict => BoardErrorKind.Conflict,
            _ => BoardErrorKind.ApiError
        };
        var message = kind switch
        {
            BoardErrorKind.Validation => "The board API rejected the request.",
            BoardErrorKind.NotFound => "The card was not found.",
            BoardErrorKind.Conflict => "A card with this key already exists.",
            _ => $"The board API answered {(int)status}."
        };
        var applied = !write ? AppliedState.NotApplicable
            : (int)status >= 500 ? AppliedState.Unknown : AppliedState.No;
        return Fail<T>(kind, message, api, (int)status, ProblemDetail(text), applied);
    }

    private static string ProblemDetail(string text)
    {
        try
        {
            using var doc = JsonDocument.Parse(text);
            var root = doc.RootElement;
            var parts = new List<string>();
            if (root.TryGetProperty("title", out var title) && title.ValueKind == JsonValueKind.String)
                parts.Add(title.GetString()!);
            if (root.TryGetProperty("errors", out var errors) && errors.ValueKind == JsonValueKind.Object)
                parts.AddRange(errors.EnumerateObject().Select(e => $"{e.Name}: {e.Value}"));
            if (parts.Count > 0)
                return Truncate(string.Join("; ", parts));
        }
        catch (JsonException)
        {
            return Truncate(text);
        }
        return Truncate(text);
    }

    private static string Redact(Uri uri) => string.IsNullOrEmpty(uri.UserInfo)
        ? uri.AbsoluteUri.TrimEnd('/')
        : new UriBuilder(uri) { UserName = "", Password = "" }.Uri.AbsoluteUri.TrimEnd('/');

    private static AppliedState Uncertain(bool write) => write ? AppliedState.Unknown : AppliedState.NotApplicable;

    private static string Truncate(string text) => text.Length <= DetailLimit ? text : text[..DetailLimit] + "...";

    private static BoardResult<T> Fail<T>(BoardErrorKind kind, string message, string api, int? status, string? detail, AppliedState applied) =>
        BoardResult<T>.Fail(new BoardError(kind, message, api, status, detail, applied));
}
