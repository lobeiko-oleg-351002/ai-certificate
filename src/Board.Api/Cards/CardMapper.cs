using Board.Contracts;

namespace Board.Api.Cards;

public static class CardMapper
{
    public static CardDto ToDto(this Card card) => new(
        card.Key,
        card.Title,
        card.Description,
        card.Status,
        card.Phase,
        card.PendingGate,
        card.CreatedAt,
        card.UpdatedAt);
}
