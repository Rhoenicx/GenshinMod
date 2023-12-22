using GenshinMod.Common.ModObjects.NPCs;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using ReLogic.Content;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;
using static Terraria.ModLoader.ModContent;

namespace GenshinMod.Common.ModObjects.ModSystems;

public class DamageClassSystem : ModSystem
{
    public override void Load()
    {
        // IL Patches for the elemental colors
        IL_NPC.StrikeNPC_HitInfo_bool_bool += IL_CombatText_StrikeNPC;
        IL_Player.Hurt_HurtInfo_bool += IL_CombatText_Hurt;
    }

    public override void Unload()
    {

    }

    #region IL_Patches
    private void IL_CombatText_StrikeNPC(ILContext il)
    {
        ILCursor c = new(il);

        if (c.TryGotoNext(
            x => x.MatchLdloc2(),
            x => x.MatchBrtrue(out _),
            x => x.MatchLdsfld<CombatText>("DamagedFriendly"),
            x => x.MatchBr(out _),
            x => x.MatchLdsfld<CombatText>("DamagedFriendlyCrit")))
        {
            // Define labels for branches
            ILLabel branch = il.DefineLabel();
            ILLabel end = il.DefineLabel();

            // Put the HitInfo struct on the stack
            c.Emit(OpCodes.Ldarg_1);

            // Create delegate to evaluate if we need to modify the color
            c.EmitDelegate<Func<NPC.HitInfo, bool>>((hit) =>
            {
                // If the damagetype of the attack is an Genshin element
                if (hit.DamageType is GenshinDamageClass)
                {
                    // Yes
                    return true;
                }

                // No
                return false;
            });

            // If true => jump over the vanilla color code and
            // branch to our own code
            c.Emit(OpCodes.Brtrue, branch);

            // Move cursor by 5
            c.Index += 5;

            // Branch to the end, jump over our inserted code
            c.Emit(OpCodes.Br, end);

            // Mark our branch
            c.MarkLabel(branch);

            // Put the HitInfo struct on the stack
            c.Emit(OpCodes.Ldarg_1);

            // Create delegate to insert our color
            c.EmitDelegate<Func<NPC.HitInfo, Color>>((hit) =>
            {
                // If the damagetype of the attack is an Genshin element
                if (hit.DamageType is GenshinDamageClass damageClass)
                {
                    // Put the custom color on the stack
                    return damageClass.GetColor();
                }

                // Fail-safe 
                return Color.White;
            });

            // Mark the end branch
            c.MarkLabel(end);
        }

        if (c.TryGotoNext(x => x.MatchLdloc(5)))
        {
            ILLabel branch = il.DefineLabel();
            ILLabel end = il.DefineLabel();

            c.Emit(OpCodes.Ldarg_1);
            c.EmitDelegate<Func<NPC.HitInfo, bool>>((hit) =>
            {
                if (hit.DamageType is GenshinDamageClass)
                {
                    return true;
                }

                return false;
            });

            c.Emit(OpCodes.Brtrue, branch);

            c.Index++;

            c.Emit(OpCodes.Br, end);

            c.MarkLabel(branch);

            c.Emit(OpCodes.Ldarg_1);
            c.EmitDelegate<Func<NPC.HitInfo, Color>>((hit) =>
            {
                if (hit.DamageType is GenshinDamageClass damageClass)
                {
                    return damageClass.GetColor();
                }

                return Color.White;
            });

            c.MarkLabel(end);
        }

    }

