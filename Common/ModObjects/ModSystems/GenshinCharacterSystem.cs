using GenshinMod.Common.GameObjects;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;

namespace GenshinMod.Common.ModObjects.ModSystems;

public class GenshinCharacterSystem : ModSystem
{
    public static Dictionary<GenshinCharacterID, GenshinCharacter> CharacterArchive = new();

    public override void Load()
    {
        LoadGenshinCharacterArchive();
        base.Load();
    }

    public override void Unload()
    {
        //UnloadGenshinCharacterArchive();
        base.Unload();
    }

    private static void LoadGenshinCharacterArchive()
    {
        // Initialize dictionary
        CharacterArchive ??= new();

        // Create character objects and it them to the Character Archive
        foreach (GenshinCharacterID id in Enum.GetValues(typeof(GenshinCharacterID)))
        {
            AddCharacterToArchive(id, GetGenshinCharacter(id, null));
        }

        // Run Load() hook on all the characters
        foreach (GenshinCharacter chr in CharacterArchive.Values)
        {
            chr.Load();
        }
    }

    private static void UnloadGenshinCharacterArchive()
    {
        // Run Unload() hook on all the characters
        foreach (GenshinCharacter chr in CharacterArchive.Values)
        {
            chr.Unload();
        }
    }

    public static GenshinCharacter GetGenshinCharacter(GenshinCharacterID id, Player plr = null)
    {
        switch (id)
        {
            case GenshinCharacterID.Aether:
                return new Content.Characters.Aether.CharacterAether(plr);

            case GenshinCharacterID.Lumine:
                return new Content.Characters.Lumine.CharacterLumine(plr);

            case GenshinCharacterID.Albedo:
                return new Content.Characters.Albedo.CharacterAlbedo(plr);

            case GenshinCharacterID.Amber:
                return new Content.Characters.Amber.CharacterAmber(plr);

            case GenshinCharacterID.Barbara:
                return new Content.Characters.Barbara.CharacterBarbara(plr);

            case GenshinCharacterID.Jean:
                return new Content.Characters.Jean.CharacterJean(plr);

            case GenshinCharacterID.Kaeya:
                return new Content.Characters.Kaeya.CharacterKaeya(plr);

            case GenshinCharacterID.Klee:
                return new Content.Characters.Klee.CharacterKlee(plr);

            case GenshinCharacterID.Lisa:
                return new Content.Characters.Lisa.CharacterLisa(plr);
            
            case GenshinCharacterID.Noelle:
                return new Content.Characters.Noelle.CharacterNoelle(plr);
            
            default:
                // This is a empty / dummy character for multiplayer fail-safe
                return new Content.Characters.Terrarian.CharacterTerrarian(plr);
        }
    }

    private static void AddCharacterToArchive(GenshinCharacterID id, GenshinCharacter chr)
    {
        if (CharacterArchive.ContainsKey(id))
        {
            CharacterArchive[id] = chr;
            return;
        }

        CharacterArchive.Add(id, chr);
    }
}

public enum GenshinCharacterID
{ 
    Terrarian,
    Aether,
    Lumine,
    Amber,
    Barbara,
    Jean,
    Kaeya,
    Klee,
    Lisa,
    Noelle,
    Albedo
}

