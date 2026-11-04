namespace Board.Mcp;

public static class BoardHttp
{
    public static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

    public static HttpClient Create() => new() { Timeout = Timeout };
}