    private void IL_CombatText_Hurt(ILContext il)
    {
        ILCursor c = new(il);

        if (c.TryGotoNext(
            x => x.MatchLdloc(5),
            x => x.MatchBrtrue(out _),
            x => x.MatchLdsfld<CombatText>("DamagedFriendly"),
            x => x.MatchBr(out _),
            x => x.MatchLdsfld<CombatText>("DamagedFriendlyCrit")))
        {
            ILLabel branch = il.DefineLabel();
            ILLabel end = il.DefineLabel();

            c.Emit(OpCodes.Ldarg_1);
            c.EmitDelegate<Func<Player.HurtInfo, bool>>((hit) =>
            {
                // Check Projectile Hit
                if (hit.DamageSource.SourceProjectileLocalIndex != -1
                    && Main.projectile[hit.DamageSource.SourceProjectileLocalIndex].DamageType is GenshinDamageClass)
                {
                    return true;
                }

                // Check NPC hit
                if (hit.DamageSource.SourceNPCIndex != -1
                    && Main.npc[hit.DamageSource.SourceNPCIndex].GetGlobalNPC<GenshinGlobalNPC>().damageType is GenshinDamageClass)
                {
                    return true;
                }

                // Check Item hit
                if (hit.DamageSource.SourceItem != null
                    && hit.DamageSource.SourceItem.DamageType is GenshinDamageClass)
                {
                    return true;
                }

                return false;
            });

            c.Emit(OpCodes.Brtrue, branch);

            c.Index += 5;

            c.Emit(OpCodes.Br, end);

            c.MarkLabel(branch);

            c.Emit(OpCodes.Ldarg_1);
            c.EmitDelegate<Func<Player.HurtInfo, Color>>((hit) =>
            {
                // Check Projectile Hit
                {
                    if (hit.DamageSource.SourceProjectileLocalIndex != -1
                        && Main.projectile[hit.DamageSource.SourceProjectileLocalIndex].DamageType is GenshinDamageClass damageClass)
                    {
                        return damageClass.GetColor();
                    }
                }

                // Check NPC hit
                {
                    if (hit.DamageSource.SourceNPCIndex != -1
                        && Main.npc[hit.DamageSource.SourceNPCIndex].GetGlobalNPC<GenshinGlobalNPC>().damageType is GenshinDamageClass damageClass)
                    {
                        return damageClass.GetColor();
                    }
                }

                // Check Item hit
                {
                    if (hit.DamageSource.SourceItem != null
                        && hit.DamageSource.SourceItem.DamageType is GenshinDamageClass damageClass)
                    {
                        return damageClass.GetColor();
                    }
                }
                return Color.White;
            });

            c.MarkLabel(end);
        }
    }

    #endregion

}

#region ElementApplication
public class GenshinElementApplication
{
    public const float Weak = 1f;
    public const float Medium = 1.5f;
    public const float Strong = 2f;
    public const float VeryStrong = 4f;
    public const float Extreme = 8f;
}
#endregion


#region DamageClasses
public abstract class GenshinDamageClass : DamageClass
{
    // The color of this DamageText popup
    public abstract Color GetColor();
    public abstract bool CanDrawIcon();
    protected virtual Asset<Texture2D> GetIconTexture() => TextureAssets.MagicPixel;

    // Inheritance of stats, do not inherit any other stats from damage classes other than GenshinGeneric
    public override StatInheritanceData GetModifierInheritance(DamageClass damageClass)
    {
        return damageClass == ModContent.GetInstance<GenshinGeneric>() ? StatInheritanceData.Full : StatInheritanceData.None;
    }

    // Draw the icon above NPC
    public void DrawIcon(SpriteBatch spriteBatch, Vector2 position, Color drawColor)
    {
        if (!CanDrawIcon())
        {
            return;
        }

        Texture2D iconTexture = GetIconTexture().Value;

        spriteBatch.Draw(
            iconTexture,
            position,
            iconTexture.Bounds,
            drawColor,
            0f,
            iconTexture.Size() * 0.5f,
            1f,
            SpriteEffects.None,
            0);
    }
}

public class GenshinGeneric : DamageClass
{
    public override StatInheritanceData GetModifierInheritance(DamageClass damageClass) => StatInheritanceData.None;
}

public class GenshinPhysical : GenshinDamageClass
{
    private Color _color = new(200, 200, 200);
    public override Color GetColor() => _color;
    public override bool CanDrawIcon() => false;
}

public class GenshinPyro : GenshinDamageClass
{
    private Color _color = new(255, 155, 0);
    private Asset<Texture2D> _iconTexture;
    public override Color GetColor() => _color;
    public override bool CanDrawIcon() => true;
    protected override Asset<Texture2D> GetIconTexture()
    {
        _iconTexture ??= Request<Texture2D>("GenshinMod/Common/UI/ElementIcons/Icon_Pyro");
        return _iconTexture;
    }
}

public class GenshinHydro : GenshinDamageClass
{
    private Color _color = new(51, 204, 255);
    private Asset<Texture2D> _iconTexture;
    public override Color GetColor() => _color;
    public override bool CanDrawIcon() => true;
    protected override Asset<Texture2D> GetIconTexture()
    {
        _iconTexture ??= Request<Texture2D>("GenshinMod/Common/UI/ElementIcons/Icon_Hydro");
        return _iconTexture;
    }
}

public class GenshinCryo : GenshinDamageClass
{
    private Color _color = new(153, 255, 255);
    private Asset<Texture2D> _iconTexture;
    public override Color GetColor() => _color;
    public override bool CanDrawIcon() => true;
    protected override Asset<Texture2D> GetIconTexture()
    {
        _iconTexture ??= Request<Texture2D>("GenshinMod/Common/UI/ElementIcons/Icon_Cryo");
        return _iconTexture;
    }
}

