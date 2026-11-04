namespace Board.Api.Cards;

public static class CardFieldRules
{
    public static void CheckText(
        Dictionary<string, string[]> errors, string? title, string? description, string? phase, string? pendingGate)
    {
        if (string.IsNullOrWhiteSpace(title))
            errors["title"] = ["Title is required."];
        else if (title.Length > CardLimits.Title)
            errors["title"] = [$"Title must be at most {CardLimits.Title} characters."];

        if (description is { Length: > CardLimits.Description })
            errors["description"] = [$"Description must be at most {CardLimits.Description} characters."];

        if (phase is { Length: > CardLimits.Label })
            errors["phase"] = [$"Phase must be at most {CardLimits.Label} characters."];

        if (pendingGate is { Length: > CardLimits.Label })
            errors["pendingGate"] = [$"PendingGate must be at most {CardLimits.Label} characters."];
    }
}
