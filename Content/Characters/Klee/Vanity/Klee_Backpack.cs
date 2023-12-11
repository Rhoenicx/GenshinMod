using System.Collections.Generic;
using Microsoft.Xna.Framework;
using GenshinMod.Common.GameObjects;
using GenshinMod.Common.ModObjects.ModSystems;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace GenshinMod.Content.Characters.Klee.Vanity;

[AutoloadEquip(EquipType.Back)]
public class Klee_Backpack : ModItem
{
    public override void SetStaticDefaults()
    {
        ArmorIDs.Back.Sets.DrawInBackpackLayer[Item.backSlot] = true;
    }

    public override void SetDefaults()
    {
        // Information
        Item.value = Item.buyPrice(0, 0, 5, 0);
        Item.rare = ItemRarityID.White;

        // Hitbox
        Item.width = 22;
        Item.height = 22;

        // Armor-Specific
        Item.vanity = true;
        Item.accessory = true;
    }
}
