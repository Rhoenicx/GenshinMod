using GenshinMod.Common.GameObjects;
using GenshinMod.Common.ModObjects.ModSystems;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace GenshinMod.Content.Characters.Amber.Vanity;

[AutoloadEquip(EquipType.Head)]
public class Amber_Head : ModItem
{
    public override void SetStaticDefaults()
    {
        // Register Amber's Head equip slot
        GenshinPlayerSystem.RegisterEquipSlot(EquipType.Head, Item.bodySlot, new GenshinEquipSettings()
        {
            characterID = GenshinCharacterID.Amber,
            hatOffset = new Dictionary<int, Vector2> { { -1, new Vector2(0f, -2f) } }
        });

        ArmorIDs.Head.Sets.DrawFullHair[Item.headSlot] = true;
        ArmorIDs.Head.Sets.DrawHatHair[Item.headSlot] = false;
    }

    public override void SetDefaults()
    {
        // Information
        Item.value = Item.buyPrice(0, 0, 5, 0);
        Item.rare = ItemRarityID.White;

        // Hitbox
        Item.width = 16;
        Item.height = 16;

        // Armor-Specific
        Item.vanity = true;
    }
}
