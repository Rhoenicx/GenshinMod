using GenshinMod.Common.ModObjects.ModSystems;
using GenshinMod.Common.ModObjects.Players;
using System.IO;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace GenshinMod.Common.GameObjects;

public abstract class GenshinCharacter
{
    #region Variables
    //-------------------------------------------------------------------------
    //-------------------------- Automatic variables --------------------------
    //-------------------------------------------------------------------------

    public GenshinCharacterID CharacterID;
    public Player Player;
    public GenshinPlayer GenshinPlayer => !IsDummyCharacter ? Player.GetModPlayer<GenshinPlayer>() : null;
    public bool IsDummyCharacter => Player == null;
    public bool IsOnField => GenshinPlayer.CurrentCharacterID == CharacterID;

    // 

    //-------------------------------------------------------------------------
    //--------------------------------- Stats ---------------------------------
    //-------------------------------------------------------------------------

    // Character details
    public GenshinRarity Rarity;
    public GenshinWeaponType WeaponType;
    public DamageClass Vision;

    // The levels of the character
    public const int LevelMax = 90;
    public int Level = 1;
    public long Experience = 0;

    // NA Attack 
    public int NormalAttackAmount = 1;
    public int CurrentAttack = 0;
    public bool AutoswingNA = true; // NA autoswing
    public bool AutoRepeatNACombo = false; // Automatically repeats NA combo

    // CA Attack
    public int StaminaChargedAttack = 1;
    public bool AutoswingCA = false; // CA autoswing
    public bool AutoRepeatCACombo = false; // Automatically repeats CA combo
    public bool HoldableSkill = false; // Skill hold

    // Talents
    public const int TalentLevelMax = 10;
    public int TalentLevelAttack = 1;
    public int TalentLevelElementalSkill = 1;
    public int TalentLevelElementalBurst = 1;

    // Ascension
    public int Ascension = 0;
    public bool FirstAscensionPassiveUnlocked => Ascension >= 1;
    public bool SecondAscensionPassiveUnlocked => Ascension >= 4;

    // HP/DEF/ATK stats of the character
    public int BaseHealthMax = 100;
    public int BaseDefenseMax = 100;
    public int BaseAttackMax = 100;

    public int AscensionHealthMax = 100;
    public int AscensionDefenseMax = 100;
    public int AscensionAttackMax = 100;

    public int BaseHealth => (int)(BaseHealthMax * LevelMultiplier() + AscensionHealthMax * AscensionSection());
    public int BaseDefense => (int)(BaseDefenseMax * LevelMultiplier() + AscensionHealthMax * AscensionSection());
    public int BaseAttack => (int)(BaseAttackMax * LevelMultiplier() + AscensionHealthMax * AscensionSection());

    public int EffectiveHealth => (int)(BaseHealth * (1f + StatHealthPercentage)) + StatHealthFlat;
    public int EffectiveDefense => (int)(BaseDefense * (1f + StatDefensePercentage)) + StatDefenseFlat;
    public int EffectiveAttack => (int)(BaseAttack * (1f * StatAttackPercentage)) + StatAttackFlat;

    // Generic stats
    public float StatAttackPercentage = 1f; // Bonus Attack% (base = 0%)
    public int StatAttackFlat = 0; // Bonus flat damage (base = 0)
    public float StatHealthPercentage = 1f; // Health bonus % (of FlatHealth, base = 0%)
    public int StatHealthFlat = 0; // Bonus flat health (base = 0)
    public float StatDefensePercentage = 1f; // Defense bonus % (of FlatDefense, base = 0%)
    public int StatDefenseFlat = 0; // Bonus flat defense (base = 0)
    public int StatElementalMastery = 0; // Elemental mastery (base = 0)
    public float StatEnergyRecharge = 1f; // Energy Recharge % (base = 100%)
    public float StatCritRate = 0.05f; // Critical Strike Chance (base = 0.05f, cap = 1f = 100%)
    public float StatCritDamage = 0.50f; // Critical Strike damage bonus (base = 0.5f)
    public float StatAttackSpeed = 1f; // Attack speed (base = 100%)

