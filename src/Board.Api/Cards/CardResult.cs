using Board.Contracts;

namespace Board.Api.Cards;

public sealed record CardResult(CardDto? Card, CardError Error)
{
    public static CardResult Ok(CardDto card) => new(card, CardError.None);

    public static CardResult Failed(CardError error) => new(null, error);
}
