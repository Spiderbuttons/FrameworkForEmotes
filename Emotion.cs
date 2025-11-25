using System;
using FrameworkForEmotes.Helpers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Characters;

namespace FrameworkForEmotes;

public class Emotion(Character emoter, EmotionData emotionData, bool isEventEmote, bool immediateEventCommand = false)
{
    private const float DRAW_SCALE = 4f;

    private int currentFrame;
    private float emoteFrameTimer;
    private bool emoteIsShrinking;
    private bool emoteIsGrowing = true;
    private int loopCount;

    public Character EmotionalBeing { get; } = emoter;
    private EmotionData EmotionData { get; } = emotionData;

    private Texture2D? _emoteTexture;
    private Texture2D EmoteTexture => _emoteTexture ??= Game1.content.Load<Texture2D>(EmotionData.Texture);
    
    private Texture2D? _openingTexture;
    private Texture2D OpeningTexture => _openingTexture ??= EmotionData.OpeningTexture is not null ?
        Game1.content.Load<Texture2D>(EmotionData.OpeningTexture) :
        EmotionManager.VanillaEmotes;

    private int OpeningFrames => OpeningTexture.Width / FrameWidth;
    
    private int FrameWidth => EmotionData.FrameWidth ?? 16;
    private int FrameHeight => EmotionData.FrameHeight ?? FrameWidth;
    private int FramesPerEmote => EmoteTexture.Width / FrameWidth;
    public bool IsActive { get; set; } = true;

    public void Draw(SpriteBatch b)
    {
        if (EmotionalBeing.IsEmoting) IsActive = false;
        if (isEventEmote && Game1.currentLocation.currentEvent is null) IsActive = false;
        if (!IsActive || EmotionalBeing.currentLocation.NameOrUniqueName != Game1.currentLocation.NameOrUniqueName) return;
        
        Vector2 emotePosition = EmotionalBeing.getLocalPosition(Game1.viewport);
        switch (EmotionalBeing)
        {
            case Farmer:
            {
                emotePosition.Y -= 160f;
                break;
            }
            case Child child:
            {
                emotePosition.Y -= 32 + child.Sprite.SpriteHeight * DRAW_SCALE - (child.Age is 1 or 3 ? 64 : 0);
                emotePosition.X += child.Age == 1 ? 8 : 0;
                break;
            }
            case Pet pet:
            {
                Point offset = pet.GetPetData()?.EmoteOffset ?? Point.Zero;
                emotePosition.X += 32f + offset.X;
                emotePosition.Y -= 96f + offset.Y;
                break;
            }
            case NPC npc:
            {
                Point offset = npc.GetData()?.EmoteOffset ?? Point.Zero;
                emotePosition.X += offset.X + (npc.Sprite.SourceRect.Width / 2f - 8f) * DRAW_SCALE;
                emotePosition.Y += offset.Y + npc.emoteYOffset - (npc.Sprite.SpriteHeight + 3) * DRAW_SCALE;
                if (npc.NeedsBirdieEmoteHack()) emotePosition.X += 64f;
                if (npc.Age is 2) emotePosition.Y += 32f;
                if (npc.Gender is Gender.Female) emotePosition.Y += 10f;
                break;
            }
            default:
                emotePosition.Y -= 96f;
                emotePosition.Y += EmotionalBeing.emoteYOffset;
                emotePosition.X += EmotionalBeing.Sprite.SourceRect.Width * DRAW_SCALE / 2f - 32f;
                break;
        }
        
        // These formulas will compensate for emotes that are not square and taller than they are wide.
        // Without this adjustment you get emotes starting like, inside the body, which is not desirable, as it turns out.
        // (We don't need to compensate for emotes that are wider than they are tall, that came for free with my Xbox.)
        float scaleTo16 = 16f / FrameWidth;
        float heightAdjustment = (FrameHeight * scaleTo16 - 16f) / 16;
        emotePosition.Y -= 16f * heightAdjustment * DRAW_SCALE;
        
        emotePosition += EmotionData.PositionOffset;

        Texture2D texture = emoteIsGrowing || emoteIsShrinking ? OpeningTexture : EmoteTexture;
        int sourceY = emoteIsGrowing || emoteIsShrinking ? 0 : EmotionData.SpriteIndex * FrameHeight;
        
        // We still want it to appear as 64 pixels wide (16 * DRAW_SCALE) even if the emote is higher/lower resolution.
        float scale = DRAW_SCALE / (FrameWidth / 16f);
        b.Draw(
            texture: texture,
            position: emotePosition,
            sourceRectangle: new Rectangle(
                x: currentFrame * FrameWidth,
                y: sourceY,
                width: FrameWidth,
                height: FrameHeight
            ),
            color: Color.White,
            rotation: 0f,
            origin: Vector2.Zero,
            scale: scale,
            effects: SpriteEffects.None,
            layerDepth: EmotionalBeing.StandingPixel.Y / 10000f
        );
    }

    public void Update(GameTime time)
    {
        if (EmotionalBeing.IsEmoting) IsActive = false;
        if (isEventEmote && Game1.currentLocation.currentEvent is null) IsActive = false;
        if (!IsActive) return;
        
        if (emoteIsGrowing && !EmotionData.ShowOpeningAnimation) emoteIsGrowing = false;
        if (emoteIsShrinking && !EmotionData.ShowClosingAnimation)
        {
            IsActive = false;
            if (immediateEventCommand && Game1.currentLocation.currentEvent is { } @event &&
                (@event.actors.Contains(EmotionalBeing as NPC) ||
                 @event.farmerActors.Contains(EmotionalBeing as Farmer) ||
                 EmotionalBeing.Name.Equals(Game1.player.Name)))
            {
                @event.CurrentCommand++;
            }
            return;
        }
        
        emoteFrameTimer += time.ElapsedGameTime.Milliseconds;

        // This if-block handles reversing the opening animation to eventually end the emote.
        if (emoteIsShrinking && emoteFrameTimer > 20f)
        {
            emoteFrameTimer = 0f;
            currentFrame--;
            if (currentFrame < 0)
            {
                emoteIsShrinking = false;
                IsActive = false;
                if (immediateEventCommand && Game1.currentLocation.currentEvent is { } @event &&
                    (@event.actors.Contains(EmotionalBeing as NPC) ||
                     @event.farmerActors.Contains(EmotionalBeing as Farmer) ||
                     EmotionalBeing.Name.Equals(Game1.player.Name)))
                {
                    @event.CurrentCommand++;
                }
            }
        }

        // This if-block handles when the emote first starts and needs to play the opening animation.
        else if (emoteIsGrowing && emoteFrameTimer > 20f && currentFrame <= OpeningFrames - 1)
        {
            emoteFrameTimer = 0f;
            currentFrame++;
            if (currentFrame == OpeningFrames)
            {
                currentFrame = 0;
                emoteIsGrowing = false;
            }
        }

        // Finally, this if-block handles the custom emote itself, advancing frame by frame every MillisecondsPerFrame ms until it reaches the last one.
        // (And until it's done looping, if applicable.)
        else if (!emoteIsShrinking && !emoteIsGrowing && emoteFrameTimer > EmotionData.MillisecondsPerFrame)
        {
            emoteFrameTimer = 0f;
            currentFrame++;
            if (currentFrame >= FramesPerEmote)
            {
                loopCount++;
                if (loopCount >= EmotionData.Loops)
                {
                    emoteIsShrinking = true;
                    currentFrame = FramesPerEmote - 1;
                }
                else currentFrame = 0;
            }
        }
    }
}