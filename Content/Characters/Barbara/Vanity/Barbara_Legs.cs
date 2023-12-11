using System.Collections.Generic;
using Microsoft.Xna.Framework;
using GenshinMod.Common.GameObjects;
using GenshinMod.Common.ModObjects.ModSystems;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace GenshinMod.Content.Characters.Barbara.Vanity;

[AutoloadEquip(EquipType.Legs)]
public class Barbara_Legs : ModItem
{
    public override void SetStaticDefaults()
    {
        // Register Barbara's leg equip slot
        GenshinPlayerSystem.RegisterEquipSlot(EquipType.Legs, Item.legSlot, new GenshinEquipSettings() 
        {
            characterID = GenshinCharacterID.Barbara,
            legsOffset = new Dictionary<int, Vector2> 
            { 
                { -1, new Vector2(0f, 0f) },
                { 0, new Vector2(-2f, 0f) },
                { 1, new Vector2(-2f, 0f) },
                { 2, new Vector2(-2f, 0f) },
                { 3, new Vector2(-2f, 0f) },
                { 4, new Vector2(-2f, 0f) }
            }
        });

        // Legs piece settings
        ArmorIDs.Legs.Sets.HidesBottomSkin[Item.legSlot] = false;
        ArmorIDs.Legs.Sets.OverridesLegs[Item.legSlot] = false;
    }

    public override void SetDefaults()
    {
        // Information
        Item.value = Item.buyPrice(0, 0, 5, 0);
        Item.rare = ItemRarityID.White;

        // Hitbox
        Item.width = 26;
        Item.height = 18;

        // Armor-Specific
        Item.vanity = true;
    }
}
