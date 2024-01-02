using Microsoft.Xna.Framework;
using GenshinMod.Common.ModObjects.ModSystems;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.WorldBuilding;
using System.Collections.Generic;
using GenshinMod.Common.ModObjects.Shields;
using System.Linq;
using Microsoft.Xna.Framework.Graphics;
using System;
using GenshinMod.Common.ModObjects.Weapons;
using GenshinMod.Common.ModObjects.Players;
using GenshinMod.Common.ModObjects.Projectiles;
using GenshinMod.Common.GameObjects;
using Microsoft.CodeAnalysis;

namespace GenshinMod.Common.ModObjects.NPCs
{
    public class GenshinGlobalNPC : GlobalNPC
    {
        // Enable instance objects for this NPC
        public override bool InstancePerEntity => true;

        //----- Standard -----
        public int Level;                                                       // Level of this NPC (1-100)
        public float ReductionDefense;                                          // Add to deal more damage with all attacks
        public float BaseKnockBackResistance = 1f;                              // The amount of knockback resistance this NPC has
        public bool DrawNPCLevel = false;                                       // Should a level text be drawn above the npc?
        public Vector2 DrawNPCLevelOffset = Vector2.Zero;                       // The additional offset where the level text will be drawn

        //----- Shields -----
        public List<GenshinShieldNPC> Shields;                                  // List of shields on this NPC
        public bool DrawShieldHealth = true;                                    // Should a shield health bar be drawn under the npc health bar?
        public Vector2 DrawShieldHealthOffset = Vector2.Zero;                   // The additional offset where the shield healthbar will be drawn

        //----- Damage -----
        public DamageClass damageType = DamageClass.Default;                    // The damage type this NPC inflicts with a melee attack / contact dmg
        public bool BluntTarget = false;                                        // is the target more susceptible to heavy attacks ?

        //----- Energy -----
        public bool GiveEnergyParticlesLife = true;                             // should the NPC release particles at half and 0 health
        public bool GiveEnergyParticlesHit = true;                              // should the NPC release particles when hit by projectiles
        private bool _halfLifeParticle = false;                                 // Keeps track if we already spawned half life particles when enabled           

        //----- Elements -----
        public Dictionary<DamageClass, float> ElementalResistance = new();             // Base resistance to an Element, 0f = 100% damage taken, 1f+ = immune
        public Dictionary<DamageClass, float> ReductionElementalResistance = new();    // Redution of elemental resistance, 0f = reduce (base) resistance by 0%, 1f = reduce (base) resistance by 100%
        public Dictionary<DamageClass, int> ElementalTimer = new();
        public Dictionary<DamageClass, int> ElementalDecayRate = new();

        private int _hitsSwirl;
        private int _hitsSuperconduct;

        public override void PostDraw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            // -------------- Draw the element Icons --------------- //

            // Get the elements that affect this NPC
            List<DamageClass> affectedElements = ElementalTimer.Keys
                .Where(d => ElementalTimer[d] > 0 
                && d is GenshinElementDamageClass damageClass 
                && damageClass.CanDrawIcon()).ToList();

            // Add shields
            if (HasShield())
            {
                foreach (GenshinShieldNPC shield in Shields)
                {
                    if (!affectedElements.Contains(shield.DamageType())
                        && shield.DamageType() is GenshinElementDamageClass damageClass
                        && damageClass.CanDrawIcon())
                    {
                        affectedElements.Add(shield.DamageType());
                    }
                }
            }

            // Count the amount of elements
            int count = affectedElements.Count;

            // Constant offset between multiple icons
            const int elementSymbolDrawOffsetX = 30;
            const int elementSymbolDrawOffsetY = 30;

            // Start position above NPC
            Vector2 position = npc.Top + new Vector2(-count * elementSymbolDrawOffsetX / 2, -elementSymbolDrawOffsetY) - screenPos;

            // Draw the icons
            for (int i = 0; i < count; i++)
            {
                if (affectedElements[i] is GenshinElementDamageClass damageClass)
                {
                    damageClass.DrawIcon(spriteBatch, position + new Vector2(elementSymbolDrawOffsetX, 0f) * i, drawColor);
                }
            }

        }

