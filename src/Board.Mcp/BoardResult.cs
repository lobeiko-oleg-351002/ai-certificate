namespace Board.Mcp;

public sealed record BoardResult<T>(T? Value, BoardError? Error)
{
    public static BoardResult<T> Ok(T value) => new(value, null);

    public static BoardResult<T> Fail(BoardError error) => new(default, error);
}
