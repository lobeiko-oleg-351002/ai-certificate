using System.Text.Json.Nodes;
using Board.Contracts;

namespace Board.Api.Cards;

public static class CardEndpoints
{
    public static IEndpointRouteBuilder MapCardEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/cards");

        group.MapGet("/", async (CardService cards, CancellationToken ct) =>
            Results.Ok(await cards.ListAsync(ct)));

        group.MapGet("/{key}", async (string key, CardService cards, CancellationToken ct) =>
            ToResponse(await cards.GetAsync(key, ct)));

        group.MapPost("/", async (CreateCardRequest request, CardService cards, CancellationToken ct) =>
        {
            var errors = CreateCardValidator.Validate(request);
            if (errors.Count > 0)
                return Results.ValidationProblem(errors);

            var result = await cards.CreateAsync(request, ct);
            return result.Card is null
                ? ToResponse(result)
                : Results.Created($"/api/cards/{result.Card.Key}", result.Card);
        });

        group.MapPatch("/{key}", async (string key, JsonObject? body, CardService cards, CancellationToken ct) =>
        {
            var read = CardPatchReader.Read(body);
            if (read.Patch is null)
                return Results.ValidationProblem(read.Errors);

            return ToResponse(await cards.PatchAsync(key, read.Patch, ct));
        });

        return app;
    }

    private static IResult ToResponse(CardResult result) => result switch
    {
        { Card: not null } => Results.Ok(result.Card),
        { Error: CardError.Conflict } => Results.Problem(
            statusCode: StatusCodes.Status409Conflict, title: "A card with this key already exists."),
        _ => Results.Problem(statusCode: StatusCodes.Status404NotFound, title: "Card not found.")
    };
}
