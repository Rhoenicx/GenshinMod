
using System;
using System.ComponentModel;
using Terraria.ModLoader.Config;

namespace GenshinMod.Common.Configs;

public class GenshinConfigServer : ModConfig
{
    public override ConfigScope Mode => ConfigScope.ServerSide;

    public override bool Autoload(ref string name) => true;

    [Header("Cheats")]

    [DefaultValue(4)]
    [Slider]
    [Range(1, 16)]
    public int MaxTeamSize;

    [DefaultValue(1)]
    [Slider]
    [Range(1, 16)]
    public int MinCharactersPerPlayer;

    public override void OnLoaded()
    {
        GenshinMod.GenshinConfigServer = this;

        // Make sure the configured values
        // of some settings are within the bounds.
        MaxTeamSize = Math.Max(MaxTeamSize, 1);
        MinCharactersPerPlayer = Math.Max(MinCharactersPerPlayer, 1);
    }

    public override void OnChanged()
    {
        // Limit the minimum
        if (MinCharactersPerPlayer > MaxTeamSize)
        {
            MinCharactersPerPlayer = MaxTeamSize;
        }
    }
}