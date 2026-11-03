using System.Text.RegularExpressions;

namespace Board.Api.Cards;

public static partial class CardKey
{
    public static string Format(int number) => $"BRD-{number}";

    public static bool TryParse(string? key, out int number)
    {
        number = 0;
        var match = KeyPattern().Match(key ?? "");
        return match.Success && int.TryParse(match.Groups[1].ValueSpan, out number);
    }

    [GeneratedRegex("^BRD-([1-9][0-9]{0,8})$")]
    private static partial Regex KeyPattern();
}
