using UnityEngine;

public static class Palette
{
    // Environment
    public static readonly Color Background = Hex("#141E2E");
    public static readonly Color Bridge = Hex("#463E34");
    public static readonly Color Wall = Hex("#2C2823");

    // Signals (primary)
    public static readonly Color SignalRed = Hex("#C75A4A");
    public static readonly Color SignalYellow = Hex("#D6AE4A");
    public static readonly Color SignalBlue = Hex("#5A8FBF");

    // Signals (compound, Phase 2)
    public static readonly Color SignalOrange = Hex("#C48244");
    public static readonly Color SignalGreen = Hex("#65A872");
    public static readonly Color SignalPurple = Hex("#8967B8");

    // Actors / UI
    public static readonly Color Robot = Hex("#8ADDEE");
    public static readonly Color Highlight = Hex("#F5E090");
    public static readonly Color UIText = Hex("#EAEAEA");
    public static readonly Color Wire = Hex("#6F7A8C");

    // Tile details
    public static readonly Color TileOutline = Hex("#1F2A3A");
    public static readonly Color TileCrack = Hex("#1F2A3A");

    // Glow alpha multiplier (0..1)
    public const float GlowAlpha = 0.35f;
    public const float GlowScale = 1.6f;

    private static Color Hex(string hex)
    {
        ColorUtility.TryParseHtmlString(hex, out var c);
        return c;
    }
}
