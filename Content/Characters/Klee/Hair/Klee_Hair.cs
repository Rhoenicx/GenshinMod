using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace GenshinMod.Content.Characters.Klee.Hair
{
    public class Klee_Hair : ModHair
    {
        public static int hairType;

        public override Gender RandomizedCharacterCreationGender => Gender.Female;

        public override bool AvailableDuringCharacterCreation => true;

        public override void SetStaticDefaults()
        {
            HairID.Sets.DrawBackHair[Type] = true;

            // Save the ID of this modded hairstyle
            hairType = Type;
        }
    }
}
