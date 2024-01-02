using GenshinMod.Common.ModObjects.ModSystems;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace GenshinMod.Common.ModObjects.Shields
{
    public abstract class GenshinShieldNPC : ILoadable
    {  
        public bool Active => Life > 0;     // Determines if this shield is still Active
        public int Life = 100;              // The amount of life of the shield before it breaks
        public NPC Owner;                   // Owner

        // The element that this shield protects against.
        public abstract DamageClass DamageType();

        // Loads assets of this shield (textures)
        public virtual void Load(Mod mod)
        {
            
        }

        // Unloads assets of this shield (textures)
        public virtual void Unload()
        {
            
        }

        public virtual void Draw(SpriteBatch spriteBatch)
        { 
            
        }

        public GenshinShieldNPC NewShield(int life = 100)
        {
            this.Life = life;
            return this;
        }

        public virtual void ResetEffects()
        { 
        
        }

        public virtual void StrikeShield(DamageClass damageType, ElementApplication application = ElementApplication.Weak)
        { 
            
        }

        public virtual void Kill()
        { 
        
        }
    }
}
