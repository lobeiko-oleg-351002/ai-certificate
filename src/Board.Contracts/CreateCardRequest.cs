namespace Board.Contracts;

public sealed record CreateCardRequest(
    string? Title,
    string? Description = null,
    string? Key = null,
    CardStatus? Status = null,
    string? Phase = null,
    string? PendingGate = null);
