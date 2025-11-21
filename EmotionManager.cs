using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI.Events;
using StardewValley;

namespace FrameworkedEmotionsMod;

public class EmotionManager
{
    private static Dictionary<string, EmotionData>? _emotions;
    public static Dictionary<string, EmotionData> Emotions { get; } = _emotions ??= Game1.content.Load<Dictionary<string, EmotionData>>(@"Spiderbuttons.FEM\Emotes");
    private List<Emotion> ActiveEmotions { get; } = [];

    private static Texture2D? vanillaEmotes;
    public static Texture2D VanillaEmotes => vanillaEmotes ??= Game1.content.Load<Texture2D>(@"TileSheets\emotes");

    public EmotionManager()
    {
        ModEntry.ModHelper.Events.Display.RenderedWorld += OnRenderedWorld;
        ModEntry.ModHelper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
        ModEntry.ModHelper.Events.Content.AssetRequested += OnAssetRequested;
        ModEntry.ModHelper.Events.Content.AssetsInvalidated += OnAssetsInvalidated;
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
    
    public void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
    {
        if (e.Name.IsEquivalentTo(@"Spiderbuttons.FEM\Emotes"))
        {
            e.LoadFrom(() => new Dictionary<string, EmotionData>(), AssetLoadPriority.Exclusive);
        }
    }

    public void OnAssetsInvalidated(object? sender, AssetsInvalidatedEventArgs e)
    {
        if (e.NamesWithoutLocale.Any(ass => ass.IsEquivalentTo(@"Spiderbuttons.FEM\Emotes")))
        {
            _emotions = null;
        }
    }
}