    // Damage stats
    public float StatDamage = 0f; // Bonus Damage% (base = 0%)
    public float StatDamagePhysical = 0f; // Bonus Physical Damage% (base = 0%)
    public float StatDamageGeo = 0f; // Bonus Geo Damage% (base = 0%)
    public float StatDamageAnemo = 0f; // Bonus Anemo Damage% (base = 0%)
    public float StatDamageCryo = 0f; // Bonus Cryo Damage% (base = 0%)
    public float StatDamageElectro = 0f; // Bonus Electro Damage% (base = 0%)
    public float StatDamageDendro = 0f; // Bonus Dencro Damage% (base = 0%)
    public float StatDamageHydro = 0f; // Bonus Hydro Damage% (base = 0%)
    public float StatDamagePyro = 0f; // Bonus Pyro Damage% (base = 0%)
    public float StatDamageNA = 0f; // Bonus NA Damage% (base = 0%)
    public float StatDamageCA = 0f; // Bonus CA Damage% (base = 0%)
    public float StatDamageSkill = 0f; // Bonus Skill Damage% (base = 0%)
    public float StatDamageBurst = 0f; // Bonus Burst Damage% (base = 0%)
    public float StatHealingBonus = 0f; // Bonus Healing Done% (base = 0%)
    public float StatHealingReceived = 0f; // Bonus Healind Received% (base = 0%)
    public float StatShieldStrength = 0f; // Bonus Shield Strength% (base = 0%)

    // Damage reactions
    public float StatDamageReactionAll = 0f; // Bonus Reaction Damage (base = 0%)
    public float StatDamageReactionVaporize = 0f; // Bonus Vaporize Reaction Damage (base = 0%)
    public float StatDamageReactionOverloaded = 0f; // Bonus Overloaded Reaction Damage (base = 0%)
    public float StatDamageReactionMelt = 0f; // Bonus Melt Reaction Damage (base = 0%)
    public float StatDamageReactionElectrocharged = 0f; // Bonus Electrocharged Reaction Damage (base = 0%)
    public float StatDamageReactionSuperconduct = 0f; // Bonus Superconduct Reaction Damage (base = 0%)
    public float StatDamageReactionSwirl = 0f; // Bonus Swirl Reaction Damage (base = 0%)
    public float StatDamageReactionBurning = 0f; // Bonus Burning Reaction Damage (base = 0%)
    public float StatDamageReactionBloom = 0f; // Bonus Bloom Reaction Damage (base = 0%)
    public float StatDamageReactionHyperbloom = 0f; // Bonus Hyperbloom Reaction Damage (base = 0%)
    public float StatDamageReactionBurgeon = 0f; // Bonus Burgeon Reaction Damage (base = 0%)
    public float StatDamageReactionQuicken = 0f; // Bonus Quicken Reaction Damage (base = 0%)
    public float StatDamageReactionAggravate = 0f; // Bonus Aggravate Reaction Damage (base = 0%)
    public float StatDamageReactionSpread = 0f; // Bonus Spread Reaction Damage (base = 0%)
    public float StatDamageReactionShatter = 0f; // Bonus Shatter Reaction Damage (base = 0%)
    public float StatDamageReactionFrozen = 0f; // Bonus Frozen Duration (base = 0%)
    public float StatDamageReactionCrystallize = 0f; // Bonus Crystallize Reaction Shield Value (base = 0%)

    // Resistances
    public float StatResistanceGeo = 0f; // 0f = 100% damage taken by Geo, 1f = immune, can be negative to take more damage (base = 0%)
    public float StatResistanceAnemo = 0f; // 0f = 100% damage taken by Anemo, 1f = immune, can be negative to take more damage (base = 0%)
    public float StatResistanceCryo = 0f; // 0f = 100% damage taken by Cryo, 1f = immune, can be negative to take more damage (base = 0%)
    public float StatResistanceElectro = 0f; // 0f = 100% damage taken by Electro, 1f = immune, can be negative to take more damage (base = 0%)
    public float StatResistanceDendro = 0f; // 0f = 100% damage taken by Dendro, 1f = immune, can be negative to take more damage (base = 0%)
    public float StatResistanceHydro = 0f; // 0f = 100% damage taken by Hydro, 1f = immune, can be negative to take more damage (base = 0%)
    public float StatResistancePyro = 0f; // 0f = 100% damage taken by Pyro, 1f = immune, can be negative to take more damage (base = 0%)
    public float StatResistancePhysical = 0f; // 0f = 100% damage taken by Physical (or "none"), 1f = immune, can be negative to take more damage (base = 0%)

