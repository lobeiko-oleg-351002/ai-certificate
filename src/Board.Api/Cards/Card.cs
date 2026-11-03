using Board.Contracts;

namespace Board.Api.Cards;

public sealed class Card
{
    public required string Key { get; init; }
    public required int Number { get; init; }
    public required string Title { get; set; }
    public string Description { get; set; } = "";
    public CardStatus Status { get; set; }
    public string? Phase { get; set; }
    public string? PendingGate { get; set; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; set; }
}
