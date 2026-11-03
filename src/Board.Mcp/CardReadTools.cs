using System.ComponentModel;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace Board.Mcp;

[McpServerToolType]
public sealed class CardReadTools(BoardClient board)
{
    [McpServerTool(Name = "list_cards", ReadOnly = true), Description("List every card on the board, ordered by key number.")]
    public async Task<CallToolResult> ListCards(CancellationToken ct) =>
        ToolResults.From("list_cards", await board.ListAsync(ct));

    [McpServerTool(Name = "get_card", ReadOnly = true), Description("Get one card by key (for example BRD-3).")]
    public async Task<CallToolResult> GetCard([Description("Card key, BRD-n.")] string key, CancellationToken ct) =>
        string.IsNullOrWhiteSpace(key)
            ? ToolResults.InvalidArgument("get_card", "key is required.")
            : ToolResults.From("get_card", await board.GetAsync(key, ct));
}
