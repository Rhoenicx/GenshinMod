using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace GenshinMod.Content.Characters.Amber.Hair
{
    public class Amber_Hair : ModHair
    {
        public override Gender RandomizedCharacterCreationGender => Gender.Female;

        public override bool AvailableDuringCharacterCreation => true;

        public override void SetStaticDefaults()
        {
            HairID.Sets.DrawBackHair[Type] = true;
        }
    }
}
