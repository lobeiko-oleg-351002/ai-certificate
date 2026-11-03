using Board.Contracts;

namespace Board.Mcp;

public sealed record SyncOutcome(string Action, CardDto Card);
