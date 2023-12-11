using GenshinMod.Common.GameObjects;
using GenshinMod.Common.ModObjects.ModSystems;
using Terraria;
using Terraria.ModLoader;

namespace GenshinMod.Content.Characters.Noelle;

public class CharacterNoelle : GenshinCharacter
{
    public CharacterNoelle(Player player = null) : base(GenshinCharacterID.Noelle, player)
    {

    }

    public override int GetEquipSlot(GenshinEquipType type)
    {
        switch (type)
        {
            case GenshinEquipType.Head:
                return EquipLoader.GetEquipSlot(GenshinMod.Instance, "Noelle_Head", EquipType.Head);

            case GenshinEquipType.Body:
                return EquipLoader.GetEquipSlot(GenshinMod.Instance, "Noelle_Body", EquipType.Body);

            case GenshinEquipType.Legs:
                return EquipLoader.GetEquipSlot(GenshinMod.Instance, "Noelle_Legs", EquipType.Legs);

            default:
                return -1;
        }
    }
}