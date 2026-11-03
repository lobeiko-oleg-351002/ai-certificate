using System.Text.Json;
using ModelContextProtocol.Protocol;

namespace Board.Mcp;

public static class ToolResults
{
    public static CallToolResult From<T>(string tool, BoardResult<T> result) =>
        result.Error is not null
            ? Failure(result.Error.Format(tool))
            : Success(JsonSerializer.Serialize(result.Value, BoardJson.Options));

    public static CallToolResult InvalidArgument(string tool, string message) =>
        Failure(new BoardError(BoardErrorKind.InvalidArgument, message, "n/a", null, "no request was sent", AppliedState.No).Format(tool));

    private static CallToolResult Success(string text) =>
        new() { Content = [new TextContentBlock { Text = text }] };

    private static CallToolResult Failure(string text) =>
        new() { IsError = true, Content = [new TextContentBlock { Text = text }] };
}
