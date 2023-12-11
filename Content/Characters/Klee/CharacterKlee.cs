using GenshinMod.Common.GameObjects;
using GenshinMod.Common.ModObjects.ModSystems;
using Terraria;
using Terraria.ModLoader;

namespace GenshinMod.Content.Characters.Klee;

public class CharacterKlee : GenshinCharacter
{
    public CharacterKlee(Player player = null) : base(GenshinCharacterID.Klee, player)
    {
        
    }

    public override int GetEquipSlot(GenshinEquipType type)
    {
        switch (type)
        {
            case GenshinEquipType.Head:
                return EquipLoader.GetEquipSlot(GenshinMod.Instance, "Klee_Head", EquipType.Head);

            case GenshinEquipType.Body:
                return EquipLoader.GetEquipSlot(GenshinMod.Instance, "Klee_Body", EquipType.Body);

            case GenshinEquipType.Legs:
                return EquipLoader.GetEquipSlot(GenshinMod.Instance, "Klee_Legs", EquipType.Legs);

            case GenshinEquipType.Backpack:
                return EquipLoader.GetEquipSlot(GenshinMod.Instance, "Klee_Backpack", EquipType.Back);

            default:
                return -1;
        }
    }
}