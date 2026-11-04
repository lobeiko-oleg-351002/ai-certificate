using Board.Api.Data;
using Board.Contracts;
using Microsoft.EntityFrameworkCore;

namespace Board.Api.Cards;

public sealed class CardService(BoardDbContext db, TimeProvider clock)
{
    private const int MaxAllocationAttempts = 10;
    private const int MaxUpsertAttempts = 10;

    public async Task<IReadOnlyList<CardDto>> ListAsync(CancellationToken ct)
    {
        var cards = await db.Cards.AsNoTracking().OrderBy(c => c.Number).ToListAsync(ct);
        return cards.ConvertAll(c => c.ToDto());
    }

    public async Task<CardResult> GetAsync(string key, CancellationToken ct)
    {
        var card = await db.Cards.AsNoTracking().FirstOrDefaultAsync(c => c.Key == key, ct);
        return card is null ? CardResult.Failed(CardError.NotFound) : CardResult.Ok(card.ToDto());
    }

    public async Task<CardResult> CreateAsync(CreateCardRequest request, CancellationToken ct)
    {
        var explicitKey = request.Key is not null;
        for (var attempt = 1; ; attempt++)
        {
            var number = explicitKey
                ? ParseExplicit(request.Key)
                : (await db.Cards.MaxAsync(c => (int?)c.Number, ct) ?? 0) + 1;
            var now = clock.GetUtcNow();
            var card = new Card
            {
                Key = CardKey.Format(number),
                Number = number,
                Title = request.Title!,
                Description = request.Description ?? "",
                Status = request.Status ?? CardStatus.ToDo,
                Phase = request.Phase,
                PendingGate = request.PendingGate,
                CreatedAt = now,
                UpdatedAt = now
            };
            db.Cards.Add(card);

            try
            {
                await db.SaveChangesAsync(ct);
                return CardResult.Ok(card.ToDto());
            }
            catch (DbUpdateException)
            {
                db.Entry(card).State = EntityState.Detached;
                if (explicitKey && await db.Cards.AnyAsync(c => c.Key == card.Key, ct))
                    return CardResult.Failed(CardError.Conflict);
                if (explicitKey || attempt >= MaxAllocationAttempts)
                    throw;
            }
        }
    }

    private static int ParseExplicit(string? key) =>
        CardKey.TryParse(key, out var number)
            ? number
            : throw new ArgumentException("Key must be validated before it reaches the service.", nameof(key));

    public async Task<UpsertResult> UpsertAsync(string key, UpsertCardRequest request, CancellationToken ct)
    {
        var number = ParseExplicit(key);
        for (var attempt = 1; ; attempt++)
        {
            await using var transaction = await db.Database.BeginTransactionAsync(ct);
            var card = await db.Cards.FirstOrDefaultAsync(c => c.Key == key, ct);
            var now = clock.GetUtcNow();
            var created = card is null;
            card ??= db.Cards.Add(new Card { Key = key, Number = number, Title = request.Title!, CreatedAt = now }).Entity;

            card.Title = request.Title!;
            card.Description = request.Description ?? "";
            card.Status = request.Status!.Value;
            card.Phase = request.Phase;
            card.PendingGate = request.PendingGate;
            card.UpdatedAt = now;

            try
            {
                await db.SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);
                return new UpsertResult(card.ToDto(), created);
            }
            catch (DbUpdateException) when (attempt < MaxUpsertAttempts)
            {
                // A concurrent writer won the key or the write lock; reload and apply as a replace.
                db.ChangeTracker.Clear();
            }
        }
    }

    public async Task<CardResult> PatchAsync(string key, CardPatch patch, CancellationToken ct)
    {
        var card = await db.Cards.FirstOrDefaultAsync(c => c.Key == key, ct);
        if (card is null)
            return CardResult.Failed(CardError.NotFound);

        if (patch.Title is not null) card.Title = patch.Title;
        if (patch.Description is not null) card.Description = patch.Description;
        if (patch.Status is not null) card.Status = patch.Status.Value;
        if (patch.PhaseSpecified) card.Phase = patch.Phase;
        if (patch.PendingGateSpecified) card.PendingGate = patch.PendingGate;
        card.UpdatedAt = clock.GetUtcNow();

        await db.SaveChangesAsync(ct);
        return CardResult.Ok(card.ToDto());
    }
}
