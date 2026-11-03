using Board.Contracts;

namespace Board.Api.Cards;

public sealed record CardPatch(
    string? Title,
    string? Description,
    CardStatus? Status,
    bool PhaseSpecified,
    string? Phase,
    bool PendingGateSpecified,
    string? PendingGate);
