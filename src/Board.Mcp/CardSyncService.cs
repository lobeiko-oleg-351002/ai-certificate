using Board.Contracts;

namespace Board.Mcp;

public sealed class CardSyncService(BoardClient board)
{
    public async Task<BoardResult<SyncOutcome>> SyncAsync(SyncRequest request, CancellationToken ct)
    {
        var upsert = new UpsertCardRequest(
            request.Title, request.Status, request.Description,
            PatchBodyBuilder.BlankToNull(request.Phase), PatchBodyBuilder.BlankToNull(request.PendingGate));

        var result = await board.UpsertAsync(request.Key, upsert, ct);
        return result.Error is null
            ? BoardResult<SyncOutcome>.Ok(new SyncOutcome(result.Value!.Created ? "created" : "updated", result.Value.Card))
            : BoardResult<SyncOutcome>.Fail(result.Error);
    }
}