        public override void SetDefaults(NPC npc)
        {
            // NPC Setup
            BaseKnockBackResistance = npc.knockBackResist;

            // Resistances example
            ElementalResistance = new Dictionary<DamageClass, float>() { { ModContent.GetInstance<GenshinHydro>(), 0f } };
        }

        public override void ResetEffects(NPC npc)
        {
            // Reduce elemental timer, <= 0 is no element applied
            ElementalTimer ??= new();
            foreach (DamageClass damageType in ElementalTimer.Keys.Where(damageType => ElementalTimer[damageType] > 0))
            {
                // Reduce the timer with the corresponding decay rate
                ElementalDecayRate ??= new();
                ElementalTimer[damageType] = ElementalDecayRate.TryGetValue(damageType, out int value)
                    ? ElementalTimer[damageType] - value : 0;

                // Reset Swirl reaction hits
                if (damageType == ModContent.GetInstance<GenshinSwirl>()
                    && ElementalTimer[damageType] <= 0)
                {
                    _hitsSwirl = 0;
                }

                // Reset Superconduct reaction hits
                if (damageType == ModContent.GetInstance<GenshinSuperconduct>()
                    && ElementalTimer[damageType] <= 0)
                {
                    _hitsSuperconduct = 0;
                }
            }

            // Set base resistances back to 0f
            ReductionElementalResistance ??= new();
            foreach (DamageClass damageType in ReductionElementalResistance.Keys)
            {
                ReductionElementalResistance[damageType] = 0f;
            }
            ReductionDefense = 0f;

            // Reset knockBackResistance
            npc.knockBackResist = BaseKnockBackResistance;

            // Execute logic for shields
            Shields ??= new();
            for (int i = Shields.Count - 1; i >= 0; i--)
            {
                // ResetEffects on shield
                Shields[i].ResetEffects();

                // If shield has been broken but is
                // still in the list => kill&Remove
                if (!Shields[i].Active)
                {
                    Shields[i].Kill();
                    Shields.RemoveAt(i);
                }
            }

            base.ResetEffects(npc);
        }

        public override void ModifyHitByItem(NPC npc, Player player, Item item, ref NPC.HitModifiers modifiers)
        {
            // The item is not a GenshinProjectile => return
            if (item.ModItem is not GenshinWeapon genshinWeapon)
            {
                return;
            }
        }

        public override void ModifyHitByProjectile(NPC npc, Projectile projectile, ref NPC.HitModifiers modifiers)
        {
            // The projectile is not a GenshinProjectile => return
            if (projectile.ModProjectile is not GenshinProjectile genshinProjectile)
            {
                return;
            }

            // The owner of this projectile is a Player
            if (!genshinProjectile.BelongsToNPC
                && Main.player[projectile.owner].active
                && genshinProjectile.CharacterOwnerID != GenshinCharacterID.None
                && Main.player[projectile.owner].TryGetModPlayer(out GenshinPlayer genshinPlayer)
                && genshinPlayer.GenshinModeEnabled
                && genshinPlayer.TryGetTeamCharacter(genshinProjectile.CharacterOwnerID, out GenshinCharacter genshinCharacter))
            {
                // Re-calculate the base damage of the projectile
                projectile.damage = (int)(genshinCharacter.GetBaseDamage(genshinProjectile.HitType)
                    * genshinCharacter.GetBaseDamageMultiplier())
                    + genshinCharacter.GetAdditiveBaseDamageBonus();

                return;
            }

            // The Owner of this projectile is an NPC
            if (genshinProjectile.BelongsToNPC
                && Main.npc[genshinProjectile.NPCOwnerID].active
                && Main.npc[genshinProjectile.NPCOwnerID].TryGetGlobalNPC(out GenshinGlobalNPC genshinGlobalNPC))
            {
                // Re-calculate the base damage of the projectile
                projectile.damage = (int)(genshinGlobalNPC.GetBaseDamage(genshinProjectile.HitType) 
                    * genshinGlobalNPC.GetBaseDamageMultiplier())
                    + genshinGlobalNPC.GetAdditiveBaseDamageBonus();

                return;
            }
        }

