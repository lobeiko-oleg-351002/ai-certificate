using Board.Contracts;

namespace Board.Mcp;

public static class StatusArgument
{
    public static string Allowed => string.Join(", ", Enum.GetNames<CardStatus>());

    public static bool TryParse(string? text, out CardStatus status)
    {
        var name = Enum.GetNames<CardStatus>()
            .FirstOrDefault(n => string.Equals(n, text?.Trim(), StringComparison.OrdinalIgnoreCase));
        status = name is null ? default : Enum.Parse<CardStatus>(name);
        return name is not null;
    }
}
