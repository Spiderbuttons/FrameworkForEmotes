using HarmonyLib;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using FrameworkForEmotes.Helpers;
using StardewValley.Delegates;
using StardewValley.Triggers;

namespace FrameworkForEmotes
{
    internal sealed class ModEntry : Mod
    {
        internal static IModHelper ModHelper { get; set; } = null!;
        internal static IMonitor ModMonitor { get; set; } = null!;
        internal static Harmony Harmony { get; set; } = null!;
        
        private static EmotionManager EmotionManager { get; set; } = null!;

        public override void Entry(IModHelper helper)
        {
            ModHelper = helper;
            ModMonitor = Monitor;
            
            EmotionManager = new EmotionManager();
            
            TriggerActionManager.RegisterAction($"{ModManifest.UniqueID}_Emote", EmoteTraction);
            GameLocation.RegisterTileAction($"{ModManifest.UniqueID}_Emote",
                (_, args, _, _) => EmoteTraction(args, TriggerActionManager.EmptyManualContext, out _));
            GameLocation.RegisterTouchAction($"{ModManifest.UniqueID}_Emote", 
                (_, args, _, _) => EmoteTraction(args, TriggerActionManager.EmptyManualContext, out _));

            Helper.Events.GameLoop.GameLaunched += OnGameLaunched;
            Helper.Events.Input.ButtonPressed += OnButtonPressed;
            
            Helper.Events.Content.AssetRequested += OnAssetRequested;
        }
        
        private void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
        {
            if (e.Name.IsEquivalentTo(@"Spiderbuttons.FEM\Emotes"))
            {
                e.Edit(ass =>
                {
                    var dict = ass.AsDictionary<string, EmotionData>().Data;
                    dict["TestEmotion"] = new EmotionData
                    {
                        Id = "TestEmotion",
                        Texture = Helper.ModContent.GetInternalAssetName("emotes.png").BaseName,
                        SpriteIndex = 1,
                        MillisecondsPerFrame = 100,
                        Loops = 3,
                        PositionOffset = 0,
                        FrameWidth = 256,
                        FrameHeight = 256,
                        OpeningTexture = Helper.ModContent.GetInternalAssetName("emotes.png").BaseName,
                    };
                });
            }
        }
        
        [EventPriority(EventPriority.Low)]
        private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
        {
            Log.Trace("*spider noises*");
            
            // I don't want the game to register a warning when I overwrite the original, so...
            Event.SetupEventCommandsIfNeeded();
            Event.Commands["Emote"] = CustomEmoteEventCommand;
        }

        private static bool EmoteTraction(string[] args, TriggerActionContext context, out string error)
        {
            if (!ArgUtility.TryGet(args, 1, out string characterName, out error, allowBlank: false) || !ArgUtility.TryGet(args, 2, out string emoteId, out error, allowBlank: false))
            {
                Log.Error(error);
                return false;
            }

            bool isFarmer = characterName.ToLower().Equals("farmer");
            Character emoter = isFarmer ? Game1.player : Game1.getCharacterFromName(characterName);
            if (emoter is null)
            {
                Log.Error($"No character found with name '{characterName}'");
                return false;
            }
            
            if (int.TryParse(emoteId, out int emoteInt))
            {
                emoter.doEmote(emoteInt);
                return true;
            }

            if (!EmotionManager.TryGetEmotion(emoteId, out var emotion))
            {
                Log.Error($"The provided emote Id '${emoteId}' does not exist");
                return false;
            }
            EmotionManager.PlayEmotion(emoter, emotion, isEventEmote: Game1.currentLocation.currentEvent is not null);
            return true;
        }
        
        private static void CustomEmoteEventCommand(Event @event, string[] args, EventContext context)
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
                context.LogErrorAndSkip($"[Frameworked Emotions Mod] The provided emote Id '${emoteId}' does not exist");
                return;
            }

            if (@event.IsFarmerActorId(actorName, out var farmerNumber))
            {
                var farmerActor = @event.GetFarmerActor(farmerNumber);
                EmotionManager.PlayEmotion(farmerActor, emotion, isEventEmote: true, !nextCommandImmediate);
            }
            else
            {
                NPC npc = @event.getActorByName(actorName, out bool isOptionalNpc);
                if (npc is null)
                {
                    context.LogErrorAndSkip($"no NPC found with name '{actorName}'", isOptionalNpc);
                    return;
                }
                EmotionManager.PlayEmotion(npc, emotion, isEventEmote: true, !nextCommandImmediate);
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
        }
    }
}