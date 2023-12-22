using GenshinMod.Common.ModObjects.ModSystems;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria.DataStructures;
using Terraria.ModLoader;
using Terraria;
using Microsoft.Xna.Framework;
using static Terraria.ModLoader.ModContent;
using Terraria.ID;

namespace GenshinMod.Common.ModObjects.Players;

public class GenshinMiscBackLayer : PlayerDrawLayer
{
    private static Asset<Texture2D> _jeanCape;
    private static Asset<Texture2D> _kaeyaCape;

    public override bool IsHeadLayer => false;

    public override Position GetDefaultPosition() => new AfterParent(PlayerDrawLayers.BackAcc);

    public override bool GetDefaultVisibility(PlayerDrawSet drawInfo)
    {
        return !drawInfo.drawPlayer.dead
            && drawInfo.drawPlayer.body != -1;
    }

    protected override void Draw(ref PlayerDrawSet drawInfo)
    {
        if (drawInfo.drawPlayer.body == EquipLoader.GetEquipSlot(Mod, "Jean_Body", EquipType.Body)
            && drawInfo.drawPlayer.body != -1)
        {
            // Vanilla position code
            int drawX = (int)(drawInfo.Position.X - Main.screenPosition.X - drawInfo.drawPlayer.bodyFrame.Width / 2f + drawInfo.drawPlayer.width / 2f);
            int drawY = (int)(drawInfo.Position.Y - Main.screenPosition.Y + drawInfo.drawPlayer.height - drawInfo.drawPlayer.bodyFrame.Height + 4.0);
            Vector2 position = new Vector2(drawX, drawY) + drawInfo.drawPlayer.bodyPosition + drawInfo.bodyVect;
            position = position.Floor();

            // Make sure the texture is loaded
            _jeanCape ??= Request<Texture2D>("GenshinMod/Content/Characters/Jean/Textures/Jean_Cape");

            DrawData data = new DrawData(
                _jeanCape.Value,
                position,
                drawInfo.drawPlayer.bodyFrame,
                drawInfo.colorArmorBody,
                drawInfo.drawPlayer.bodyRotation,
                drawInfo.bodyVect,
                1f,
                drawInfo.playerEffect)
            {
                shader = drawInfo.cBody
            };

            drawInfo.DrawDataCache.Add(data);
        }

        if (drawInfo.drawPlayer.body == EquipLoader.GetEquipSlot(Mod, "Kaeya_Body", EquipType.Body)
            && drawInfo.drawPlayer.body != -1)
        {
            // Vanilla position code
            int drawX = (int)(drawInfo.Position.X - Main.screenPosition.X - drawInfo.drawPlayer.bodyFrame.Width / 2f + drawInfo.drawPlayer.width / 2f);
            int drawY = (int)(drawInfo.Position.Y - Main.screenPosition.Y + drawInfo.drawPlayer.height - drawInfo.drawPlayer.bodyFrame.Height + 4.0);
            Vector2 position = new Vector2(drawX, drawY) + drawInfo.drawPlayer.bodyPosition + drawInfo.bodyVect;
            position = position.Floor();

            // Make sure the texture is loaded
            _kaeyaCape ??= Request<Texture2D>("GenshinMod/Content/Characters/Kaeya/Textures/Kaeya_Cape");

            DrawData data = new DrawData(
                _kaeyaCape.Value,
                position,
                drawInfo.drawPlayer.bodyFrame,
                drawInfo.colorArmorBody,
                drawInfo.drawPlayer.bodyRotation,
                drawInfo.bodyVect,
                1f,
                drawInfo.playerEffect)
            {
                shader = drawInfo.cBody
            };

            drawInfo.DrawDataCache.Add(data);
        }
    }
}

public class GenshinBackShoulderLayer : PlayerDrawLayer
{
    private static Asset<Texture2D> _jeanShouldersBack;
    private static Asset<Texture2D> _noelleShouldersBack;

    public override bool IsHeadLayer => false;

    public override Position GetDefaultPosition() => new AfterParent(PlayerDrawLayers.Torso);

    public override bool GetDefaultVisibility(PlayerDrawSet drawInfo)
    {
        return !drawInfo.drawPlayer.dead
            && drawInfo.drawPlayer.body != -1;
    }

