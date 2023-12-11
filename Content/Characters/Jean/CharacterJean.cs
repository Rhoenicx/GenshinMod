using GenshinMod.Common.GameObjects;
using GenshinMod.Common.ModObjects.ModSystems;
using Terraria;
using Terraria.ModLoader;

namespace GenshinMod.Content.Characters.Jean;

public class CharacterJean : GenshinCharacter
{
    public CharacterJean(Player player = null) : base(GenshinCharacterID.Jean, player)
    {

    }

    public override int GetEquipSlot(GenshinEquipType type)
    {
        switch (type)
        {
            case GenshinEquipType.Body:
                return EquipLoader.GetEquipSlot(GenshinMod.Instance, "Jean_Body", EquipType.Body);

            case GenshinEquipType.Legs:
                return EquipLoader.GetEquipSlot(GenshinMod.Instance, "Jean_Legs", EquipType.Legs);

            default:
                return -1;
        }
    }
}