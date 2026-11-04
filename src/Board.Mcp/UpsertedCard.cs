using Board.Contracts;

namespace Board.Mcp;

public sealed record UpsertedCard(CardDto Card, bool Created);