    public float WeaponSize = 1f; // Character weapon size multiplier

    // Weapon object the character is currently holding
    //public GenshinWeapon EquippedWeapon;


    // Other
    public bool IsAlive => Health > 0;
    public int Health = 1000;

    #endregion

    /// <summary>
    /// The constructor of the abstract character class, runs
    /// when a character object is created. Used to set the 
    /// CharacterID and player owning this character object.
    /// Initializes and resets the object.
    /// </summary>
    protected GenshinCharacter(GenshinCharacterID id, Player player = null)
    {
        // Assign the ID
        CharacterID = id;

        // Assign player
        Player = player;

        // Initialize data structures upon creation of this object
        Initialize();

        // Apply defaults of this character
        SetDefaults();
    }

    /// <summary>
    /// Use this to run initialization of data structures
    /// whenever this character object is created.
    /// </summary>
    public virtual void Initialize()
    {

    }

    /// <summary>
    /// Use this to load static character data
    /// when the mod is loading. Data structures, textures etc.
    /// Gets called once when the mod loads.
    /// </summary>
    public virtual void Load()
    {

    }

    /// <summary>
    /// Use this to unload/clear static character 
    /// data when the mod is loading. Data structures etc.
    /// Gets called once when the mod unloads.
    /// </summary>
    public virtual void Unload()
    { 
    
    }

    /// <summary>
    /// Use this to save character specific data.
    /// Equipement, artifacts, level, talents etc.
    /// Gets called through the player owning this
    /// character.
    /// </summary>
    public void SaveData(TagCompound tag)
    {

    }

    /// <summary>
    /// Use this to load character specific data.
    /// Equipement, artifacts, level, talents etc.
    /// Gets called through the player owning this
    /// character.
    /// </summary>
    public void LoadData(TagCompound tag)
    {

    }

    /// <summary>
    /// Resets all the characters stats back to their defaults
    /// Gets called whenever this character object is created
    /// </summary>
    public virtual void SetDefaults()
    { 
    
    }

    /// <summary>
    /// Runs when the player owning this character enters a 
    /// world. Here the Player object is assigned again.
    /// This serves as a fix in multiplayer.
    /// </summary>
    public virtual void OnEnterWorld(Player player)
    {
        Player = player;
    }

    /// <summary>
    /// Disables the character, this hook should be used
    /// to reset/purge all running effects on this character.
    /// Runs whenever this character is no longer controllable
    /// in multiplayer.
    /// Note that characters that are not controllable will no
    /// longer controllable will not be updated.
    /// </summary>
    public virtual void Disable()
    {
        // Terminate if this is the Terrarian character 
        if (CharacterID == GenshinCharacterID.Terrarian)
        {
            return;
        }
    }

    /// <summary>
    /// This hook requests the value of the Equipment slot with the
    /// 'type' argument. With this hook the visible accessories/armor/vanity 
    /// are requested from the current displayed character.
    /// This hook is called from IL.
    /// Make sure every slot is properly set up in either an ModItem or
    /// in the character's Load() function.
    /// Returns -1 (nothing equipped) by default.
    /// </summary>
    public virtual int GetEquipSlot(GenshinEquipType type)
    {
        // Terminate if this is the Terrarian character 
        if (CharacterID == GenshinCharacterID.Terrarian)
        {
            return -1;
        }

        return -1;
    }

    /// <summary>
    /// Use this hook to modify character specific drawing.
    /// When this hook runs, the character's textures, color
    /// and shaders are already reset/applied.
    /// </summary>
    public virtual void ModifyDrawInfo(ref PlayerDrawSet drawInfo)
    {
        // Terminate if this is the Terrarian character 
        if (CharacterID == GenshinCharacterID.Terrarian)
        {
            return;
        }
    }

    /// <summary>
    /// Use to make things happen at the start of the player
    /// and character update hook. This runs even before
    /// ResetEffects() hook.
    /// </summary>
    public virtual void PreUpdate()
    {
        // Terminate if this is the Terrarian character 
        if (CharacterID == GenshinCharacterID.Terrarian)
        {
            return;
        }
    }

