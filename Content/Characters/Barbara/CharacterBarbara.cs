using GenshinMod.Common.GameObjects;
using GenshinMod.Common.ModObjects.ModSystems;
using Terraria;
using Terraria.ModLoader;

namespace GenshinMod.Content.Characters.Barbara;

public class CharacterBarbara : GenshinCharacter
{
    public CharacterBarbara(Player player = null) : base(GenshinCharacterID.Barbara, player)
    {

    }

    public override int GetEquipSlot(GenshinEquipType type)
    {
        switch (type)
        {
            case GenshinEquipType.Head:
                return EquipLoader.GetEquipSlot(GenshinMod.Instance, "Barbara_Head", EquipType.Head);

            case GenshinEquipType.Body:
                return EquipLoader.GetEquipSlot(GenshinMod.Instance, "Barbara_Body", EquipType.Body);

            case GenshinEquipType.Legs:
                return EquipLoader.GetEquipSlot(GenshinMod.Instance, "Barbara_Legs", EquipType.Legs);

            default:
                return -1;
        }
    }
}