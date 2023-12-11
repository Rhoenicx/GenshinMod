using GenshinMod.Common.ModObjects;
using System;
using System.ComponentModel;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader.Config;

namespace GenshinMod.Common.Configs;
public class GenshinConfigClient : ModConfig
{
    public override ConfigScope Mode => ConfigScope.ClientSide;

    public override bool Autoload(ref string name) => true;

    [Header("Misc")]

    [DefaultValue(false)]
    [BackgroundColor(50, 200, 100)]
    public bool EnableBurstQuotes;

    [Header("Cheats")]

    [DefaultValue(4)]
    [Slider]
    [Range(1,16)]
    public int MaxTeamSize;

    [DefaultValue(false)]
    [BackgroundColor(200, 40, 40)]
    public bool GenshinModeEnabled;

    [DefaultValue(false)]
    public bool EnableTeamDebugger;

    public override void OnLoaded()
    {
        GenshinMod.GenshinConfigClient = this;

        // Make sure the configured values
        // of some settings are within the bounds.
        MaxTeamSize = Math.Max(MaxTeamSize, 1);
    }

    public override void OnChanged()
    {
        if (!Main.gameMenu && Main.LocalPlayer != null)
        {
            Main.LocalPlayer.GetModPlayer<GenshinPlayer>().GenshinModeEnabled = GenshinModeEnabled;
            GenshinPlayer.TeamDebugger = EnableTeamDebugger;
        }
    }
}