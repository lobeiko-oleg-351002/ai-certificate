using Board.Contracts;

namespace Board.Mcp;

public sealed class CardSyncService(BoardClient board)
{
    public async Task<BoardResult<SyncOutcome>> SyncAsync(SyncRequest request, CancellationToken ct)
    {
        var existing = await board.GetAsync(request.Key, ct);
        if (existing.Error is null)
            return await UpdateAsync(request, ct);
        if (existing.Error.Kind != BoardErrorKind.NotFound)
            return BoardResult<SyncOutcome>.Fail(existing.Error with { Applied = AppliedState.No });

        var created = await board.CreateAsync(new CreateCardRequest(
            request.Title, request.Description, request.Key, request.Status,
            PatchBodyBuilder.BlankToNull(request.Phase), PatchBodyBuilder.BlankToNull(request.PendingGate)), ct);
        if (created.Error is { Kind: BoardErrorKind.Conflict })
        {
            var retried = await UpdateAsync(request, ct);
            return retried.Error is null
                ? retried
                : BoardResult<SyncOutcome>.Fail(retried.Error with
                {
                    Detail = $"POST answered 409, then the PATCH retry failed: {retried.Error.Detail}"
                });
        }
        return Outcome("created", created);
    }

    private async Task<BoardResult<SyncOutcome>> UpdateAsync(SyncRequest request, CancellationToken ct)
    {
        var body = PatchBodyBuilder.Build(request.Title, request.Description, request.Status, request.Phase, request.PendingGate);
        return Outcome("updated", await board.PatchAsync(request.Key, body, ct));
    }

    private static BoardResult<SyncOutcome> Outcome(string action, BoardResult<CardDto> result) =>
        result.Error is null
            ? BoardResult<SyncOutcome>.Ok(new SyncOutcome(action, result.Value!))
            : BoardResult<SyncOutcome>.Fail(result.Error);
}
