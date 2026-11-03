namespace Board.Api.Cards;

public sealed record PatchReadResult(CardPatch? Patch, Dictionary<string, string[]> Errors);