public class GenshinAnemo : GenshinDamageClass
{
    private Color _color = new(102, 255, 204);
    private Asset<Texture2D> _iconTexture;
    public override Color GetColor() => _color;
    public override bool CanDrawIcon() => true;
    protected override Asset<Texture2D> GetIconTexture()
    {
        _iconTexture ??= Request<Texture2D>("GenshinMod/Common/UI/ElementIcons/Icon_Anemo");
        return _iconTexture;
    }
}

public class GenshinGeo : GenshinDamageClass
{
    private Color _color = new(255, 204, 102);
    private Asset<Texture2D> _iconTexture;
    public override Color GetColor() => _color;
    public override bool CanDrawIcon() => true;
    protected override Asset<Texture2D> GetIconTexture()
    {
        _iconTexture ??= Request<Texture2D>("GenshinMod/Common/UI/ElementIcons/Icon_Geo");
        return _iconTexture;
    }
}

public class GenshinDendro : GenshinDamageClass
{
    private Color _color = new(0, 234, 82);
    private Asset<Texture2D> _iconTexture;
    public override Color GetColor() => _color;
    public override bool CanDrawIcon() => true;
    protected override Asset<Texture2D> GetIconTexture()
    {
        _iconTexture ??= Request<Texture2D>("GenshinMod/Common/UI/ElementIcons/Icon_Dendro");
        return _iconTexture;
    }
}

public class GenshinElectro : GenshinDamageClass
{
    private Color _color = new(225, 155, 255);
    private Asset<Texture2D> _iconTexture;
    public override Color GetColor() => _color;
    public override bool CanDrawIcon() => true;
    protected override Asset<Texture2D> GetIconTexture()
    {
        _iconTexture ??= Request<Texture2D>("GenshinMod/Common/UI/ElementIcons/Icon_Electro");
        return _iconTexture;
    }
}

public class GenshinVaporize : GenshinDamageClass
{
    private Color _color = new(255, 204, 102);
    public override Color GetColor() => _color;
    public override bool CanDrawIcon() => false;
}

public class GenshinOverloaded : GenshinDamageClass
{
    private Color _color = new(255, 128, 155);
    public override Color GetColor() => _color;
    public override bool CanDrawIcon() => false;
}

public class GenshinMelt : GenshinDamageClass
{
    private Color _color = new(255, 204, 102);
    public override Color GetColor() => _color;
    public override bool CanDrawIcon() => false;
}

public class GenshinElectrocharged : GenshinDamageClass
{
    private Color _color = new(225, 155, 255);
    public override Color GetColor() => _color;
    public override bool CanDrawIcon() => false;
}

public class GenshinFrozen : GenshinDamageClass
{
    private Color _color = new(153, 255, 255);
    public override Color GetColor() => _color;
    public override bool CanDrawIcon() => false;
}

public class GenshinSuperconduct : GenshinDamageClass
{
    private Color _color = new(180, 180, 255);
    public override Color GetColor() => _color;
    public override bool CanDrawIcon() => false;
}

public class GenshinSwirl : GenshinDamageClass
{
    private Color _color = new(102, 255, 204);
    public override Color GetColor() => _color;
    public override bool CanDrawIcon() => false;
}

public class GenshinCrystallize : GenshinDamageClass
{
    private Color _color = new(255, 204, 102);
    public override Color GetColor() => _color;
    public override bool CanDrawIcon() => false;
}

public class GenshinBurning : GenshinDamageClass
{
    private Color _color = new(255, 255, 255);
    public override Color GetColor() => _color;
    public override bool CanDrawIcon() => false;
}

public class GenshinBloom : GenshinDamageClass
{
    private Color _color = new(0, 234, 82);
    public override Color GetColor() => _color;
    public override bool CanDrawIcon() => false;
}

public class GenshinHyperbloom : GenshinDamageClass
{
    private Color _color = new(225, 155, 255);
    public override Color GetColor() => _color;
    public override bool CanDrawIcon() => false;
}

public class GenshinBurgeon : GenshinDamageClass
{
    private Color _color = new(255, 155, 0);
    public override Color GetColor() => _color;
    public override bool CanDrawIcon() => false;
}

public class GenshinQuicken : GenshinDamageClass
{
    private Color _color = new(0, 234, 82);
    public override Color GetColor() => _color;
    public override bool CanDrawIcon() => false;
}

public class GenshinAggravate : GenshinDamageClass
{
    private Color _color = new(225, 155, 255);
    public override Color GetColor() => _color;
    public override bool CanDrawIcon() => false;
}

public class GenshinSpread : GenshinDamageClass
{
    private Color _color = new(0, 234, 82);
    public override Color GetColor() => _color;
    public override bool CanDrawIcon() => false;
}

public class GenshinShatter : GenshinDamageClass
{
    private Color _color = new(255, 255, 255);
    public override Color GetColor() => _color;
    public override bool CanDrawIcon() => false;
}

#endregion

