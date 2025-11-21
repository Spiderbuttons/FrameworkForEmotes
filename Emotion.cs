using System;
using FrameworkedEmotionsMod.Helpers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Characters;

namespace FrameworkedEmotionsMod;

public class Emotion(Character emoter, EmotionData emotionData, bool immediateEventCommand = false)
{
    private const float DRAW_SCALE = 4f;

    private int currentFrame = 0;
    private float emoteFrameTimer = 0f;
    private bool emoteIsShrinking = false;
    private bool emoteIsGrowing = true;
    private int loopCount;

    private float alpha = 1f;

    private Character EmotionalBeing { get; set; } = emoter;
    private EmotionData EmotionData { get; set; } = emotionData;

    private Texture2D? _emoteTexture;
    private Texture2D EmoteTexture => _emoteTexture ??= Game1.content.Load<Texture2D>(EmotionData.Texture);
    
    private Texture2D? _openingTexture;
    private Texture2D OpeningTexture => _openingTexture ??= EmotionData.OpeningTexture is not null ?
        Game1.content.Load<Texture2D>(EmotionData.OpeningTexture) :
        EmotionManager.VanillaEmotes;
    
    private int FramesPerEmote => EmoteTexture.Width / EmotionData.FrameWidth;
    public bool IsActive { get; set; } = true;

    public void Draw(SpriteBatch b)
    {
        if (!IsActive) return;
        
        Vector2 emotePosition = EmotionalBeing.getLocalPosition(Game1.viewport);
        emotePosition.Y -= (EmotionData.FrameHeight - 16) * DRAW_SCALE;
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

        Texture2D texture = emoteIsGrowing || emoteIsShrinking ? OpeningTexture : EmoteTexture;
        int sourceY = emoteIsGrowing || emoteIsShrinking ? 0 : EmotionData.SpriteIndex * EmotionData.FrameHeight;
        
        // We still want it to appear as 64 pixels wide (16 * DRAW_SCALE) even if the emote is higher/lower resolution.
        float scale = DRAW_SCALE / (EmotionData.FrameWidth / 16f);
        b.Draw(
            texture: texture,
            position: emotePosition,
            sourceRectangle: new Rectangle(
                x: currentFrame * EmotionData.FrameWidth,
                y: sourceY,
                width: EmotionData.FrameWidth,
                height: EmotionData.FrameHeight
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

        else if (emoteIsGrowing && emoteFrameTimer > 20f && currentFrame <= 3)
        {
            emoteFrameTimer = 0f;
            currentFrame++;
            if (currentFrame == 4)
            {
                currentFrame = 0;
                emoteIsGrowing = false;
            }
        }

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