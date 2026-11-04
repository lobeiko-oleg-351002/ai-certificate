using Board.Contracts;

namespace Board.Api.Cards;

public static class UpsertCardValidator
{
    public static Dictionary<string, string[]> Validate(string key, UpsertCardRequest request)
    {
        var errors = new Dictionary<string, string[]>();
        CardFieldRules.CheckText(errors, request.Title, request.Description, request.Phase, request.PendingGate);

        if (!CardKey.TryParse(key, out _))
            errors["key"] = ["Key must look like BRD-1 (no leading zeros)."];

        if (request.Status is null)
            errors["status"] = [$"Status is required: {string.Join(", ", Enum.GetNames<CardStatus>())}."];

        return errors;
    }
}
