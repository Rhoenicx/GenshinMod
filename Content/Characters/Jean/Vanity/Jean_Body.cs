using System.Collections.Generic;
using Microsoft.Xna.Framework;
using GenshinMod.Common.GameObjects;
using GenshinMod.Common.ModObjects.ModSystems;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace GenshinMod.Content.Characters.Jean.Vanity;

[AutoloadEquip(EquipType.Body)]
public class Jean_Body : ModItem
{
    public override void SetStaticDefaults()
    {
        // Register Jean's body equip slot
        GenshinPlayerSystem.RegisterEquipSlot(EquipType.Body, Item.bodySlot, new GenshinEquipSettings()
        {
            characterID = GenshinCharacterID.Jean
        });

        // Body piece settings
        ArmorIDs.Body.Sets.HidesArms[Item.bodySlot] = false;
        ArmorIDs.Body.Sets.HidesHands[Item.bodySlot] = false;
        ArmorIDs.Body.Sets.HidesTopSkin[Item.bodySlot] = false;
        ArmorIDs.Body.Sets.DisableHandOnAndOffAccDraw[Item.bodySlot] = true;
    }

    public override void SetDefaults()
    {
        // Information
        Item.value = Item.buyPrice(0, 0, 5, 0);
        Item.rare = ItemRarityID.White;

        // Hitbox
        Item.width = 30;
        Item.height = 20;

        // Armor-Specific
        Item.vanity = true;
    }
}
