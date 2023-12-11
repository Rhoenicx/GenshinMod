using GenshinMod.Common.GameObjects;
using GenshinMod.Common.ModObjects.ModSystems;
using Terraria;
using Terraria.ModLoader;

namespace GenshinMod.Content.Characters.Albedo;

public class CharacterAlbedo : GenshinCharacter
{
    public CharacterAlbedo(Player player = null) : base(GenshinCharacterID.Albedo, player)
    {

    }

    public override int GetEquipSlot(GenshinEquipType type)
    {
        switch (type)
        {
            case GenshinEquipType.Body:
                return EquipLoader.GetEquipSlot(GenshinMod.Instance, "Albedo_Body", EquipType.Body);

            case GenshinEquipType.Legs:
                return EquipLoader.GetEquipSlot(GenshinMod.Instance, "Albedo_Legs", EquipType.Legs);

            default:
                return -1;
        }
    }
}
