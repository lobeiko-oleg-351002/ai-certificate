using Board.Contracts;

namespace Board.Api.Cards;

public static class CreateCardValidator
{
    public static Dictionary<string, string[]> Validate(CreateCardRequest request)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(request.Title))
            errors["title"] = ["Title is required."];
        else if (request.Title.Length > CardLimits.Title)
            errors["title"] = [$"Title must be at most {CardLimits.Title} characters."];

        if (request.Description is { Length: > CardLimits.Description })
            errors["description"] = [$"Description must be at most {CardLimits.Description} characters."];

        if (request.Key is not null && !CardKey.TryParse(request.Key, out _))
            errors["key"] = ["Key must look like BRD-1 (no leading zeros)."];

        if (request.Phase is { Length: > CardLimits.Label })
            errors["phase"] = [$"Phase must be at most {CardLimits.Label} characters."];

        if (request.PendingGate is { Length: > CardLimits.Label })
            errors["pendingGate"] = [$"PendingGate must be at most {CardLimits.Label} characters."];

        return errors;
    }
}
