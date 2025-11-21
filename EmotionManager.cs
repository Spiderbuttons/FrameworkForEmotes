using System.Collections.Generic;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI.Events;
using StardewValley;

namespace FrameworkedEmotionsMod;

public class EmotionManager
{
    public Dictionary<string, EmotionData> Emotions { get; } = new();
    private List<Emotion> ActiveEmotions { get; } = [];

    private static Texture2D? vanillaEmotes = null;
    public static Texture2D VanillaEmotes => vanillaEmotes ??= Game1.content.Load<Texture2D>(@"TileSheets\emotes");

    public EmotionManager()
    {
        ModEntry.ModHelper.Events.Display.RenderedWorld += OnRenderedWorld;
        ModEntry.ModHelper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
    }
    
    public void PlayEmotion(Character emoter, EmotionData emotionData, bool immediateEventCommand = false)
    {
        Emotion newEmotion = new(emoter, emotionData, immediateEventCommand);
        ActiveEmotions.Add(newEmotion);
    }

    public void OnRenderedWorld(object? sender, RenderedWorldEventArgs e)
    {
        for (int i = ActiveEmotions.Count - 1; i >= 0; i--)
        {
            Emotion emotion = ActiveEmotions[i];
            if (emotion.IsActive) emotion.Draw(e.SpriteBatch);
        }
    }

    public void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
    {
        for (int i = ActiveEmotions.Count - 1; i >= 0; i--)
        {
            Emotion emotion = ActiveEmotions[i];
            if (emotion.IsActive) emotion.Update(Game1.currentGameTime);
            else ActiveEmotions.RemoveAt(i);
        }
    }
}