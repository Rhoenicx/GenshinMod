using GenshinMod.Common.GameObjects;
using GenshinMod.Common.ModObjects.ModSystems;
using Terraria;
using Terraria.ModLoader;

namespace GenshinMod.Content.Characters.Kaeya;

public class CharacterKaeya : GenshinCharacter
{
    public CharacterKaeya(Player player = null) : base(GenshinCharacterID.Kaeya, player)
    {

    }

    public override int GetEquipSlot(GenshinEquipType type)
    {
        switch (type)
        {
            case GenshinEquipType.Body:
                return EquipLoader.GetEquipSlot(GenshinMod.Instance, "Kaeya_Body", EquipType.Body);

            case GenshinEquipType.Legs:
                return EquipLoader.GetEquipSlot(GenshinMod.Instance, "Kaeya_Legs", EquipType.Legs);

            default:
                return -1;
        }
    }
}