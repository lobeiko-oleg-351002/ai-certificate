using System.Net;

namespace Board.Mcp.Tests;

public class ErrorMatrixTests
{
    private static FakeHandler Throwing(Exception exception) => new(_ => throw exception);

    private static HttpRequestException Refused() => new(HttpRequestError.ConnectionError, "No connection could be made");

    [Fact]
    public async Task UnreachableRead_IsLoudAndNamesTheUrl()
    {
        var result = await Harness.For(Throwing(Refused()), "http://localhost:5999").Read.GetCard("BRD-1", default);

        var text = Harness.TextOf(result);
        Assert.True(result.IsError);
        Assert.StartsWith("BOARD ERROR [unreachable] get_card:", text);
        Assert.Contains("api: http://localhost:5999", text);
        Assert.Contains("No connection could be made", text);
        Assert.Contains("applied: n/a", text);
    }

    [Fact]
    public async Task UnreachableWrite_SaysTheUpdateWasNotApplied()
    {
        var result = await Harness.For(Throwing(Refused())).Write.MoveCard("BRD-1", "Done", default);

        var text = Harness.TextOf(result);
        Assert.True(result.IsError);
        Assert.Contains("[unreachable] move_card", text);
        Assert.Contains("Update NOT applied.", text);
        Assert.Contains("applied: no", text);
    }

    [Fact]
    public async Task ConnectionResetAfterSending_IsUnknownNotNo()
    {
        var reset = new HttpRequestException(HttpRequestError.Unknown, "connection reset");

        var result = await Harness.For(Throwing(reset)).Write.MoveCard("BRD-1", "Done", default);

        Assert.Contains("applied: unknown", Harness.TextOf(result));
    }

    [Fact]
    public async Task TimeoutOnAWrite_IsUnknownAndPointsAtGetCard()
    {
        var result = await Harness.For(Throwing(new TaskCanceledException("timeout"))).Write.MoveCard("BRD-1", "Done", default);

        var text = Harness.TextOf(result);
        Assert.True(result.IsError);
        Assert.Contains("[timeout] move_card", text);
        Assert.Contains("applied: unknown", text);
        Assert.Contains("verify with get_card", text);
    }

