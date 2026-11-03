using System.Text;

namespace Board.Mcp;

public sealed record BoardError(
    BoardErrorKind Kind,
    string Message,
    string Api,
    int? Status,
    string? Detail,
    AppliedState Applied)
{
    public string Format(string tool)
    {
        var text = new StringBuilder();
        text.Append("BOARD ERROR [").Append(KindName).Append("] ").Append(tool).Append(": ").Append(Message);
        text.Append(Applied switch
        {
            AppliedState.No => " Update NOT applied.",
            AppliedState.Unknown => " Outcome UNKNOWN: verify with get_card before retrying.",
            _ => ""
        });
        text.AppendLine();
        text.Append("api: ").AppendLine(Api);
        text.Append("status: ").AppendLine(Status?.ToString() ?? "n/a");
        text.Append("detail: ").AppendLine(Detail ?? "n/a");
        text.Append("applied: ").Append(Applied switch
        {
            AppliedState.No => "no",
            AppliedState.Unknown => "unknown",
            _ => "n/a"
        });
        return text.ToString();
    }

    private string KindName => Kind switch
    {
        BoardErrorKind.InvalidArgument => "invalid_argument",
        BoardErrorKind.NotFound => "not_found",
        BoardErrorKind.ApiError => "api_error",
        BoardErrorKind.BadResponse => "bad_response",
        _ => Kind.ToString().ToLowerInvariant()
    };
}