    protected override void Draw(ref PlayerDrawSet drawInfo)
    {
        if (drawInfo.drawPlayer.body == EquipLoader.GetEquipSlot(Mod, "Jean_Body", EquipType.Body)
            && drawInfo.drawPlayer.body != -1)
        {
            // Vanilla position code
            int drawX = (int)(drawInfo.Position.X - Main.screenPosition.X - drawInfo.drawPlayer.bodyFrame.Width / 2f + drawInfo.drawPlayer.width / 2f);
            int drawY = (int)(drawInfo.Position.Y - Main.screenPosition.Y + drawInfo.drawPlayer.height - drawInfo.drawPlayer.bodyFrame.Height + 4.0);
            Vector2 position = new Vector2(drawX, drawY) + drawInfo.drawPlayer.bodyPosition + drawInfo.bodyVect;
            position = position.Floor();

            _jeanShouldersBack ??= Request<Texture2D>("GenshinMod/Content/Characters/Jean/Textures/Jean_ShouldersBack");

            Rectangle bodyFrame = drawInfo.drawPlayer.bodyFrame;
            Vector2 offset = Vector2.Zero;

            if (drawInfo.drawPlayer.compositeBackArm.enabled)
            {
                bodyFrame.Y = drawInfo.drawPlayer.compositeBackArm.rotation * drawInfo.drawPlayer.direction > MathHelper.ToRadians(-90f) ? 0 : 280;
                offset.Y += drawInfo.drawPlayer.bodyFrame.Y is 392 or 448 or 504 or 784 or 840 or 896 ? -2 : 0;
            }

            DrawData data = new DrawData(
                _jeanShouldersBack.Value,
                position + offset,
                bodyFrame,
                drawInfo.colorArmorBody,
                drawInfo.drawPlayer.bodyRotation,
                drawInfo.bodyVect,
                1f,
                drawInfo.playerEffect)
            {
                shader = drawInfo.cBody
            };

            drawInfo.DrawDataCache.Add(data);
        }

        if (drawInfo.drawPlayer.body == EquipLoader.GetEquipSlot(Mod, "Noelle_Body", EquipType.Body)
            && drawInfo.drawPlayer.body != -1)
        {
            // Vanilla position code
            int drawX = (int)(drawInfo.Position.X - Main.screenPosition.X - drawInfo.drawPlayer.bodyFrame.Width / 2f + drawInfo.drawPlayer.width / 2f);
            int drawY = (int)(drawInfo.Position.Y - Main.screenPosition.Y + drawInfo.drawPlayer.height - drawInfo.drawPlayer.bodyFrame.Height + 4.0);
            Vector2 position = new Vector2(drawX, drawY) + drawInfo.drawPlayer.bodyPosition + drawInfo.bodyVect;
            position = position.Floor();

            _noelleShouldersBack ??= Request<Texture2D>("GenshinMod/Content/Characters/Noelle/Textures/Noelle_ShouldersBack");

            Rectangle bodyFrame = drawInfo.drawPlayer.bodyFrame;
            Vector2 offset = Vector2.Zero;

            if (drawInfo.drawPlayer.compositeBackArm.enabled)
            {
                bodyFrame.Y = drawInfo.drawPlayer.compositeBackArm.rotation * drawInfo.drawPlayer.direction > MathHelper.ToRadians(-90f) ? 0 : 280;
                offset.Y += drawInfo.drawPlayer.bodyFrame.Y is 392 or 448 or 504 or 784 or 840 or 896 ? -2 : 0;
            }

            DrawData data = new DrawData(
                _noelleShouldersBack.Value,
                position + offset,
                bodyFrame,
                drawInfo.colorArmorBody,
                drawInfo.drawPlayer.bodyRotation,
                drawInfo.bodyVect,
                1f,
                drawInfo.playerEffect)
            {
                shader = drawInfo.cBody
            };

            drawInfo.DrawDataCache.Add(data);
        }
    }
}

public class GenshinFrontShoulderLayer_Before : PlayerDrawLayer
{
    private static Asset<Texture2D> _jeanShouldersFront;
    private static Asset<Texture2D> _noelleShouldersFront;

    public override bool IsHeadLayer => false;

    public override Position GetDefaultPosition() => new BeforeParent(PlayerDrawLayers.ArmOverItem);

