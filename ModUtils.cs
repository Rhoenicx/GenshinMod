using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GenshinMod;

public static class ModUtils
{
    public static void Swap<T>(this List<T> list, int index1, int index2)
    {
        T temp = list[index1];
        list[index1] = list[index2];
        list[index2] = temp;
    }
}

public enum GenshinWeaponType : Byte
{
    None,
    Sword,
    Claymore,
    Polearm,
    Bow,
    Catalyst
}

public enum GenshinRarity : byte
{
    OneStar,
    TwoStar,
    ThreeStar,
    FourStar,
    FiveStar
}

