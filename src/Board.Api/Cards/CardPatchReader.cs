using System.Text.Json.Nodes;
using Board.Contracts;

namespace Board.Api.Cards;

public static class CardPatchReader
{
    private static readonly string[] Known = ["title", "description", "status", "phase", "pendingGate"];

    public static PatchReadResult Read(JsonObject? body)
    {
        var errors = new Dictionary<string, string[]>();
        if (body is null || !Known.Any(body.ContainsKey))
        {
            errors["body"] = [$"Provide at least one of: {string.Join(", ", Known)}."];
            return new PatchReadResult(null, errors);
        }

        var title = ReadText(body, "title", CardLimits.Title, nullable: false, errors);
        var description = ReadText(body, "description", CardLimits.Description, nullable: false, errors);
        var phase = ReadText(body, "phase", CardLimits.Label, nullable: true, errors);
        var gate = ReadText(body, "pendingGate", CardLimits.Label, nullable: true, errors);
        var status = ReadStatus(body, errors);

        if (title is not null && string.IsNullOrWhiteSpace(title))
            errors["title"] = ["title must not be empty."];

        if (errors.Count > 0)
            return new PatchReadResult(null, errors);

        var patch = new CardPatch(
            title, description, status,
            body.ContainsKey("phase"), phase,
            body.ContainsKey("pendingGate"), gate);
        return new PatchReadResult(patch, errors);
    }

    private static string? ReadText(JsonObject body, string name, int max, bool nullable, Dictionary<string, string[]> errors)
    {
        if (!body.TryGetPropertyValue(name, out var node))
            return null;
        if (node is null)
        {
            if (!nullable)
                errors[name] = [$"{name} cannot be null."];
            return null;
        }
        if (node is not JsonValue value || !value.TryGetValue<string>(out var text))
        {
            errors[name] = [$"{name} must be a string."];
            return null;
        }
        if (text.Length > max)
            errors[name] = [$"{name} must be at most {max} characters."];
        return text;
    }

    private static CardStatus? ReadStatus(JsonObject body, Dictionary<string, string[]> errors)
    {
        if (!body.TryGetPropertyValue("status", out var node))
            return null;
        if (node is JsonValue value && value.TryGetValue<string>(out var text)
            && Enum.GetNames<CardStatus>().Any(n => string.Equals(n, text, StringComparison.OrdinalIgnoreCase)))
            return Enum.Parse<CardStatus>(text, ignoreCase: true);

        errors["status"] = [$"status must be one of: {string.Join(", ", Enum.GetNames<CardStatus>())}."];
        return null;
    }
}