    public virtual bool BeforeOrAfter => false;

    private static bool GetLayerPosition(PlayerDrawSet drawInfo)
    {
        // When the composite Front Arm is enabled, determine
        // the current rotation of the arm to handle layer position.
        if (drawInfo.drawPlayer.compositeFrontArm.enabled)
        {
            return drawInfo.drawPlayer.compositeFrontArm.rotation * drawInfo.drawPlayer.direction > MathHelper.ToRadians(-90f);
        }

        return drawInfo.compShoulderOverFrontArm;
    }

    public override bool GetDefaultVisibility(PlayerDrawSet drawInfo)
    {
        return !drawInfo.drawPlayer.dead
            && (BeforeOrAfter ? GetLayerPosition(drawInfo) : !GetLayerPosition(drawInfo))
            && drawInfo.drawPlayer.body != -1;
    }

    protected override void Draw(ref PlayerDrawSet drawInfo)
    {
        if (drawInfo.drawPlayer.body == EquipLoader.GetEquipSlot(Mod, "Jean_Body", EquipType.Body)
            && drawInfo.drawPlayer.body != -1)
        {
            // Vanilla position code
            int drawX = (int)(drawInfo.Position.X - Main.screenPosition.X - drawInfo.drawPlayer.bodyFrame.Width / 2f + drawInfo.drawPlayer.width / 2f);
            int drawY = (int)(drawInfo.Position.Y - Main.screenPosition.Y + drawInfo.drawPlayer.height - drawInfo.drawPlayer.bodyFrame.Height + 4.0);
            Vector2 position = new Vector2(drawX, drawY) + drawInfo.drawPlayer.bodyPosition + drawInfo.bodyVect;
            position = position.Floor();

            _jeanShouldersFront ??= Request<Texture2D>("GenshinMod/Content/Characters/Jean/Textures/Jean_ShouldersFront");

            Rectangle bodyFrame = drawInfo.drawPlayer.bodyFrame;
            Vector2 offset = Vector2.Zero;

            if (drawInfo.drawPlayer.compositeFrontArm.enabled)
            {
                bodyFrame.Y = drawInfo.drawPlayer.compositeFrontArm.rotation * drawInfo.drawPlayer.direction > MathHelper.ToRadians(-90f) ? 0 : 56;
                offset.Y += drawInfo.drawPlayer.bodyFrame.Y is 392 or 448 or 504 or 784 or 840 or 896 ? -2 : 0;
            }

            DrawData data = new DrawData(
                _jeanShouldersFront.Value,
                position + offset,
                bodyFrame,
                drawInfo.colorArmorBody,
                drawInfo.drawPlayer.bodyRotation,
                drawInfo.bodyVect,
                1f,
                drawInfo.playerEffect)
            {
                shader = drawInfo.cBody
            };

            drawInfo.DrawDataCache.Add(data);
        }

        if (drawInfo.drawPlayer.body == EquipLoader.GetEquipSlot(Mod, "Noelle_Body", EquipType.Body)
            && drawInfo.drawPlayer.body != -1)
        {
            // Vanilla position code
            int drawX = (int)(drawInfo.Position.X - Main.screenPosition.X - drawInfo.drawPlayer.bodyFrame.Width / 2f + drawInfo.drawPlayer.width / 2f);
            int drawY = (int)(drawInfo.Position.Y - Main.screenPosition.Y + drawInfo.drawPlayer.height - drawInfo.drawPlayer.bodyFrame.Height + 4.0);
            Vector2 position = new Vector2(drawX, drawY) + drawInfo.drawPlayer.bodyPosition + drawInfo.bodyVect;
            position = position.Floor();

            _noelleShouldersFront ??= Request<Texture2D>("GenshinMod/Content/Characters/Noelle/Textures/Noelle_ShouldersFront");

            Rectangle bodyFrame = drawInfo.drawPlayer.bodyFrame;
            Vector2 offset = Vector2.Zero;

            if (drawInfo.drawPlayer.compositeFrontArm.enabled)
            {
                bodyFrame.Y = drawInfo.drawPlayer.compositeFrontArm.rotation * drawInfo.drawPlayer.direction > MathHelper.ToRadians(-90f) ? 0 : 56;
                offset.Y += drawInfo.drawPlayer.bodyFrame.Y is 392 or 448 or 504 or 784 or 840 or 896 ? -2 : 0;
            }

            DrawData data = new DrawData(
                _noelleShouldersFront.Value,
                position + offset,
                bodyFrame,
                drawInfo.colorArmorBody,
                drawInfo.drawPlayer.bodyRotation,
                drawInfo.bodyVect,
                1f,
                drawInfo.playerEffect)
            {
                shader = drawInfo.cBody
            };

            drawInfo.DrawDataCache.Add(data);
        }
    }
}

