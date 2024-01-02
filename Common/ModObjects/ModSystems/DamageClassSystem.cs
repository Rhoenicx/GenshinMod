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

#region AttackTypes
public enum AttackType : byte
{ 
    None,
    NormalAttack,
    ChargedAttack,
    ElementalSkill,
    ElementalBurst,
    Special
}

#endregion

#region AttackWeight
public enum AttackWeight : byte 
{ 
    None,
    Arrow,
    Light,
    Medium,
    Strong,
    VeryStrong
}
#endregion

#region ElementApplication
public enum ElementApplication : byte
{ 
    None, // 0U
    Weak, // 1U
    Medium, // 1.5U
    Strong, // 2U
    VeryStrong, // 4U
    Extreme // 8U
}
#endregion

#region DamageClasses
public abstract class GenshinDamageClass : DamageClass
{
    // The color of this DamageText popup
    public abstract Color GetColor();

    // Inheritance of stats, do not inherit any other stats from damage classes other than GenshinGeneric
    public override StatInheritanceData GetModifierInheritance(DamageClass damageClass)
    {
        return damageClass == ModContent.GetInstance<GenshinGeneric>() ? StatInheritanceData.Full : StatInheritanceData.None;
    }
}

public abstract class GenshinElementDamageClass : GenshinDamageClass
{
    public virtual bool CanDrawIcon() => false;

    protected virtual Asset<Texture2D> GetIconTexture() => TextureAssets.MagicPixel;

    public void DrawIcon(SpriteBatch spriteBatch, Vector2 position, Color drawColor)
    {
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

public abstract class GenshinReactionDamageClass : GenshinDamageClass
{ 

}

public class GenshinGeneric : DamageClass
{
    public override StatInheritanceData GetModifierInheritance(DamageClass damageClass) => StatInheritanceData.None;
}

public class GenshinPhysical : GenshinElementDamageClass
{
    private Color _color = new(200, 200, 200);
    public override Color GetColor() => _color;
    public override bool CanDrawIcon() => false;
}

public class GenshinPyro : GenshinElementDamageClass
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

public class GenshinHydro : GenshinElementDamageClass
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

public class GenshinCryo : GenshinElementDamageClass
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

public class GenshinAnemo : GenshinElementDamageClass
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

public class GenshinGeo : GenshinElementDamageClass
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

public class GenshinDendro : GenshinElementDamageClass
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

public class GenshinElectro : GenshinElementDamageClass
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

public class GenshinVaporize : GenshinReactionDamageClass
{
    private Color _color = new(255, 204, 102);
    public override Color GetColor() => _color;
}

public class GenshinOverloaded : GenshinReactionDamageClass
{
    private Color _color = new(255, 128, 155);
    public override Color GetColor() => _color;
}

public class GenshinMelt : GenshinReactionDamageClass
{
    private Color _color = new(255, 204, 102);
    public override Color GetColor() => _color;
}

public class GenshinElectrocharged : GenshinReactionDamageClass
{
    private Color _color = new(225, 155, 255);
    public override Color GetColor() => _color;
}

public class GenshinFrozen : GenshinReactionDamageClass
{
    private Color _color = new(153, 255, 255);
    public override Color GetColor() => _color;
}

public class GenshinSuperconduct : GenshinReactionDamageClass
{
    private Color _color = new(180, 180, 255);
    public override Color GetColor() => _color;
}

public class GenshinSwirl : GenshinReactionDamageClass
{
    private Color _color = new(102, 255, 204);
    public override Color GetColor() => _color;
}

public class GenshinCrystallize : GenshinReactionDamageClass
{
    private Color _color = new(255, 204, 102);
    public override Color GetColor() => _color;
}

public class GenshinBurning : GenshinReactionDamageClass
{
    private Color _color = new(255, 255, 255);
    public override Color GetColor() => _color;
}

public class GenshinBloom : GenshinReactionDamageClass
{
    private Color _color = new(0, 234, 82);
    public override Color GetColor() => _color;
}

public class GenshinHyperbloom : GenshinReactionDamageClass
{
    private Color _color = new(225, 155, 255);
    public override Color GetColor() => _color;
}

public class GenshinBurgeon : GenshinReactionDamageClass
{
    private Color _color = new(255, 155, 0);
    public override Color GetColor() => _color;
}

public class GenshinQuicken : GenshinReactionDamageClass
{
    private Color _color = new(0, 234, 82);
    public override Color GetColor() => _color;
}

public class GenshinAggravate : GenshinReactionDamageClass
{
    private Color _color = new(225, 155, 255);
    public override Color GetColor() => _color;
}

public class GenshinSpread : GenshinReactionDamageClass
{
    private Color _color = new(0, 234, 82);
    public override Color GetColor() => _color;
}

public class GenshinShatter : GenshinReactionDamageClass
{
    private Color _color = new(255, 255, 255);
    public override Color GetColor() => _color;
}

#endregion