using Board.Api.Cards;
using Microsoft.EntityFrameworkCore;

namespace Board.Api.Data;

public sealed class BoardDbContext(DbContextOptions<BoardDbContext> options) : DbContext(options)
{
    public DbSet<Card> Cards => Set<Card>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BoardDbContext).Assembly);
}
