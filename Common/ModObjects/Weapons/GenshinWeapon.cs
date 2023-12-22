using GenshinMod.Common.GameObjects;
using GenshinMod.Common.ModObjects.ModSystems;
using GenshinMod.Common.ModObjects.Players;
using Terraria;
using Terraria.ModLoader;

namespace GenshinMod.Common.ModObjects.Weapons;
public abstract class GenshinWeapon : ModItem
{
    // Variables
    public GenshinWeaponType WeaponType = GenshinWeaponType.None;
    public GenshinRarity Rarity = GenshinRarity.OneStar;
    public int Level = 1;
    public int Refinement = 1;

    // Elements
    public float ElementalApplication => InfusionActive ? _infusionElementalApplication : BaseElementalApplication;
    protected DamageClass BaseDamageType = DamageClass.Default;
    protected float BaseElementalApplication = GenshinElementApplication.Weak;

    // Element Infusion
    public bool InfusionActive => _canBeInfused && _infusionTimer > 0;
    private bool _canBeInfused = true;
    private DamageClass _infusionDamageType = DamageClass.Default;
    private float _infusionElementalApplication = GenshinElementApplication.Weak;
    private bool _infusionCanBeOverwritten = true;
    private int _infusionTimer = 0;

    protected override bool CloneNewInstances => true;

    public override void SetDefaults()
    {
        BaseDamageType = Item.DamageType;
    }

    public override void UpdateInventory(Player player)
    {
        Item.DamageType = InfusionActive ? _infusionDamageType : BaseDamageType;

        if (InfusionActive)
        {
            _infusionTimer--;
        }
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

    public override void ModifyHitNPC(Player player, NPC target, ref NPC.HitModifiers modifiers)
    {
        modifiers.DamageVariationScale *= 0f;
        modifiers.DefenseEffectiveness *= 0f;
    }

    public bool InfuseWeapon(DamageClass damageType, float application = GenshinElementApplication.Weak, int infuseTime = 60, bool canBeOverwritten = true, bool ignoreOverwrite = false)
    {
        // Cannot apply infusion
        if (!_canBeInfused || (InfusionActive && !_infusionCanBeOverwritten && !ignoreOverwrite))
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
}