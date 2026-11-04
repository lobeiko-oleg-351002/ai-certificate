namespace Board.Contracts;

public sealed record UpsertCardRequest(
    string? Title,
    CardStatus? Status,
    string? Description = null,
    string? Phase = null,
    string? PendingGate = null);
