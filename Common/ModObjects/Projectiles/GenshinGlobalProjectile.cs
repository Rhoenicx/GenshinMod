using GenshinMod.Common.ModObjects.ModSystems;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace GenshinMod.Common.ModObjects.Projectiles;

public class GenshinGlobalProjectile : GlobalProjectile
{
    public override bool InstancePerEntity => true;

    public override void SetDefaults(Projectile entity)
    {
        if (entity.type == ProjectileID.FlamingArrow)
        {
            entity.DamageType = ModContent.GetInstance<GenshinHydro>();
        }
    }
}

