namespace FrameworkedEmotionsMod;

public class EmotionData
{
    public string Id { get; set; }
    public string Texture { get; set; }
    public int SpriteIndex { get; set; }
    public int? FrameWidth { get; set; }
    public int? FrameHeight { get; set; }
    public int MillisecondsPerFrame { get; set; }
    public int Loops { get; set; } = 1;
    public int PositionOffset { get; set; }
    public string? OpeningTexture { get; set; }
    public bool ShowOpeningAnimation { get; set; } = true;
    public bool ShowClosingAnimation { get; set; } = true;
}