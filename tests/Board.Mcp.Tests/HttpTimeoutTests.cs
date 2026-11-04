namespace Board.Mcp.Tests;

public class HttpTimeoutTests
{
    [Fact]
    public void BoardHttpClient_TimesOutAfterTenSeconds()
    {
        using var http = BoardHttp.Create();

        Assert.Equal(TimeSpan.FromSeconds(10), http.Timeout);
    }
}
