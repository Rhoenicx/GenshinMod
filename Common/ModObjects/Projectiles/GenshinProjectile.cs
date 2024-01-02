using GenshinMod.Common.GameObjects;
using GenshinMod.Common.ModObjects.ModSystems;
using GenshinMod.Common.ModObjects.NPCs;
using GenshinMod.Common.ModObjects.Players;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.ModLoader;

namespace GenshinMod.Common.ModObjects.Projectiles;

public abstract class GenshinProjectile : ModProjectile
{
    // Owner of this projectile, can be NPC or Player
    public bool BelongsToNPC => NPCOwnerID >= 0;
    public int NPCOwnerID = -1;

    // Attack stats
    public GenshinCharacterID CharacterOwnerID = GenshinCharacterID.None;
    public GenshinWeaponType WeaponType = GenshinWeaponType.None;
    public ElementApplication Application = ElementApplication.None;
    public AttackType HitType = AttackType.None;
    public AttackWeight HitWeight = AttackWeight.None;

    #region network
    public sealed override void SendExtraAI(BinaryWriter writer)
    {
        writer.Write((int)NPCOwnerID);
        writer.Write((int)Projectile.DamageType.Type);
        writer.Write((int)CharacterOwnerID);
        writer.Write((byte)WeaponType);
        writer.Write((byte)Application);
        writer.Write((byte)HitType);
        writer.Write((byte)HitWeight);
    }

    public virtual void SendExtraProjectileAI(BinaryWriter writer) { }

    public sealed override void ReceiveExtraAI(BinaryReader reader)
    {
        // Read the NPCOwnerID from the packet
        NPCOwnerID = reader.ReadInt32();

        // Read the damageType from the packet
        int damageType = reader.ReadInt32();
        Projectile.DamageType = DamageClassLoader.GetDamageClass(damageType) 
            ?? DamageClass.Generic;

        // Read the CharacterID from the packet
        int characterID = reader.ReadInt32();
        CharacterOwnerID = Enum.IsDefined(typeof(GenshinCharacterID), characterID)
            ? (GenshinCharacterID)characterID
            : GenshinCharacterID.Terrarian;

        // Read the WeaponType from the packet
        byte weaponType = reader.ReadByte();
        WeaponType = Enum.IsDefined(typeof(GenshinWeaponType), weaponType)
            ? (GenshinWeaponType)weaponType
            : GenshinWeaponType.None;

        // Read the ElementApplication from the packet
        byte application = reader.ReadByte();
        Application = Enum.IsDefined(typeof(ElementApplication), application)
            ? (ElementApplication)application
            : ElementApplication.None;

        // Read the AttackType from the packet
        byte attackType = reader.ReadByte();
        HitType = Enum.IsDefined(typeof(AttackType), attackType)
            ? (AttackType)attackType
            : AttackType.None;

        // Read the AttackWeight from the packet
        byte attackWeight = reader.ReadByte();
        HitWeight = Enum.IsDefined(typeof(AttackWeight), attackWeight)
            ? (AttackWeight)attackWeight
            : AttackWeight.None;
    }

    public virtual void ReceiveExtraProjectileAI(BinaryReader reader) { }

    #endregion
    public void GetProjectileStats(out GenshinCharacterID genshinCharacterID, out GenshinWeaponType weaponType, out ElementApplication elementApplication, out AttackType attackType, out AttackWeight attackWeight)
    {
        genshinCharacterID = CharacterOwnerID;
        weaponType = WeaponType;
        elementApplication = Application;
        attackType = HitType;
        attackWeight = HitWeight;
    }
}

