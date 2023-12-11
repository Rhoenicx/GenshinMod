using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace GenshinMod.Content.Characters.Jean.Hair
{
    public class Jean_Hair : ModHair
    {
        public static int ID = -1;

        public override Gender RandomizedCharacterCreationGender => Gender.Female;

        public override bool AvailableDuringCharacterCreation => true;

        public override void SetStaticDefaults()
        {
            HairID.Sets.DrawBackHair[Type] = true;

            ID = Type;
        }
    }
}
