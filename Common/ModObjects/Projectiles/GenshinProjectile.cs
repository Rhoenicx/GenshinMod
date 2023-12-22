using GenshinMod.Common.ModObjects.ModSystems;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.ModLoader;

namespace GenshinMod.Common.ModObjects.Projectiles;

public abstract class GenshinProjectile : ModProjectile
{
    // The Character owning this projectile, when this projectile hits a target
    // it should grab the stats from the Character that produced this projectile.
    public GenshinCharacterID CharacterOwnerID = GenshinCharacterID.Terrarian;

    public float ElementApplication = GenshinElementApplication.Weak;
}

