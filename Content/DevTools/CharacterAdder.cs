using GenshinMod.Common.ModObjects;
using GenshinMod.Common.ModObjects.ModSystems;
using Microsoft.Xna.Framework;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using static Terraria.ModLoader.ModContent;

namespace GenshinMod.Content.DevTools
{
    public class CharacterAdder : ModItem
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

                // Add the Terrarian to the player's team
                if (!gPlayer.ObtainedCharacters.ContainsKey(GenshinCharacterID.Terrarian))
                {
                    gPlayer.AddCharacter(GenshinCharacterID.Terrarian);
                }

                // Add Lumine to the player's team
                else if (!gPlayer.ObtainedCharacters.ContainsKey(GenshinCharacterID.Lumine))
                {
                    gPlayer.AddCharacter(GenshinCharacterID.Lumine);
                }

                // Add Amber to the player's team
                else if (!gPlayer.ObtainedCharacters.ContainsKey(GenshinCharacterID.Amber))
                {
                    gPlayer.AddCharacter(GenshinCharacterID.Amber);
                }

                // Add Kaeya to the player's team
                else if (!gPlayer.ObtainedCharacters.ContainsKey(GenshinCharacterID.Kaeya))
                {
                    gPlayer.AddCharacter(GenshinCharacterID.Kaeya);
                }

                // Add Barbara to the player's team
                else if (!gPlayer.ObtainedCharacters.ContainsKey(GenshinCharacterID.Barbara))
                {
                    gPlayer.AddCharacter(GenshinCharacterID.Barbara);
                }

                // Add Klee to the player's team
                else if (!gPlayer.ObtainedCharacters.ContainsKey(GenshinCharacterID.Klee))
                {
                    gPlayer.AddCharacter(GenshinCharacterID.Klee);
                }

                // Add Lisa to the player's team
                else if (!gPlayer.ObtainedCharacters.ContainsKey(GenshinCharacterID.Lisa))
                {
                    gPlayer.AddCharacter(GenshinCharacterID.Lisa);
                }

                // Add Noelle to the player's team
                else if (!gPlayer.ObtainedCharacters.ContainsKey(GenshinCharacterID.Noelle))
                {
                    gPlayer.AddCharacter(GenshinCharacterID.Noelle);
                }

                // Add Jean to the player's team
                else if (!gPlayer.ObtainedCharacters.ContainsKey(GenshinCharacterID.Jean))
                {
                    gPlayer.AddCharacter(GenshinCharacterID.Jean);
                }

                // Add Albedo to the player's team
                else if (!gPlayer.ObtainedCharacters.ContainsKey(GenshinCharacterID.Albedo))
                {
                    gPlayer.AddCharacter(GenshinCharacterID.Albedo);
                }
            }
            return base.CanUseItem(player);
        }
    }
}
