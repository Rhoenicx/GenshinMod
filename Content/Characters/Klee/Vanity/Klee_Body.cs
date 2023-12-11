using System.Collections.Generic;
using Microsoft.Xna.Framework;
using GenshinMod.Common.GameObjects;
using GenshinMod.Common.ModObjects.ModSystems;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace GenshinMod.Content.Characters.Klee.Vanity;

[AutoloadEquip(EquipType.Body)]
public class Klee_Body : ModItem
{
    public override void SetStaticDefaults()
    {
        // Register Klee's body equip slot
        GenshinPlayerSystem.RegisterEquipSlot(EquipType.Body, Item.bodySlot, new GenshinEquipSettings()
        {
            characterID = GenshinCharacterID.Klee,
            bodyOffset = new Dictionary<int, Vector2> { { -1, new Vector2(0f, 4f) } },
            wingsOffset = new Dictionary<int, Vector2> { { -1, new Vector2(0f, 4f) } },
            backpackOffset = new Dictionary<int, Vector2> { { -1, new Vector2(4f, 4f) } }
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
        Item.height = 16;

        // Armor-Specific
        Item.vanity = true;
    }
}
