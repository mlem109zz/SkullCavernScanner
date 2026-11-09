using StardewModdingAPI;

namespace SkullCavernScanner;

internal sealed class ModConfig
{
    public bool Enabled { get; set; } = true;
    public bool ShowCandidateRocks { get; set; } = true;
    public bool ShowGeneratedExit { get; set; } = true;
    public bool ShowDropPossibilities { get; set; } = true;
    public bool ShowTileCoordinates { get; set; } = true;
    public float HighlightAlpha { get; set; } = 0.88f;
    public SButton ToggleKey { get; set; } = SButton.F8;
}
