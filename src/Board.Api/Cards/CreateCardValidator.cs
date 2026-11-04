using Board.Contracts;

namespace Board.Api.Cards;

public static class CreateCardValidator
{
    public static Dictionary<string, string[]> Validate(CreateCardRequest request)
    {
        var errors = new Dictionary<string, string[]>();
        CardFieldRules.CheckText(errors, request.Title, request.Description, request.Phase, request.PendingGate);

        if (request.Key is not null && !CardKey.TryParse(request.Key, out _))
            errors["key"] = ["Key must look like BRD-1 (no leading zeros)."];

        return errors;
    }
}
