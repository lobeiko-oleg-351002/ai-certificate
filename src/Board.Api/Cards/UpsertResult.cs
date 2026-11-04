using Board.Contracts;

namespace Board.Api.Cards;

public sealed record UpsertResult(CardDto Card, bool Created);
