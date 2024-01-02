using GenshinMod.Common.GameObjects;
using GenshinMod.Common.ModObjects.ModSystems;
using GenshinMod.Common.ModObjects.Players;
using GenshinMod.Common.ModObjects.Projectiles;
using Terraria;
using Terraria.ModLoader;

namespace GenshinMod.Common.ModObjects.Weapons;
public abstract class GenshinWeapon : ModItem
{
    // Variables
    protected GenshinWeaponType WeaponType = GenshinWeaponType.None;
    protected GenshinRarity Rarity = GenshinRarity.OneStar;
    protected int Level = 1;
    protected int Refinement = 1;

    // Owner of this weapon Item
    public bool BelongsToNPC => NPCOwnerID >= 0;
    protected int NPCOwnerID = -1;

    // Attack Stats
    protected GenshinCharacterID CharacterOwnerID = GenshinCharacterID.Terrarian;
    protected ElementApplication Application = ElementApplication.None;
    protected AttackType HitType = AttackType.None;
    protected AttackWeight HitWeight = AttackWeight.None;

    // Elements
    protected DamageClass BaseDamageType = DamageClass.Default;
    protected ElementApplication BaseElementalApplication = ElementApplication.Weak;

    // Element Infusion
    public bool InfusionActive => CanBeInfused && _infusionTimer > 0;
    protected bool CanBeInfused = true;
    private DamageClass _infusionDamageType = DamageClass.Default;
    private ElementApplication _infusionElementalApplication = ElementApplication.Weak;
    private bool _infusionCanBeOverwritten = true;
    private int _infusionTimer = 0;

    // other
    private int _baseDamage;

    protected override bool CloneNewInstances => true;

    public override void SetDefaults()
    {
        _baseDamage = Item.damage;
        BaseDamageType = Item.DamageType;
    }

    public override void UpdateInventory(Player player)
    {
        Item.DamageType = InfusionActive ? _infusionDamageType : BaseDamageType;
        Application = InfusionActive ? _infusionElementalApplication : BaseElementalApplication;

        if (InfusionActive)
        {
            _infusionTimer--;
        }
    }

    public override bool CanUseItem(Player player)
    {
        CharacterOwnerID = player.GetModPlayer<GenshinPlayer>().CurrentCharacterID;

        if (player.altFunctionUse == 2)
        {
            HitType = AttackType.NormalAttack;
        }
        else
        {
            HitType = AttackType.ChargedAttack;
        }

        return base.CanUseItem(player);
    }

    public override bool? CanAutoReuseItem(Player player)
    {
        // If the player is currently not controlling a valid Character,
        // return the base solution.
        if (!player.GetModPlayer<GenshinPlayer>().CharacterActive)
        {
            return base.CanAutoReuseItem(player);
        }

        // Grab the player's character
        GenshinCharacter character = player.GetModPlayer<GenshinPlayer>().CurrentCharacter;

        // Determine if the weapon can autoswing and/or autorepeat
        return (character.CurrentAttack < character.NormalAttackAmount && character.AutoswingNA)
            || (character.AutoswingNA && character.AutoRepeatNACombo);
    }

    public override void ModifyWeaponDamage(Player player, ref StatModifier damage)
    {
        if (player.TryGetModPlayer(out GenshinPlayer genshinPlayer)
            && genshinPlayer.GenshinModeEnabled
            && genshinPlayer.TryGetTeamCharacter(CharacterOwnerID, out GenshinCharacter genshinCharacter))
        {
            Item.damage = (int)(genshinCharacter.GetBaseDamage(HitType)
                    * genshinCharacter.GetBaseDamageMultiplier())
                    + genshinCharacter.GetAdditiveBaseDamageBonus();
        }
        else
        {
            Item.damage = _baseDamage;
        }
    }

    public bool InfuseWeapon(DamageClass damageType, ElementApplication application = ElementApplication.Weak, int infuseTime = 60, bool canBeOverwritten = true, bool ignoreOverwrite = false)
    {
        // Cannot apply infusion
        if (!CanBeInfused || (InfusionActive && !_infusionCanBeOverwritten && !ignoreOverwrite))
        {
            return false;
        }

        _infusionDamageType = damageType;
        _infusionElementalApplication = application;
        _infusionCanBeOverwritten = canBeOverwritten;
        _infusionTimer = infuseTime;

        return true;
    }

    public void RemoveInfusion()
    {
        _infusionCanBeOverwritten = true;
        _infusionTimer = 0;
    }

    public void GetWeaponStats(out GenshinCharacterID genshinCharacterID, out GenshinWeaponType weaponType, out ElementApplication elementApplication, out AttackType attackType, out AttackWeight attackWeight)
    {
        genshinCharacterID = CharacterOwnerID;
        weaponType = WeaponType;
        elementApplication = Application;
        attackType = HitType;
        attackWeight = HitWeight;
    }
}