using System.Text.Json.Nodes;
using Board.Contracts;

namespace Board.Mcp;

public static class PatchBodyBuilder
{
    public static JsonObject Build(string? title, string? description, CardStatus? status, string? phase, string? pendingGate)
    {
        var body = new JsonObject();
        if (title is not null)
            body["title"] = title;
        if (description is not null)
            body["description"] = description;
        if (status is not null)
            body["status"] = status.Value.ToString();
        if (phase is not null)
            body["phase"] = BlankToNull(phase);
        if (pendingGate is not null)
            body["pendingGate"] = BlankToNull(pendingGate);
        return body;
    }

    public static string? BlankToNull(string? value) => string.IsNullOrWhiteSpace(value) ? null : value;
}
