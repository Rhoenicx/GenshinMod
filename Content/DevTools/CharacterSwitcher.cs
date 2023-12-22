using GenshinMod.Common.ModObjects.Players;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using static Terraria.ModLoader.ModContent;

namespace GenshinMod.Content.DevTools
{
    public class CharacterSwitcher : ModItem
    {
        public override void SetDefaults() 
        {
            // Information
            Item.value = Item.buyPrice(0, 0, 0, 1);
            Item.rare = ItemRarityID.White;

            // Hitbox
            Item.width = 32;
            Item.height = 32;

            // Usage and animation (optionally noUseGraphic, channel, noMelee, autoReuse)
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.useTime = 20;
            Item.useAnimation = 20;
            Item.autoReuse = false;
            Item.noMelee = true;

            // Sound
            Item.UseSound = SoundID.MaxMana;
        }

        public override bool CanUseItem(Player player)
        {
            if (Main.myPlayer == player.whoAmI)
            {
                GenshinPlayer gPlayer = player.GetModPlayer<GenshinPlayer>();

                int slot = gPlayer.CurrentCharacterSlot;
                slot++;

                if (slot >= gPlayer.CurrentTeamComposition.Count
                    || slot >= gPlayer.MaxControllableCharacters)
                {
                    slot = 0;
                }

                gPlayer.SwitchCharacter(gPlayer.CurrentTeamComposition[slot]);

            }
            return base.CanUseItem(player);
        }
    }
}
