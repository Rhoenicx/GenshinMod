using GenshinMod.Common.ModObjects.ModSystems;
using GenshinMod.Common.ModObjects.Weapons;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace GenshinMod.Content.DevTools;

public class ElementalSword : GenshinWeapon
{
    private int Element = 0;

    public override void SetDefaults()
    {
        Item.width = 40;
        Item.height = 40;

        Item.useStyle = ItemUseStyleID.Swing;
        Item.useTime = 20;
        Item.useAnimation = 20;
        Item.autoReuse = true;

        Item.DamageType = ModContent.GetInstance<GenshinPhysical>();
        Item.damage = 100;
        Item.knockBack = 1f;
        Item.crit = 4;

        Item.value = Item.buyPrice(gold: 1);
        Item.rare = ItemRarityID.Master;
        Item.UseSound = SoundID.Item1;

        base.SetDefaults();
    }

    public override bool AltFunctionUse(Player player)
    {
        return true;
    }

    public override bool CanUseItem(Player player)
    {
        if (player.altFunctionUse == 2)
        {
            Element++;

            if (Element >= 7)
            {
                Element = 0;
            }

            InfuseWeapon(GetDamageClass(), GenshinElementApplication.Weak, 600);

            return false;
        }

        return base.CanUseItem(player);
    }

    private DamageClass GetDamageClass()
    {
        return Element switch
        {
            0 => ModContent.GetInstance<GenshinPyro>(),
            1 => ModContent.GetInstance<GenshinHydro>(),
            2 => ModContent.GetInstance<GenshinCryo>(),
            3 => ModContent.GetInstance<GenshinDendro>(),
            4 => ModContent.GetInstance<GenshinElectro>(),
            5 => ModContent.GetInstance<GenshinAnemo>(),
            6 => ModContent.GetInstance<GenshinGeo>(),
            _ => BaseDamageType
        };
    }
}

