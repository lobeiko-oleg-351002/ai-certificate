namespace Board.Contracts;

public sealed record CardDto(
    string Key,
    string Title,
    string Description,
    CardStatus Status,
    string? Phase,
    string? PendingGate,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
