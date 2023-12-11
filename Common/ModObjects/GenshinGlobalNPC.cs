using Terraria;
using Terraria.ModLoader;

namespace GenshinMod.Common.ModObjects
{
    public class GenshinGlobalNPC : GlobalNPC
    {
        public override void ModifyHitByItem(NPC npc, Player player, Item item, ref NPC.HitModifiers modifiers)
        {
            modifiers.HideCombatText();
        }

        public override void ModifyHitByProjectile(NPC npc, Projectile projectile, ref NPC.HitModifiers modifiers)
        {
            modifiers.HideCombatText();
        }

        public override void ModifyHitNPC(NPC npc, NPC target, ref NPC.HitModifiers modifiers)
        {
            modifiers.HideCombatText();
        }

        public override void HitEffect(NPC npc, NPC.HitInfo hit)
        {
            hit.HideCombatText = true;
        }
    }
}
