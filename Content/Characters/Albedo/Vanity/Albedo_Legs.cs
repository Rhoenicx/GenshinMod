using System.Collections.Generic;
using Microsoft.Xna.Framework;
using GenshinMod.Common.GameObjects;
using GenshinMod.Common.ModObjects.ModSystems;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace GenshinMod.Content.Characters.Albedo.Vanity;

[AutoloadEquip(EquipType.Legs)]
public class Albedo_Legs : ModItem
{
    public override void SetStaticDefaults()
    {
        // Register Albedo's leg equip slot
        GenshinPlayerSystem.RegisterEquipSlot(EquipType.Legs, Item.legSlot, new GenshinEquipSettings() 
        {
            characterID = GenshinCharacterID.Albedo
        });

        // Legs piece settings
        ArmorIDs.Legs.Sets.HidesBottomSkin[Item.legSlot] = false;
    }

    public override void SetDefaults()
    {
        // Information
        Item.value = Item.buyPrice(0, 0, 5, 0);
        Item.rare = ItemRarityID.White;

        // Hitbox
        Item.width = 18;
        Item.height = 16;

        // Armor-Specific
        Item.vanity = true;
    }
}
