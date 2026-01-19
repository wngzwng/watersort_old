namespace WaterSort.Core.Solvers;

public static class AnsiColor
{
    private static readonly string[] Palette =
    {
        // "\u001b[31m", // red
        // "\u001b[32m", // green
        // "\u001b[33m", // yellow
        // "\u001b[34m", // blue
        // "\u001b[35m", // magenta
        // "\u001b[36m", // cyan
        // "\u001b[91m", // bright red
        // "\u001b[92m", // bright green
        // "\u001b[93m", // bright yellow
        // "\u001b[94m", // bright blue
        
        
        "\u001b[30m", // black
        "\u001b[31m", // red
        "\u001b[32m", // green
        "\u001b[33m", // yellow
        "\u001b[34m", // blue
        "\u001b[35m", // magenta
        "\u001b[36m", // cyan
        "\u001b[37m", // white

        "\u001b[90m", // bright black (gray)
        "\u001b[91m", // bright red
        "\u001b[92m", // bright green
        "\u001b[93m", // bright yellow
        "\u001b[94m", // bright blue
        "\u001b[95m", // bright magenta
        "\u001b[96m", // bright cyan
        "\u001b[97m", // bright white
    };

    public const string Reset = "\u001b[0m";

    public static string Colorize(int value, string text)
    {
        if (value <= 0)
            return text;

        var color = Palette[(value - 1) % Palette.Length];
        return color + text + Reset;
    }
}
