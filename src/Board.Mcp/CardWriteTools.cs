using System.ComponentModel;
using Board.Contracts;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace Board.Mcp;

[McpServerToolType]
public sealed class CardWriteTools(BoardClient board, CardSyncService sync)
{
    [McpServerTool(Name = "create_card"), Description("Create a card. Without key the board allocates the next BRD-n; an explicit key is for catch-up.")]
    public async Task<CallToolResult> CreateCard(
        [Description("Card title, required.")] string title,
        [Description("Card description.")] string? description = null,
        [Description("Explicit key BRD-n, optional.")] string? key = null,
        [Description("ToDo, InProgress or Done; default ToDo.")] string? status = null,
        [Description("Free-text phase copied from STATE.md.")] string? phase = null,
        [Description("Pending human gate, free text.")] string? pendingGate = null,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(title))
            return ToolResults.InvalidArgument("create_card", "title is required.");
        if (!TryStatus("create_card", status, out var parsed, out var invalid))
            return invalid!;

        var request = new CreateCardRequest(title, description, PatchBodyBuilder.BlankToNull(key), parsed,
            PatchBodyBuilder.BlankToNull(phase), PatchBodyBuilder.BlankToNull(pendingGate));
        return ToolResults.From("create_card", await board.CreateAsync(request, ct));
    }

    [McpServerTool(Name = "move_card"), Description("Move a card to another column by setting its status.")]
    public async Task<CallToolResult> MoveCard(
        [Description("Card key, BRD-n.")] string key,
        [Description("ToDo, InProgress or Done.")] string status,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(key))
            return ToolResults.InvalidArgument("move_card", "key is required.");
        if (!TryStatus("move_card", status, out var parsed, out var invalid) || parsed is null)
            return invalid ?? ToolResults.InvalidArgument("move_card", $"status is required: {StatusArgument.Allowed}.");

        var body = PatchBodyBuilder.Build(null, null, parsed, null, null);
        return ToolResults.From("move_card", await board.PatchAsync(key, body, ct));
    }

    [McpServerTool(Name = "update_card"), Description("Update card fields. Omitted fields stay unchanged; an empty string clears phase or pendingGate.")]
    public async Task<CallToolResult> UpdateCard(
        [Description("Card key, BRD-n.")] string key,
        [Description("New title.")] string? title = null,
        [Description("New description; empty string empties it.")] string? description = null,
        [Description("New phase; empty string clears it.")] string? phase = null,
        [Description("New pending gate; empty string clears it.")] string? pendingGate = null,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(key))
            return ToolResults.InvalidArgument("update_card", "key is required.");

        var body = PatchBodyBuilder.Build(title, description, null, phase, pendingGate);
        return body.Count == 0
            ? ToolResults.InvalidArgument("update_card", "provide at least one of: title, description, phase, pendingGate.")
            : ToolResults.From("update_card", await board.PatchAsync(key, body, ct));
    }

    [McpServerTool(Name = "sync_card"), Description("Idempotent upsert for catch-up: creates the card with this key if missing, otherwise updates it to the given values.")]
    public async Task<CallToolResult> SyncCard(
        [Description("Card key, BRD-n.")] string key,
        [Description("Card title.")] string title,
        [Description("ToDo, InProgress or Done.")] string status,
        [Description("Card description.")] string? description = null,
        [Description("Phase; empty string clears it.")] string? phase = null,
        [Description("Pending gate; empty string clears it.")] string? pendingGate = null,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(title))
            return ToolResults.InvalidArgument("sync_card", "key and title are required.");
        if (!TryStatus("sync_card", status, out var parsed, out var invalid) || parsed is null)
            return invalid ?? ToolResults.InvalidArgument("sync_card", $"status is required: {StatusArgument.Allowed}.");

        var request = new SyncRequest(key, title, parsed.Value, description, phase, pendingGate);
        return ToolResults.From("sync_card", await sync.SyncAsync(request, ct));
    }

    private static bool TryStatus(string tool, string? text, out CardStatus? status, out CallToolResult? invalid)
    {
        status = null;
        invalid = null;
        if (text is null)
            return true;
        if (StatusArgument.TryParse(text, out var parsed))
        {
            status = parsed;
            return true;
        }
        invalid = ToolResults.InvalidArgument(tool, $"status must be one of: {StatusArgument.Allowed}.");
        return false;
    }
}
