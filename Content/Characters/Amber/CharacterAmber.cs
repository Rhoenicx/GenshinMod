using GenshinMod.Common.GameObjects;
using GenshinMod.Common.ModObjects.ModSystems;
using Terraria;
using Terraria.ModLoader;

namespace GenshinMod.Content.Characters.Amber;

public class CharacterAmber : GenshinCharacter
{
    public CharacterAmber(Player player = null) : base(GenshinCharacterID.Amber, player)
    {

    }

    public override int GetEquipSlot(GenshinEquipType type)
    {
        switch (type)   
        {
            case GenshinEquipType.Head:
                return EquipLoader.GetEquipSlot(GenshinMod.Instance, "Amber_Head", EquipType.Head);

            case GenshinEquipType.Body:
                return EquipLoader.GetEquipSlot(GenshinMod.Instance, "Amber_Body", EquipType.Body);

            case GenshinEquipType.Legs:
                return EquipLoader.GetEquipSlot(GenshinMod.Instance, "Amber_Legs", EquipType.Legs);

            default:
                return -1;
        }
    }
}
