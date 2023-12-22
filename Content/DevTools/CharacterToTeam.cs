using GenshinMod.Common.ModObjects.ModSystems;
using GenshinMod.Common.ModObjects.Players;
using Microsoft.Xna.Framework;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using static Terraria.ModLoader.ModContent;

namespace GenshinMod.Content.DevTools
{
    public class CharacterToTeam : ModItem
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

                gPlayer.PendingTeamComposition = gPlayer.CurrentTeamComposition.ToList();
                foreach (GenshinCharacterID id in gPlayer.ObtainedCharacters.Keys)
                {
                    if (!gPlayer.PendingTeamComposition.Contains(id))
                    { 
                        gPlayer.PendingTeamComposition.Add(id);
                    }
                }

                gPlayer.ApplyPendingTeam = true;
            }
            return base.CanUseItem(player);
        }
    }
}