public class GenshinFrontShoulderLayer_After : GenshinFrontShoulderLayer_Before
{
    public override bool BeforeOrAfter => true;

    public override Position GetDefaultPosition() => new AfterParent(PlayerDrawLayers.ArmOverItem);
}

public class GenshinMiscHeadLayer : PlayerDrawLayer
{
    private static Asset<Texture2D> _klee_Ears;
    private static Asset<Texture2D> _jean_Hairtie;

    public override bool IsHeadLayer => true;

    public override Position GetDefaultPosition() => new AfterParent(PlayerDrawLayers.Head);

    public override bool GetDefaultVisibility(PlayerDrawSet drawInfo)
    {
        return !drawInfo.drawPlayer.dead;
    }

    protected override void Draw(ref PlayerDrawSet drawInfo)
    {
        if (drawInfo.drawPlayer.GetModPlayer<GenshinPlayer>().CurrentCharacterID == GenshinCharacterID.Klee)
        {
            // Vanilla position code
            int drawX = (int)(drawInfo.Position.X - Main.screenPosition.X - drawInfo.drawPlayer.bodyFrame.Width / 2f + drawInfo.drawPlayer.width / 2f);
            int drawY = (int)(drawInfo.Position.Y - Main.screenPosition.Y + drawInfo.drawPlayer.height - drawInfo.drawPlayer.bodyFrame.Height + 4.0);
            Vector2 position = new Vector2(drawX, drawY) + drawInfo.drawPlayer.headPosition + drawInfo.headVect;
            position = position.Floor();

            _klee_Ears ??= Request<Texture2D>("GenshinMod/Content/Characters/Klee/Textures/Klee_Ears");

            DrawData data = new DrawData(
                _klee_Ears.Value,
                position,
                drawInfo.drawPlayer.bodyFrame,
                drawInfo.colorBodySkin,
                drawInfo.drawPlayer.headRotation,
                drawInfo.headVect,
                1f,
                drawInfo.playerEffect)
            {
                shader = drawInfo.skinDyePacked
            };

            drawInfo.DrawDataCache.Add(data);
        }

        if (drawInfo.drawPlayer.GetModPlayer<GenshinPlayer>().CurrentCharacterID == GenshinCharacterID.Jean
            || drawInfo.drawPlayer.GetModPlayer<GenshinPlayer>().CurrentCharacterID == GenshinCharacterID.Terrarian
            && drawInfo.drawPlayer.hair == Content.Characters.Jean.Hair.Jean_Hair.ID
            && drawInfo.drawPlayer.head == -1)
        {
            // Vanilla position code
            int drawX = (int)(drawInfo.Position.X - Main.screenPosition.X - drawInfo.drawPlayer.bodyFrame.Width / 2f + drawInfo.drawPlayer.width / 2f);
            int drawY = (int)(drawInfo.Position.Y - Main.screenPosition.Y + drawInfo.drawPlayer.height - drawInfo.drawPlayer.bodyFrame.Height + 4.0);
            Vector2 position = new Vector2(drawX, drawY) + drawInfo.drawPlayer.headPosition + drawInfo.headVect;
            position = position.Floor();

            _jean_Hairtie ??= Request<Texture2D>("GenshinMod/Content/Characters/Jean/Textures/Jean_Head");

            DrawData data = new DrawData(
                _jean_Hairtie.Value,
                position,
                drawInfo.drawPlayer.bodyFrame,
                drawInfo.colorArmorHead,
                drawInfo.drawPlayer.headRotation,
                drawInfo.headVect,
                1f,
                drawInfo.playerEffect)
            {
                shader = drawInfo.cHead
            };

            drawInfo.DrawDataCache.Add(data);
        }
    }
}