        /// <summary>
        /// Part of the damage calculation: Gets the 'Base DMG'
        /// of an attack. Place a switch with each AttackType
        /// in here, and return the Base Damage. This can
        /// scale with ATK, DEF, HP, EM etc...
        /// Returns 1 by default.
        /// </summary>
        public virtual int GetBaseDamage(AttackType hitType)
        {
            return 1;
        }

        public virtual float GetBaseDamageMultiplier()
        {
            return 1f;
        }

        public virtual int GetAdditiveBaseDamageBonus()
        {
            return 0;
        }

        /// <summary>
        /// Calculates the amount of additional passive defense this NPC should
        /// have over under-leveled characters.
        /// </summary>
        public float GetDefenseMultiplier(float defIgnore, int characterLevel) =>
            (characterLevel + 100) / (characterLevel + 100 + (Level + 100) * (1f - Math.Min(ReductionDefense, 0.9f)) * (1f - defIgnore));

        /// <summary>
        /// Gets the resistance this NPC has against the given damageType
        /// </summary>
        public float GetResistanceMultiplier(DamageClass damageType) => 
            ElementalResistance != null 
            && ElementalResistance.TryGetValue(damageType, out float resistance) 
            ? resistance : 0f;

        /// <summary>
        /// Checks whether this NPC is affected by the given damageType (element),
        /// use ignoreShield to get only the elements that affect the NPC.
        /// If the NPC has a shield and ignoreShield is false it'll grab
        /// the element of the top shield.
        /// </summary>
        public bool AffectedByElement(DamageClass damageType, bool ignoreShield = false) =>
            !ignoreShield && HasShield() && TopShieldElement() == damageType 
            || ElementalTimer != null && ElementalTimer.TryGetValue(damageType, out int timer) && timer > 0;

        /// <summary>
        /// Tries to prepare all the data before an actual Element will
        /// be applied to the target.
        /// </summary>
        private void PrepareHitWithItem(NPC npc, Player player, Item item, ref NPC.HitModifiers modifiers)
        {
            // Prepare variables
            GenshinPlayer genshinPlayer = player.GetModPlayer<GenshinPlayer>();
            GenshinCharacterID characterID = GenshinCharacterID.None;
            GenshinWeaponType weaponType = GenshinWeaponType.None;
            ElementApplication application = ElementApplication.None;
            AttackType attackType = AttackType.None;
            AttackWeight attackWeight = AttackWeight.None;

            // The Item that hit the target does not belong to our mod
            if (item.ModItem is not GenshinWeapon genshinWeapon
                || !genshinPlayer.Player.active)
            {
                return;
            }

            // Get the stats of the Item
            genshinWeapon.GetWeaponStats(out characterID, out weaponType, out application, out attackType, out attackWeight);
        }

        private void PrepareHitWithProjectile(NPC npc, Projectile projectile, ref NPC.HitModifiers modifiers)
        {
            // Prepare variables
            GenshinPlayer genshinPlayer = Main.player[projectile.owner].GetModPlayer<GenshinPlayer>();
            GenshinCharacterID characterID = GenshinCharacterID.None;
            GenshinWeaponType weaponType = GenshinWeaponType.None;
            ElementApplication application = ElementApplication.None;
            AttackType attackType = AttackType.None;
            AttackWeight attackWeight = AttackWeight.None;

            // The projectile that hit the target does not belong to our mod
            if (projectile.ModProjectile is not GenshinProjectile genshinProjectile)
            {
                return;
            }

            // The hit is caused by the server (no players and characters) or from another NPC
            if (!genshinPlayer.Player.active)

            // get the stats of the projectile
            genshinProjectile.GetProjectileStats(out characterID, out weaponType, out application, out attackType, out attackWeight);
        }

        private void ApplyHit(NPC npc, ref NPC.HitModifiers modifiers, DamageClass element, float application, float auraTax = 1f)
        { 
        
        }


        /// <summary>
        /// Checks if this NPC has any shields
        /// </summary>
        public bool HasShield() => Shields != null && Shields.Any();

        /// <summary>
        /// Gets the damageType (element) of the top shield, if present.
        /// Otherwise it return DamageClass.Default
        /// </summary>
        public DamageClass TopShieldElement() => Shields != null && Shields.Any() ? Shields.Last().DamageType() : DamageClass.Default;
    }
}
