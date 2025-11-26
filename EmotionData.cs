using Microsoft.Xna.Framework;

namespace FrameworkForEmotes;

public class EmotionData
{
    public string Id { get; set; }
    public string Texture { get; set; }
    public int SpriteIndex { get; set; }
    public int? FrameWidth { get; set; }
    public int? FrameHeight { get; set; }
    public int MillisecondsPerFrame { get; set; } = 250;
    public int Loops { get; set; } = 1;
    public Vector2 PositionOffset { get; set; } = Vector2.Zero;
    public string? OpeningTexture { get; set; }
    public bool ShowOpeningAnimation { get; set; } = true;
    public bool ShowClosingAnimation { get; set; } = true;
}