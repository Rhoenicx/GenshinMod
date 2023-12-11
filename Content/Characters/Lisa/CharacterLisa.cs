using GenshinMod.Common.GameObjects;
using GenshinMod.Common.ModObjects.ModSystems;
using Terraria;
using Terraria.ModLoader;

namespace GenshinMod.Content.Characters.Lisa;

public class CharacterLisa : GenshinCharacter
{
    public CharacterLisa(Player player = null) : base(GenshinCharacterID.Lisa, player)
    {

    }

    public override int GetEquipSlot(GenshinEquipType type)
    {
        switch (type)
        {
            case GenshinEquipType.Head:
                return EquipLoader.GetEquipSlot(GenshinMod.Instance, "Lisa_Head", EquipType.Head);

            case GenshinEquipType.Body:
                return EquipLoader.GetEquipSlot(GenshinMod.Instance, "Lisa_Body", EquipType.Body);

            case GenshinEquipType.Legs:
                return EquipLoader.GetEquipSlot(GenshinMod.Instance, "Lisa_Legs", EquipType.Legs);

            default:
                return -1;
        }
    }
}