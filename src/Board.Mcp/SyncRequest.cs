using Board.Contracts;

namespace Board.Mcp;

public sealed record SyncRequest(
    string Key,
    string Title,
    CardStatus Status,
    string? Description,
    string? Phase,
    string? PendingGate);
