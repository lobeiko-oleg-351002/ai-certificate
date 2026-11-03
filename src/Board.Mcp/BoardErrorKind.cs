namespace Board.Mcp;

public enum BoardErrorKind
{
    Unreachable,
    Timeout,
    Config,
    InvalidArgument,
    Validation,
    NotFound,
    Conflict,
    ApiError,
    BadResponse
}