    /// <summary>
    /// Use this hook to reset effects to the base values so
    /// that they can be re-calculated and re-applied for
    /// this game tick.
    /// </summary>
    public virtual void ResetEffects()
    {
        // Terminate if this is the Terrarian character 
        if (CharacterID == GenshinCharacterID.Terrarian)
        {
            return;
        }
    }

    public virtual bool InUse()
    {
        return false;
    }

    /// <summary>
    /// Called whenever this character is switched onto the field.
    /// Use this to run effects which are dependent on the previous
    /// character. This hook is only called if a valid switch has
    /// been made!
    /// Runs together with the regular OnJoinField() hook.
    /// </summary>
    public virtual void OnJoinFieldSwitch(GenshinCharacterID OldCharacterID)
    {
        Main.NewText(Player.name + " - " + CharacterID.ToString() + " OnJoinFieldSwitch()");
    }

    /// <summary>
    /// Called whenever this character is switched onto the field.
    /// Use this to run effects that needs to happen when this character
    /// takes the field. Also runs when this character is forced
    /// onto the field (for example: when removing the previous selected
    /// character from the team).
    /// </summary>
    public virtual void OnJoinField()
    {
        Main.NewText(Player.name + " - " + CharacterID.ToString() + " OnJoinField()");
    }

    /// <summary>
    /// Called whenever this character leaves the field. Use this to
    /// apply effects that need to apply to the new character taking
    /// the field.
    /// Runs together with the regular OnLeaveField() hook.
    /// </summary>
    public virtual void OnLeaveFieldSwitch(GenshinCharacterID NewCharacterID)
    {
        Main.NewText(Player.name + " - " + CharacterID.ToString() + " OnLeaveFieldSwitch()");
    }

    /// <summary>
    /// Called whenever this character leaves the field. Use this to 
    /// cancel effects of this character when leaving the field.
    /// Also gets called when the character dies.
    /// </summary>
    public virtual void OnLeaveField()
    {
        Main.NewText(Player.name + " - " + CharacterID.ToString() + " OnLeaveField()");
    }

    /// <summary>
    /// Makes something happen when this character is added to the party.
    /// NOTE: in multiplayer this hook is called during SendCharacter packet,
    /// this means equipment, talents and artifact are not received yet!
    /// For now exclusively use for playing party-join animations.
    /// </summary>
    public virtual void OnJoinTeam()
    {
        Main.NewText(Player.name + " - " + CharacterID.ToString() + " OnJoinTeam()");
    }

    /// <summary>
    /// Makes something happen when this character is removed from the party.
    /// </summary>
    public virtual void OnLeaveTeam()
    {
        Main.NewText(Player.name + " - " + CharacterID.ToString() + " OnLeaveTeam()");
    }

    public virtual void AddConstellationPoint()
    {
        // Terminate if this is the Terrarian character 
        if (CharacterID == GenshinCharacterID.Terrarian)
        {
            return;
        }
    }

    public virtual void SendClientChanges(GenshinCharacter clientGenshinPlayer, int slot)
    { 
        
    }

    public virtual void SendCharacter(int toPlayer = -1, int fromPlayer = -1, bool everything = false)
    {
        // Create a packet to send the character's data
        ModPacket packet = GenshinMod.Instance.GetPacket();
        packet.Write((byte)GenshinModMessageType.SendCharacter);
        packet.Write((int)toPlayer);
        packet.Write((int)fromPlayer);
        packet.Write((int)CharacterID);

        // CHARACTER DATA HERE

        packet.Send(toPlayer, fromPlayer);

        // OTHER DATA HERE, like talents weapons artifacts...
    }

    public virtual void ReceiveCharacter(BinaryReader reader)
    { 
    
    }

    #region Other
    public float AscensionSection()
    {
        return Ascension switch
        {
            1 => 38f / 182f,
            2 => 65f / 182f,
            3 => 101f / 182f,
            4 => 128f / 182f,
            5 => 155f / 182f,
            6 => 1f,
            _ => 0f,
        };
    }

    public float LevelMultiplier()
    {
        return Rarity switch
        {
            GenshinRarity.FourStar => 0.082573f * Level + 0.917404f,
            GenshinRarity.FiveStar => 0.086891f * Level + 0.887788f,
            _ => 1f,
        };
    }

    #endregion
}