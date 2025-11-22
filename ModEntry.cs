using System;
using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using FrameworkedEmotionsMod.Helpers;

namespace FrameworkedEmotionsMod
{
    internal sealed class ModEntry : Mod
    {
        internal static IModHelper ModHelper { get; set; } = null!;
        internal static IMonitor ModMonitor { get; set; } = null!;
        internal static Harmony Harmony { get; set; } = null!;
        
        internal static EmotionManager EmotionManager { get; set; } = null!;

        public override void Entry(IModHelper helper)
        {
            ModHelper = helper;
            ModMonitor = Monitor;
            
            EmotionManager = new EmotionManager();

            Helper.Events.GameLoop.GameLaunched += OnGameLaunched;
            Helper.Events.Input.ButtonPressed += OnButtonPressed;
        }
        
        [EventPriority(EventPriority.High)]
        private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
        {
            Event.SetupEventCommandsIfNeeded();
            Event.Commands["Emote"] = CustomEmote;
        }
        
        private static void CustomEmote(Event @event, string[] args, EventContext context)
        {
            if (ArgUtility.TryGetInt(args, 2, out _, out string error, "int emoteId"))
            {
                Event.DefaultCommands.Emote(@event, args, context);
                return;
            }
        
            if (!ArgUtility.TryGet(args, 1, out var actorName, out error, allowBlank: true, "string actorName") || !ArgUtility.TryGet(args, 2, out string emoteId, out error, allowBlank: false) || !ArgUtility.TryGetOptionalBool(args, 3, out var nextCommandImmediate, out error, defaultValue: false, "bool nextCommandImmediate"))
            {
                context.LogErrorAndSkip(error);
                return;
            }

            if (!EmotionManager.TryGetEmotion(emoteId, out var emotion))
            {
                context.LogErrorAndSkip($"The provided emote Id '${emoteId}' does not exist.");
                return;
            }

            if (@event.IsFarmerActorId(actorName, out var farmerNumber))
            {
                EmotionManager.PlayEmotion(@event.GetFarmerActor(farmerNumber), emotion, !nextCommandImmediate);
            }
            else
            {
                NPC npc = @event.getActorByName(actorName, out bool isOptionalNpc);
                if (npc is null)
                {
                    context.LogErrorAndSkip($"no NPC found with name '{actorName}'", isOptionalNpc);
                    return;
                }
                EmotionManager.PlayEmotion(npc, emotion, !nextCommandImmediate);
            }

            if (nextCommandImmediate)
            {
                @event.CurrentCommand++;
                @event.Update(context.Location, context.Time);
            }
        }

        private void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
        {
            if (!Context.IsWorldReady)
                return;

            if (e.Button == SButton.F5)
            {
                Helper.GameContent.InvalidateCache(Helper.ModContent.GetInternalAssetName("emotes.png").BaseName);
                EmotionData testEmotion = new EmotionData
                {
                    Id = "TestEmotion",
                    // Texture = @"TileSheets\emotes",
                    Texture = Helper.ModContent.GetInternalAssetName("emotes.png").BaseName,
                    SpriteIndex = 1,
                    MillisecondsPerFrame = 250,
                    PositionOffset = 0,
                    FrameWidth = 256,
                    FrameHeight = 256,
                    OpeningTexture = Helper.ModContent.GetInternalAssetName("emotes.png").BaseName,
                };
                // EmotionManager.PlayEmotion(Game1.getCharacterFromName("Haley"), testEmotion);
                EmotionManager.PlayEmotion(Game1.player, testEmotion);
            }

            if (e.Button == SButton.F6)
            {
                EmotionData testEmotion = new EmotionData
                {
                    Id = "TestEmotion",
                    // Texture = @"TileSheets\emotes",
                    Texture = Helper.ModContent.GetInternalAssetName("emotes.png").BaseName,
                    SpriteIndex = 1,
                    MillisecondsPerFrame = 250,
                    PositionOffset = 0,
                    Loops = 4,
                    FrameHeight = 32,
                    OpeningTexture = Helper.ModContent.GetInternalAssetName("emotes.png").BaseName,
                };
                EmotionManager.PlayEmotion(Game1.getCharacterFromName("Haley"), testEmotion);
                // EmotionManager.PlayEmotion(Game1.player, testEmotion);
            }
        }
    }
}