    [Fact]
    public async Task CallerCancellation_IsNotReportedAsATimeout()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => Harness.For(Throwing(new TaskCanceledException("cancelled", null, cts.Token))).Read.ListCards(cts.Token));
    }

    [Theory]
    [InlineData("not a url")]
    [InlineData("ftp://localhost")]
    public async Task BadApiUrl_IsAConfigError(string url)
    {
        var handler = Harness.Always(HttpStatusCode.OK, "[]");

        var result = await Harness.For(handler, url).Read.ListCards(default);

        Assert.True(result.IsError);
        Assert.Contains("[config] list_cards", Harness.TextOf(result));
        Assert.Contains($"api: {url}", Harness.TextOf(result));
        Assert.Empty(handler.Requests);
    }

    [Fact]
    public async Task ValidationProblem_CarriesTheApiErrors()
    {
        var handler = Harness.Always(HttpStatusCode.BadRequest,
            """{"title":"One or more validation errors occurred.","status":400,"errors":{"title":["title must not be empty."]}}""");

        var result = await Harness.For(handler).Write.UpdateCard("BRD-1", title: " ", ct: default);

        var text = Harness.TextOf(result);
        Assert.Contains("[validation] update_card", text);
        Assert.Contains("status: 400", text);
        Assert.Contains("title must not be empty.", text);
        Assert.Contains("applied: no", text);
    }

    [Fact]
    public async Task NotFound_IsReportedForAnUnknownCard()
    {
        var result = await Harness.For(Harness.Always(HttpStatusCode.NotFound, """{"title":"Card not found."}""")).Read.GetCard("BRD-99", default);

        var text = Harness.TextOf(result);
        Assert.Contains("[not_found] get_card", text);
        Assert.Contains("status: 404", text);
        Assert.Contains("Card not found.", text);
    }

    [Fact]
    public async Task Conflict_IsReportedForADuplicateKey()
    {
        var result = await Harness.For(Harness.Always(HttpStatusCode.Conflict, """{"title":"A card with this key already exists."}"""))
            .Write.CreateCard("T", key: "BRD-1", ct: default);

        Assert.Contains("[conflict] create_card", Harness.TextOf(result));
        Assert.Contains("applied: no", Harness.TextOf(result));
    }

    [Fact]
    public async Task ServerError_OnAWrite_IsUnknown()
    {
        var result = await Harness.For(Harness.Always(HttpStatusCode.InternalServerError, """{"title":"An error occurred."}"""))
            .Write.MoveCard("BRD-1", "Done", default);

        var text = Harness.TextOf(result);
        Assert.Contains("[api_error] move_card", text);
        Assert.Contains("applied: unknown", text);
    }

    [Fact]
    public async Task ServerError_OnARead_IsNotApplicable()
    {
        var result = await Harness.For(Harness.Always(HttpStatusCode.InternalServerError, "boom")).Read.ListCards(default);

        Assert.Contains("[api_error] list_cards", Harness.TextOf(result));
        Assert.Contains("applied: n/a", Harness.TextOf(result));
    }

    [Fact]
    public async Task SuccessBodyThatIsNotACard_IsABadResponseNeverSuccess()
    {
        var result = await Harness.For(Harness.Always(HttpStatusCode.OK, "<html>proxy page</html>")).Write.MoveCard("BRD-1", "Done", default);

        var text = Harness.TextOf(result);
        Assert.True(result.IsError);
        Assert.Contains("[bad_response] move_card", text);
        Assert.Contains("applied: unknown", text);
        Assert.Contains("proxy page", text);
    }

    [Fact]
    public async Task LongBodies_AreTruncatedInTheDetail()
    {
        var result = await Harness.For(Harness.Always(HttpStatusCode.InternalServerError, new string('x', 5000))).Read.ListCards(default);

        Assert.True(Harness.TextOf(result).Length < 800);
    }

    [Fact]
    public async Task ApiUrlWithCredentials_IsRedactedInTheError()
    {
        var result = await Harness.For(Throwing(Refused()), "http://user:secret@localhost:5999").Read.ListCards(default);

        var text = Harness.TextOf(result);
        Assert.Contains("api: http://localhost:5999", text);
        Assert.DoesNotContain("secret", text);
    }

    [Fact]
    public async Task OtherClientError_OnAWrite_IsAnApiErrorThatWasNotApplied()
    {
        var result = await Harness.For(Harness.Always(HttpStatusCode.MethodNotAllowed, "{}")).Write.MoveCard("BRD-1", "Done", default);

        var text = Harness.TextOf(result);
        Assert.Contains("[api_error] move_card", text);
        Assert.Contains("applied: no", text);
    }

    [Fact]
    public async Task BadApiUrl_OnAWrite_IsNotApplied()
    {
        var result = await Harness.For(Harness.Always(HttpStatusCode.OK, "{}"), "nope").Write.MoveCard("BRD-1", "Done", default);

        Assert.Contains("applied: no", Harness.TextOf(result));
    }

    [Fact]
    public async Task CreateCard_BadStatusOrMissingTitle_FailsWithoutARequest()
    {
        var handler = Harness.Always(HttpStatusCode.OK, Harness.CardJson);
        var tools = Harness.For(handler).Write;

        var badStatus = await tools.CreateCard("T", status: "Finished", ct: default);
        var noTitle = await tools.CreateCard(" ", ct: default);

        Assert.Contains("[invalid_argument] create_card", Harness.TextOf(badStatus));
        Assert.Contains("[invalid_argument] create_card", Harness.TextOf(noTitle));
        Assert.Empty(handler.Requests);
    }

    [Fact]
    public async Task SyncCard_BadStatusOrMissingKey_FailsWithoutARequest()
    {
        var handler = Harness.Always(HttpStatusCode.OK, Harness.CardJson);
        var tools = Harness.For(handler).Write;

        var badStatus = await tools.SyncCard("BRD-1", "T", "Finished", ct: default);
        var noKey = await tools.SyncCard("", "T", "ToDo", ct: default);

        Assert.Contains("[invalid_argument] sync_card", Harness.TextOf(badStatus));
        Assert.Contains("[invalid_argument] sync_card", Harness.TextOf(noKey));
        Assert.Empty(handler.Requests);
    }

    [Fact]
    public async Task SyncCard_ServerErrorOnThePut_IsUnknown()
    {
        var handler = Harness.Always(HttpStatusCode.InternalServerError, """{"title":"boom"}""");

        var result = await Harness.For(handler).Write.SyncCard("BRD-1", "T", "ToDo", ct: default);

        var text = Harness.TextOf(result);
        Assert.True(result.IsError);
        Assert.Contains("[api_error] sync_card", text);
        Assert.Contains("applied: unknown", text);
        Assert.Single(handler.Requests);
    }

    [Fact]
    public async Task SyncCard_Refused_ReportsNothingWasApplied()
    {
        var result = await Harness.For(Throwing(Refused())).Write.SyncCard("BRD-1", "T", "ToDo", ct: default);

        var text = Harness.TextOf(result);
        Assert.Contains("[unreachable] sync_card", text);
        Assert.Contains("applied: no", text);
    }

    [Fact]
    public async Task SyncCard_Validation400_IsNotApplied()
    {
        var handler = Harness.Always(HttpStatusCode.BadRequest, """{"title":"One or more validation errors occurred.","errors":{"title":["Title is required."]}}""");

        var text = Harness.TextOf(await Harness.For(handler).Write.SyncCard("BRD-1", "T", "ToDo", ct: default));

        Assert.Contains("[validation] sync_card", text);
        Assert.Contains("applied: no", text);
    }

    [Fact]
    public async Task SyncCard_Timeout_IsUnknownAndPointsAtGetCard()
    {
        var handler = new FakeHandler(_ => throw new TaskCanceledException("timed out"));

        var text = Harness.TextOf(await Harness.For(handler).Write.SyncCard("BRD-1", "T", "ToDo", ct: default));

        Assert.Contains("[timeout] sync_card", text);
        Assert.Contains("applied: unknown", text);
    }

    [Fact]
    public async Task SyncCard_Unexpected2xx_IsABadResponseNeverSuccess()
    {
        var handler = Harness.Always(HttpStatusCode.Accepted, Harness.CardJson);

        var result = await Harness.For(handler).Write.SyncCard("BRD-1", "T", "ToDo", ct: default);

        Assert.True(result.IsError);
        Assert.Contains("[bad_response] sync_card", Harness.TextOf(result));
        Assert.Contains("applied: unknown", Harness.TextOf(result));
    }
}
