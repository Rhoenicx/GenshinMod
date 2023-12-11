using GenshinMod.Common.GameObjects;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using System.Reflection;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.Graphics.Renderers;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;
using static Terraria.ModLoader.ModContent;

namespace GenshinMod.Common.ModObjects.ModSystems;

public class GenshinPlayerSystem : ModSystem
{
    // Instance
    internal static GenshinPlayerSystem Instance;

    // EquipTexture settings
    private static Dictionary<int, GenshinEquipSettings> HeadSlots = new();
    private static Dictionary<int, GenshinEquipSettings> BodySlots = new();
    private static Dictionary<int, GenshinEquipSettings> LegsSlots = new();

    // Textures
    private static Dictionary<GenshinCharacterID, Dictionary<GenshinTextureType, Asset<Texture2D>>> CharacterAndPlayerTextures = new();

    // Empty texture fail safe
    private static Asset<Texture2D> _emptyTexture;

    public GenshinPlayerSystem()
    {
        Instance = this;
    }

    #region Load
    public override void Load()
    {
        // EquipTexture loader
        HeadSlots = new();
        BodySlots = new();
        LegsSlots = new();

        // Textures
        CharacterAndPlayerTextures = new();

        // IL patches for player drawing - texture switching
        IL_PlayerDrawLayers.DrawPlayer_01_BackHair += IL_PlayerTextures;
        IL_PlayerDrawLayers.DrawPlayer_12_Skin += IL_PlayerTextures;
        IL_PlayerDrawLayers.DrawPlayer_12_Skin_Composite += IL_PlayerTextures;
        IL_PlayerDrawLayers.DrawPlayer_12_SkinComposite_BackArmShirt += IL_PlayerTextures;
        IL_PlayerDrawLayers.DrawPlayer_17_Torso += IL_PlayerTextures;
        IL_PlayerDrawLayers.DrawPlayer_21_Head += IL_PlayerTextures;
        IL_PlayerDrawLayers.DrawPlayer_21_Head_TheFace += IL_PlayerTextures;
        IL_PlayerDrawLayers.DrawPlayer_28_ArmOverItem += IL_PlayerTextures;
        IL_PlayerDrawLayers.DrawPlayer_28_ArmOverItemComposite += IL_PlayerTextures;

        // IL patches for player drawing - color and shader switching
        IL_HairShaderDataSet.GetColor += IL_PlayerHairColor;
        IL_PlayerDrawSet.CopyBasicPlayerFields += IL_PlayerShaders;
        IL_PlayerDrawSet.BoringSetup_2 += IL_PlayerColors;
        IL_PlayerDrawSet.HeadOnlySetup += IL_PlayerColors;

        // IL patches for player drawing - equipment 
        IL_Main.DoUpdate_WhilePaused += IL_PlayerArmor;
        IL_Player.Update += IL_PlayerArmor;
        IL_Player.PlayerFrame += IL_PlayerArmor;
        IL_Player.PlayerFrame += IL_PlayerAccessories;

        // IL patches for player drawing - texture offsets
        IL_PlayerDrawSet.BoringSetup_End += IL_PlayerApplyOffsets;
        IL_PlayerDrawLayers.DrawPlayer_12_Skin += IL_PlayerApplySkinOffset;
        IL_PlayerDrawLayers.DrawPlayer_12_Skin_Composite += IL_PlayerApplySkinCompositeOffset;
        IL_PlayerDrawLayers.DrawPlayer_12_SkinComposite_BackArmShirt += IL_PlayerApplySkinCompositeBackArmShirtOffset;
    }

    public override void PostSetupContent()
    {
        ModifyVanillaDrawTransformation(true);
    }

    public override void Unload()
    {
        HeadSlots = null;
        BodySlots = null;
        LegsSlots = null;
        CharacterAndPlayerTextures = null;
    }

    public override void OnModUnload()
    {
        ModifyVanillaDrawTransformation(false);
    }
    #endregion

    #region Modify_Draw_Transformations
    private static void ModifyVanillaDrawTransformation(bool load = true)
    {
        // --- Hacking the vanilla drawlayer's transform field --- \\
        Type playerDrawLayerLoader_Type = typeof(PlayerDrawLayerLoader);
        FieldInfo _layers_Field = playerDrawLayerLoader_Type.GetField("_layers", BindingFlags.NonPublic | BindingFlags.Static);
        object vanillaPlayerDrawLayerList = _layers_Field.GetValue(null);

        if (vanillaPlayerDrawLayerList != null
            && vanillaPlayerDrawLayerList is List<PlayerDrawLayer> layers)
        {
            Type vanillaPlayerDrawLayer_Type = Type.GetType("Terraria.DataStructures.VanillaPlayerDrawLayer, tModLoader");
            Type vanillaPlayerDrawTransform_Type = Type.GetType("Terraria.DataStructures.VanillaPlayerDrawTransform, tModLoader");

            foreach (PlayerDrawLayer layer in layers)
            {
                // Break if the following types could not be found.
                if (vanillaPlayerDrawLayer_Type == null
                    || vanillaPlayerDrawTransform_Type == null)
                {
                    break;
                }

                // The current layer is not of the 'VanillaPlayerDrawLayer' type.
                if (layer.GetType() != vanillaPlayerDrawLayer_Type)
                {
                    continue;
                }

                //--------------------------------------------------------------------------

                // Get the field '_tranform' of the 'VanillaPlayerDrawLayer' type.
                FieldInfo transform_Field = vanillaPlayerDrawLayer_Type.GetField("_transform", BindingFlags.NonPublic | BindingFlags.Instance);

                // The '_transform' field could not be found.
                bool hasTransformField = true;
                if (transform_Field == null)
                {
                    hasTransformField = false;
                }

                // Grab the value of the transform field. This should
                // be a VanillaPlayerDrawTransform object.
                object transform = null;
                if (hasTransformField)
                {
                    transform = transform_Field.GetValue(layer);
                }

                // Check if the field is not null
                bool hasTransformValue = true;
                if (!hasTransformField || transform == null)
                {
                    hasTransformValue = false;
                }

                //--------------------------------------------------------------------------

                // Get the field '_name' of the 'VanillaPlayerDrawLayer' typr
                FieldInfo name_Field = vanillaPlayerDrawLayer_Type.GetField("_name", BindingFlags.NonPublic | BindingFlags.Instance);

                // The '_name field could not be found'
                bool hasNameField = true;
                if (name_Field == null)
                {
                    hasNameField = false;
                }

                // Grab the value of the name field.
                object name = null;
                if (hasNameField)
                {
                    name = name_Field.GetValue(layer);
                }

                // Check if the field is not null
                bool hasNameValue = true;
                if (!hasNameField || name == null)
                {
                    hasNameValue = false;
                }

                //--------------------------------------------------------------------------
                // Check if we have found the values we need
                if (!hasNameField
                    || !hasNameValue
                    || name is not string
                    || !hasTransformField)
                {
                    continue;
                }

                // Check whether we need to modify the transformations in this layer.
                if (LayerNeedsTranformationPatch(name as string))
                {
                    // Loading logic
                    if (load)
                    {
                        // Create a new genshin transformation object
                        GenshinMultiTransform multiTransform = new();

                        // Save the current vanilla tranform object so we can revert our patch 
                        // during unload. We cannot throw away the vanilla object!
                        if (hasTransformValue && transform is PlayerDrawLayer.Transformation)
                        {
                            multiTransform.SetVanillaTransform(transform as PlayerDrawLayer.Transformation);
                        }

                        // Add our own transformations to the multi transform object
                        GetLayerFunctions(ref multiTransform, name as string);

                        // Write back the object to the layer
                        transform_Field.SetValue(layer, multiTransform);
                    }

                    // Unload logic
                    else
                    {
                        if (!hasTransformValue || transform.GetType() != typeof(GenshinMultiTransform))
                        {
                            continue;
                        }

                        GenshinMultiTransform multiTransform = (GenshinMultiTransform)transform;
                        transform_Field.SetValue(layer, multiTransform.GetVanillaTransform());
                    }
                }
            }
        }
    }

    private static bool LayerNeedsTranformationPatch(string name)
    {
        return name switch
        {
            "JimsCloak"
            or "ElectrifiedDebuffBack"
            or "ForbiddenSetRing"
            or "SafemanSun"
            or "WebbedDebuffBack"
            or "LeinforsHairShampoo"
            or "Backpacks"
            or "Tails"
            or "Wings"
            or "HairBack"
            or "BackAcc"
            or "HeadBack"
            or "Leggings"
            or "Shoes"
            or "Robe"
            or "SkinLongCoat"
            or "ArmorLongCoat"
            or "Torso"
            or "OffhandAcc"
            or "WaistAcc"
            or "NeckAcc"
            or "Head"
            or "FinchNest"
            or "FaceAcc"
            or "Pulley"
            or "JimsDroneRadio"
            or "FrontAccBack"
            or "Shield"
            or "SolarShield"
            or "ArmOverItem"
            or "HandOnAcc"
            or "BladedGlove"
            or "ElectrifiedDebuffFront"
            or "FrontAccFront" => true,
            _ => false,
        };
    }

    private static void GetLayerFunctions(ref GenshinMultiTransform transform, string name = null)
    {
        switch (name)
        {
            case "JimsCloak":
            case "ElectrifiedDebuffBack":
            case "ForbiddenSetRing":
            case "SafemanSun":
            case "WebbedDebuffBack":
            case "BackAcc":
            case "SkinLongCoat":
            case "ArmLongCoat":
            case "Torso":
            case "OffhandAcc":
            case "WaistAcc":
            case "NeckAcc":
            case "Pulley":
            case "JimsDroneRadio":
            case "Shield":
            case "SolarShield":
            case "ArmOverItem":
            case "HandOnAcc":
            case "BladedGlove":
            case "ElectrifiedDebuffFront":
            case "FrontAccFront":
                {
                    transform.AddLayerFunctions(new GenshinMultiTransform.LayerFunction(DrawPlayer_AddBodyOffset),
                        new GenshinMultiTransform.LayerFunction(DrawPlayer_RemoveBodyOffset));
                }
                break;

            case "Tails":
                {
                    transform.AddLayerFunctions(new GenshinMultiTransform.LayerFunction(DrawPlayer_AddTailOffset),
                        new GenshinMultiTransform.LayerFunction(DrawPlayer_RemoveTailOffset));
                }
                break;

            case "LeinfsHairShampoo":
            case "HairBack":
            case "HeadBack":
            case "Head":
            case "FinchNest":
            case "FaceAcc":
            case "FrontAccBack":
                {
                    transform.AddLayerFunctions(new GenshinMultiTransform.LayerFunction(DrawPlayer_AddBodyOffset),
                        new GenshinMultiTransform.LayerFunction(DrawPlayer_RemoveBodyOffset));
                    transform.AddLayerFunctions(new GenshinMultiTransform.LayerFunction(DrawPlayer_AddHeadOffset),
                        new GenshinMultiTransform.LayerFunction(DrawPlayer_RemoveHeadOffset));
                }

                break;
            case "Backpacks":
                {
                    transform.AddLayerFunctions(new GenshinMultiTransform.LayerFunction(DrawPlayer_AddBackpackOffset),
                        new GenshinMultiTransform.LayerFunction(DrawPlayer_RemoveBackpackOffset));
                }
                break;

            case "Wings":
                {
                    transform.AddLayerFunctions(new GenshinMultiTransform.LayerFunction(DrawPlayer_AddWingsOffset),
                        new GenshinMultiTransform.LayerFunction(DrawPlayer_RemoveWingsOffset));
                }
                break;

            case "Leggings":
            case "Shoes":
            case "Robe":
                {
                    transform.AddLayerFunctions(new GenshinMultiTransform.LayerFunction(DrawPlayer_AddLegsOffset),
                        new GenshinMultiTransform.LayerFunction(DrawPlayer_RemoveLegsOffset));
                }
                break;
        }
    }
    #endregion

    #region Load_Character_Textures
    private static bool LoadGenshinTexture(GenshinCharacterID characterID, GenshinTextureType textureType)
    {
        if (!CharacterAndPlayerTextures.ContainsKey(characterID))
        {
            CharacterAndPlayerTextures.Add(characterID, new());
        }

        if (!CharacterAndPlayerTextures[characterID].ContainsKey(textureType))
        {
            CharacterAndPlayerTextures[characterID].Add(textureType, null);
        }

        if (CharacterAndPlayerTextures[characterID][textureType] != null)
        {
            return true;
        }

        CharacterAndPlayerTextures[characterID][textureType]
            = Request<Texture2D>("GenshinMod/Content/Characters/" + characterID.ToString() + "/Textures/" + textureType.ToString().Replace("Character", characterID.ToString()));
        
        return true;
    }
    #endregion

    #region EquipTextures
    public static bool RegisterEquipSlot(EquipType equipType, int slot, GenshinEquipSettings settings)
    {
        switch (equipType)
        {
            case EquipType.Head:
                {
                    if (HeadSlots.ContainsKey(slot))
                    {
                        HeadSlots[slot] = settings;
                        return true;
                    }

                    return HeadSlots.TryAdd(slot, settings);
                }

            case EquipType.Body:
                {
                    if (BodySlots.ContainsKey(slot))
                    {
                        BodySlots[slot] = settings;
                        return true;
                    }
                    return BodySlots.TryAdd(slot, settings);
                }
                

            case EquipType.Legs:
                {
                    if (LegsSlots.ContainsKey(slot))
                    {
                        LegsSlots[slot] = settings;
                        return true;
                    }

                    return LegsSlots.TryAdd(slot, settings);
                }
        }

        return false;
    }

    public static bool IsGenshinHeadType(int slot) => HeadSlots.ContainsKey(slot);

    public static bool IsGenshinBodyType(int slot) => BodySlots.ContainsKey(slot);

    public static bool IsGenshinLegsType(int slot) => LegsSlots.ContainsKey(slot);

    #endregion

    #region Drawing_Offset
    private static void CalculateDrawingOffsets(ref PlayerDrawSet drawInfo)
    {
        // Teminate if we do not have a GenshinPlayer
        if (!drawInfo.drawPlayer.TryGetModPlayer(out GenshinPlayer genshinPlayer))
        {
            return;
        }

        // reset all offsets
        genshinPlayer.HeadOffset = Vector2.Zero;
        genshinPlayer.BodyOffset = Vector2.Zero;
        genshinPlayer.LegsOffset = Vector2.Zero;
        genshinPlayer.BackpackOffset = Vector2.Zero;
        genshinPlayer.WingsOffset = Vector2.Zero;
        genshinPlayer.TailOffset = Vector2.Zero;

        // Grab the Player instance
        Player drawPlayer = drawInfo.drawPlayer;

        // Check the Head slot
        if (IsGenshinHeadType(drawPlayer.head))
        {
            ApplyGenshinEquipSettings(ref drawInfo, HeadSlots[drawPlayer.head], EquipType.Head, drawPlayer.headFrame.Y / 56);
        }

        // Check the Body slot
        if (IsGenshinBodyType(drawPlayer.body))
        {
            ApplyGenshinEquipSettings(ref drawInfo, BodySlots[drawPlayer.body], EquipType.Body, drawPlayer.bodyFrame.Y / 56);
        }

        // Check the Legs slot
        if (IsGenshinLegsType(drawPlayer.legs))
        {
            ApplyGenshinEquipSettings(ref drawInfo, LegsSlots[drawPlayer.legs], EquipType.Legs, drawPlayer.legFrame.Y / 56);
        }
    }

    private static void ApplyGenshinEquipSettings(ref PlayerDrawSet drawInfo, GenshinEquipSettings settings, EquipType type , int frameNumber)
    {
        // Grab the genshin player
        GenshinPlayer genshinPlayer = drawInfo.drawPlayer.GetModPlayer<GenshinPlayer>();

        // Head offset
        if (settings.headOffset != null)
        {
            if (settings.headOffset.ContainsKey(frameNumber))
            {
                genshinPlayer.HeadOffset += settings.headOffset[frameNumber];
            }
            else if (settings.headOffset.ContainsKey(-1))
            {
                genshinPlayer.HeadOffset += settings.headOffset[-1];
            }
        }

        // Hat offset
        if (settings.hatOffset != null)
        {
            if (settings.hatOffset.ContainsKey(frameNumber))
            {
                drawInfo.helmetOffset += settings.hatOffset[frameNumber];
            }
            else if (settings.hatOffset.ContainsKey(-1))
            {
                drawInfo.helmetOffset += settings.hatOffset[-1];
            }
        }

        // Body offset
        if (settings.bodyOffset != null)
        {
            if (settings.bodyOffset.ContainsKey(frameNumber)
                && !(drawInfo.isSitting && type != EquipType.Body))
            {
                genshinPlayer.BodyOffset += settings.bodyOffset[frameNumber];
            }
            else if (settings.bodyOffset.ContainsKey(-1)
                && !(drawInfo.isSitting && type != EquipType.Body))
            {
                genshinPlayer.BodyOffset += settings.bodyOffset[-1];
            }
        }

        // Legs offset
        if (settings.legsOffset != null)
        {
            if (settings.legsOffset.ContainsKey(frameNumber))
            {
                genshinPlayer.LegsOffset += settings.legsOffset[frameNumber];
            }
            else if (settings.legsOffset.ContainsKey(-1))
            {
                genshinPlayer.LegsOffset += settings.legsOffset[-1];
            }
        }

        // Backpack offset
        if (settings.backpackOffset != null)
        {
            if (settings.backpackOffset.ContainsKey(frameNumber))
            {
                genshinPlayer.BackpackOffset += settings.backpackOffset[frameNumber];
            }
            else if (settings.backpackOffset.ContainsKey(-1))
            {
                genshinPlayer.BackpackOffset += settings.backpackOffset[-1];
            }
        }

        // Wings offset
        if (settings.wingsOffset != null)
        {
            if (settings.wingsOffset.ContainsKey(frameNumber))
            {
                genshinPlayer.WingsOffset += settings.wingsOffset[frameNumber];
            }
            else if (settings.wingsOffset.ContainsKey(-1))
            {
                genshinPlayer.WingsOffset += settings.wingsOffset[-1];
            }
        }

        // Tail offset
        if (settings.tailOffset != null)
        {
            if (settings.tailOffset.ContainsKey(frameNumber))
            {
                genshinPlayer.TailOffset += settings.tailOffset[frameNumber];
            }
            else if (settings.tailOffset.ContainsKey(-1))
            {
                genshinPlayer.TailOffset += settings.tailOffset[-1];
            }
        }
    }
    #endregion

    #region Character_Equipment
    private static int GetCharacterEquipmentSlot(Player player, GenshinEquipType type)
    {
        return player.GetModPlayer<GenshinPlayer>().CurrentCharacter.GetEquipSlot(type);
    }

    private static void UpdateVisibleCharacter(Player player)
    {
        foreach (GenshinEquipType type in Enum.GetValues(typeof(GenshinEquipType)))
        {
            // Skip Head, Body and legs, these are already loaded at the time this hook runs
            if (type is GenshinEquipType.Head or GenshinEquipType.Body or GenshinEquipType.Legs)
            {
                continue;
            }

            // Get the equipment/armor slot from the character.
            int equipSlot = GetCharacterEquipmentSlot(player, type);

            // Assign the slot from the current character
            switch (type)
            {
                case GenshinEquipType.Handon: player.handon = equipSlot; break;
                case GenshinEquipType.Handoff: player.handoff = equipSlot; break;
                case GenshinEquipType.Back: player.back = equipSlot; break;
                case GenshinEquipType.Front: player.front = equipSlot; break;
                case GenshinEquipType.Shoe: player.shoe = equipSlot; break;
                case GenshinEquipType.Waist: player.waist = equipSlot; break;
                case GenshinEquipType.Shield: player.shield = equipSlot; break;
                case GenshinEquipType.Neck: player.neck = equipSlot; break;
                case GenshinEquipType.Face: player.face = equipSlot; break;
                case GenshinEquipType.Balloon: player.balloon = equipSlot; break;
                case GenshinEquipType.Backpack: player.backpack = equipSlot; break;
                case GenshinEquipType.Tail: player.tail = equipSlot; break;
                case GenshinEquipType.FaceHead: player.faceHead = equipSlot; break;
                case GenshinEquipType.FaceFlower: player.faceFlower = equipSlot; break;
                case GenshinEquipType.BalloonFront: player.balloonFront = equipSlot; break;
                case GenshinEquipType.Beard: player.beard = equipSlot; break;
                case GenshinEquipType.Wings: player.wings = equipSlot; break;
            }
        }
    }
    #endregion

    #region Character_Textures
    private static Asset<Texture2D> GetCharacterTexture(ref PlayerDrawSet drawInfo, GenshinTextureType textureType)
    {
        // Load an empty texture in case we encounter a problem
        _emptyTexture ??= Request<Texture2D>("GenshinMod/Content/_emptyTexture");

        // Decide which texture slot we need to draw
        switch (textureType)
        {
            case GenshinTextureType.Character_SkinHead:
            case GenshinTextureType.Character_Hair:
            case GenshinTextureType.Character_HairAlt:
            case GenshinTextureType.Character_EyeWhite:
            case GenshinTextureType.Character_Eyes:
            case GenshinTextureType.Character_Eyelids:
            case GenshinTextureType.Character_SkinTorso:
            case GenshinTextureType.Character_SkinLegs:
            case GenshinTextureType.Character_SkinHandsBack:
            case GenshinTextureType.Character_SkinHandsFront:
            case GenshinTextureType.Character_SkinArmsAndHands:
                {
                    // Grab the current character ID from the player
                    GenshinCharacterID characterID = drawInfo.drawPlayer.GetModPlayer<GenshinPlayer>().CurrentCharacterID;

                    // Load the texture
                    LoadGenshinTexture(characterID, textureType);
                    return CharacterAndPlayerTextures[characterID][textureType];
                }

            case GenshinTextureType.Player_SkinHead:
            case GenshinTextureType.Mannequin_SkinHead:
                {
                    // Fail-safe
                    if (!IsGenshinHeadType(drawInfo.drawPlayer.head))
                    {
                        return _emptyTexture;
                    }

                    // Grab the equipped character's texture ID from the EquipSettings of this head piece
                    GenshinCharacterID characterID = HeadSlots[drawInfo.drawPlayer.head].characterID;

                    // Load the player skin textures
                    LoadGenshinTexture(characterID, textureType);
                    return CharacterAndPlayerTextures[characterID][textureType];
                }

            case GenshinTextureType.Player_SkinTorso:
            case GenshinTextureType.Mannequin_SkinTorso:
            case GenshinTextureType.Player_SkinHandsBack:
            case GenshinTextureType.Mannequin_SkinHandsBack:
            case GenshinTextureType.Player_SkinHandsFront:
            case GenshinTextureType.Mannequin_SkinHandsFront:
            case GenshinTextureType.Player_SkinArmsAndHands:
            case GenshinTextureType.Mannequin_SkinArmsAndHands:
                {
                    // Fail-safe
                    if (!IsGenshinBodyType(drawInfo.drawPlayer.body))
                    {
                        return _emptyTexture;
                    }

                    // Grab the equipped character's texture ID from the EquipSettings of this body piece
                    GenshinCharacterID characterID = BodySlots[drawInfo.drawPlayer.body].characterID;

                    // Load the player skin textures
                    LoadGenshinTexture(characterID, textureType);
                    return CharacterAndPlayerTextures[characterID][textureType];
                }

            case GenshinTextureType.Player_SkinLegs:
            case GenshinTextureType.Mannequin_SkinLegs:
                {
                    // Fail-safe
                    if (!IsGenshinLegsType(drawInfo.drawPlayer.legs))
                    {
                        return _emptyTexture;
                    }

                    // Grab the equipped character's texture ID from the EquipSettings of this body piece
                    GenshinCharacterID characterID = LegsSlots[drawInfo.drawPlayer.legs].characterID;

                    // Load the player skin textures
                    LoadGenshinTexture(characterID, textureType);
                    return CharacterAndPlayerTextures[characterID][textureType];
                }

            default:
                {
                    return _emptyTexture;
                }
        }
    }
    #endregion

    #region IL

    #region IL_Checks
    // Checks if the drawPlayer is currently using a GenshinCharacter
    // Must be a player, not a mannaquin/womannequin.
    public static bool GenshinCharacterActive(ref PlayerDrawSet drawInfo)
    {
        return drawInfo.skinVar != 10
            && drawInfo.skinVar != 11
            && GenshinPlayerCharacterActive(drawInfo.drawPlayer);
    }

    // Second check if the player is using a character.
    public static bool GenshinPlayerCharacterActive(Player player)
    { 
        return !player.isDisplayDollOrInanimate
            && !player.isHatRackDoll
            && player.TryGetModPlayer(out GenshinPlayer genshinPlayer)
            && genshinPlayer.CurrentCharacterID != GenshinCharacterID.Terrarian
            && genshinPlayer.GenshinModeEnabled;
    }

    public static bool InanimatePlayerActive(ref PlayerDrawSet drawInfo)
    {
        return (drawInfo.skinVar == 10 || drawInfo.skinVar == 11)
            && (drawInfo.drawPlayer.isDisplayDollOrInanimate || drawInfo.drawPlayer.isHatRackDoll);
    }

    // Checks if the drawPlayer is currently wearing a genshin equiptexture head piece
    private static bool GenshinHeadTypeActive(ref PlayerDrawSet drawInfo) => IsGenshinHeadType(drawInfo.drawPlayer.head);

    // Checks if the drawPlayer is currently wearing a genshin equiptexture body piece
    private static bool GenshinBodyTypeActive(ref PlayerDrawSet drawInfo) => IsGenshinBodyType(drawInfo.drawPlayer.body);

    // Checks if the drawPlayer is currently wearing a genshin equiptexture leg piece
    private static bool GenshinLegTypeActive(ref PlayerDrawSet drawInfo) => IsGenshinLegsType(drawInfo.drawPlayer.legs);

    #endregion

    #region IL_Delegates
    // Delegate types
    private delegate Asset<Texture2D> TexAction<T>(ref PlayerDrawSet drawInfo, GenshinTextureType textureType);
    private delegate bool BoolAction<T>(ref PlayerDrawSet drawInfo);
    private delegate float FloatAction<T>(ref PlayerDrawSet drawInfo);
    private delegate void VoidAction<T>(ref PlayerDrawSet drawInfo);
    #endregion

    #region IL_Patches
    private void IL_PlayerTextures(ILContext il)
    {
        ILCursor c = new(il);
        int vanillaTextureID = -1;

        while (c.Next != null)
        {
            // Player body texture
            if (c.Next.MatchLdsfld("Terraria.GameContent.TextureAssets", "Players")
                && c.Next.Next.MatchLdarg(0)
                && c.Next.Next.Next.MatchLdfld<PlayerDrawSet>("skinVar")
                && c.Next.Next.Next.Next.MatchLdcI4(out vanillaTextureID)
                && c.Next.Next.Next.Next.Next.MatchCall("ReLogic.Content.Asset`1<Microsoft.Xna.Framework.Graphics.Texture2D>[0...,0...]", "Get"))
            {
                // Found IL code does not need an edit:
                bool ILPatchNeeded = true;

                // Code contains .Value cast for the texture
                bool textureValue = false;

                // Move cursor forward
                c.Index += 5;

                // Check if the get_Value instructions is present
                if (c.Next.MatchCallvirt<Asset<Texture2D>>("get_Value"))
                {
                    textureValue = true;
                }

                // Move cursor backwards
                c.Index -= 5;

                // Prepare texture types to insert
                GenshinTextureType playerTexture = GenshinTextureType.None;
                GenshinTextureType characterTexture = GenshinTextureType.None;
                GenshinTextureType mannequinTexture = GenshinTextureType.None;

                switch (vanillaTextureID)
                {
                    case 0:
                        playerTexture = GenshinTextureType.Player_SkinHead;
                        characterTexture = GenshinTextureType.Character_SkinHead;
                        mannequinTexture = GenshinTextureType.Mannequin_SkinHead;
                        break;

                    case 1:
                        characterTexture = GenshinTextureType.Character_EyeWhite;
                        break;

                    case 2:
                        characterTexture = GenshinTextureType.Character_Eyes;
                        break;

                    case 3:
                        playerTexture = GenshinTextureType.Player_SkinTorso;
                        characterTexture = GenshinTextureType.Character_SkinTorso;
                        mannequinTexture = GenshinTextureType.Mannequin_SkinTorso;
                        break;

                    case 5:
                        playerTexture = GenshinTextureType.Player_SkinHandsBack;
                        characterTexture = GenshinTextureType.Character_SkinHandsBack;
                        mannequinTexture = GenshinTextureType.Mannequin_SkinHandsBack;
                        break;

                    case 7:
                        playerTexture = GenshinTextureType.Player_SkinArmsAndHands;
                        characterTexture = GenshinTextureType.Character_SkinArmsAndHands;
                        mannequinTexture = GenshinTextureType.Mannequin_SkinArmsAndHands;
                        break;

                    case 9:
                        playerTexture = GenshinTextureType.Player_SkinHandsFront;
                        characterTexture = GenshinTextureType.Character_SkinHandsFront;
                        mannequinTexture = GenshinTextureType.Mannequin_SkinHandsFront;
                        break;

                    case 10:
                        playerTexture = GenshinTextureType.Player_SkinLegs;
                        characterTexture = GenshinTextureType.Character_SkinLegs;
                        mannequinTexture = GenshinTextureType.Mannequin_SkinLegs;
                        break;

                    case 15:
                        characterTexture = GenshinTextureType.Character_Eyelids;
                        break;

                    default:
                        ILPatchNeeded = false;
                        break;
                }

                // Need to apply the edit
                if (ILPatchNeeded)
                {
                    // Define branching labels
                    ILLabel branchPlayer = il.DefineLabel();
                    ILLabel branchCharacter = il.DefineLabel();
                    ILLabel branchMannequin = il.DefineLabel();
                    ILLabel end = il.DefineLabel();

                    // Check if we need to apply a CharacterActive check here
                    if (characterTexture != GenshinTextureType.None)
                    {
                        // Push the PlayerDrawSet to the stack
                        c.Emit(OpCodes.Ldarg_0);

                        // Determine if we need to go to the character branch
                        c.EmitDelegate<BoolAction<PlayerDrawSet>>(GenshinCharacterActive);

                        // When there's a TRUE on the stack => go to character branch
                        c.Emit(OpCodes.Brtrue, branchCharacter);
                    }

                    // Check if we need to apply a leg/torso check here
                    if (playerTexture != GenshinTextureType.None || mannequinTexture != GenshinTextureType.None)
                    {
                        // Push the PlayerDrawSet to the stack
                        c.Emit(OpCodes.Ldarg_0);

                        // Determine which delegate we need to place here
                        if (playerTexture == GenshinTextureType.Player_SkinLegs)
                        {
                            // Need to check for a leg type
                            c.EmitDelegate<BoolAction<PlayerDrawSet>>(GenshinLegTypeActive);
                        }
                        else if (playerTexture == GenshinTextureType.Player_SkinHead)
                        {
                            // Need to check for a head type
                            c.EmitDelegate<BoolAction<PlayerDrawSet>>(GenshinHeadTypeActive);
                        }
                        else
                        {
                            // Need to check for a torso type
                            c.EmitDelegate<BoolAction<PlayerDrawSet>>(GenshinBodyTypeActive);
                        }

                        // When there's a TRUE on the stack => go to player branch
                        c.Emit(OpCodes.Brtrue, branchPlayer);
                    }

                    // Move cursor
                    c.Index += textureValue ? 6 : 5;

                    // Check if we need to go to the ending branch from here
                    if (playerTexture != GenshinTextureType.None || characterTexture != GenshinTextureType.None)
                    {
                        // Go to end branch
                        c.Emit(OpCodes.Br, end);
                    }

                    // Check if we need the character branch
                    if (characterTexture != GenshinTextureType.None)
                    {
                        // Mark the character branch here
                        c.MarkLabel(branchCharacter);

                        // Push the PlayerDrawSet to the stack
                        c.Emit(OpCodes.Ldarg_0);

                        // Push our characterTexture id to the stack
                        c.Emit(OpCodes.Ldc_I4, (int)characterTexture);

                        // Call our GetCharacterTexture method to place the right texture on the stack
                        c.EmitDelegate<TexAction<PlayerDrawSet>>(GetCharacterTexture);

                        if (textureValue)
                        {
                            c.Emit(OpCodes.Callvirt, typeof(Asset<Texture2D>).GetMethod("get_Value", BindingFlags.Public | BindingFlags.Instance));
                        }

                        // Check whether we also inserted a player branch
                        if (playerTexture != GenshinTextureType.None || mannequinTexture != GenshinTextureType.None)
                        {
                            // In that case we need to jump over it.
                            // Go to the end branch
                            c.Emit(OpCodes.Br, end);
                        }
                    }

                    // Check if we need the player branch
                    if (playerTexture != GenshinTextureType.None || mannequinTexture != GenshinTextureType.None)
                    {
                        // Mark the player branch here
                        c.MarkLabel(branchPlayer);

                        if (playerTexture != GenshinTextureType.None && mannequinTexture != GenshinTextureType.None)
                        {
                            // Push the PlayerDrawSet to the stack
                            c.Emit(OpCodes.Ldarg_0);

                            // Determine if this player is Inanimate
                            c.EmitDelegate<BoolAction<PlayerDrawSet>>(InanimatePlayerActive);

                            // Go to the mannequin branch if true
                            c.Emit(OpCodes.Brtrue, branchMannequin);
                        }

                        if (playerTexture != GenshinTextureType.None)
                        {
                            // Push the PlayerDrawSet to the stack
                            c.Emit(OpCodes.Ldarg_0);

                            // Push our playerTexture id to the stack
                            c.Emit(OpCodes.Ldc_I4, (int)playerTexture);

                            // Call our GetCharacterTexture method to place the right texture on the stack
                            c.EmitDelegate<TexAction<PlayerDrawSet>>(GetCharacterTexture);

                            if (textureValue)
                            {
                                c.Emit(OpCodes.Callvirt, typeof(Asset<Texture2D>).GetMethod("get_Value", BindingFlags.Public | BindingFlags.Instance));
                            }
                        }

                        if (playerTexture != GenshinTextureType.None && mannequinTexture != GenshinTextureType.None)
                        {
                            // Go to the end branch
                            c.Emit(OpCodes.Br, end);
                        }

                        if (mannequinTexture != GenshinTextureType.None)
                        {
                            // Mark the mannequin branch here
                            c.MarkLabel(branchMannequin);

                            // Push the PlayerDrawSet to the stack
                            c.Emit(OpCodes.Ldarg_0);

                            // Push our playerTexture id to the stack
                            c.Emit(OpCodes.Ldc_I4, (int)mannequinTexture);

                            // Call our GetCharacterTexture method to place the right texture on the stack
                            c.EmitDelegate<TexAction<PlayerDrawSet>>(GetCharacterTexture);

                            if (textureValue)
                            {
                                c.Emit(OpCodes.Callvirt, typeof(Asset<Texture2D>).GetMethod("get_Value", BindingFlags.Public | BindingFlags.Instance));
                            }
                        }
                    }

                    // Check if we need the end branch
                    if (playerTexture != GenshinTextureType.None || characterTexture != GenshinTextureType.None || mannequinTexture != GenshinTextureType.None)
                    {
                        // Mark the end branch here
                        c.MarkLabel(end);
                    }
                }

                // Otherwise skip this piece of IL code
                else
                {
                    c.Index += textureValue ? 6 : 5;
                }
            }

            // Player hair texture
            else if (c.Next.MatchLdsfld("Terraria.GameContent.TextureAssets", "PlayerHair")
                && c.TryGotoNext(
                    x => x.MatchLdsfld("Terraria.GameContent.TextureAssets", "PlayerHair"),
                    x => x.MatchLdarg(0),
                    x => x.MatchLdfld<PlayerDrawSet>("drawPlayer"),
                    x => x.MatchLdfld<Player>("hair"),
                    x => x.MatchLdelemRef(),
                    x => x.MatchCallvirt<Asset<Texture2D>>("get_Value")))
            {
                // Create 2 new labels for branches
                ILLabel branch = c.DefineLabel();
                ILLabel end = c.DefineLabel();

                // Push the PLayerDrawSet to the stack
                c.Emit(OpCodes.Ldarg_0);

                // Call our delegate to figure out if we're currently using a character
                c.EmitDelegate<BoolAction<PlayerDrawSet>>(GenshinCharacterActive);

                // Evaluate the boolean on the stack => branch if needed.
                c.Emit(OpCodes.Brtrue, branch);

                // move cursor
                c.Index += 6;

                // Go to the end label
                c.Emit(OpCodes.Br, end);

                // Mark the start of the branch label
                c.MarkLabel(branch);

                // Load our swapped texture
                c.Emit(OpCodes.Ldarg_0);
                c.Emit(OpCodes.Ldc_I4, (int)GenshinTextureType.Character_Hair);
                c.EmitDelegate<TexAction<PlayerDrawSet>>(GetCharacterTexture);
                c.Emit(OpCodes.Callvirt, typeof(Asset<Texture2D>).GetMethod("get_Value", BindingFlags.Public | BindingFlags.Instance));

                // Mark the start of the end label
                c.MarkLabel(end);
            }

            // Player hairAlt texture
            else if (c.Next.MatchLdsfld("Terraria.GameContent.TextureAssets", "PlayerHairAlt")
                && c.TryGotoNext(
                    x => x.MatchLdsfld("Terraria.GameContent.TextureAssets", "PlayerHairAlt"),
                    x => x.MatchLdarg(0),
                    x => x.MatchLdfld<PlayerDrawSet>("drawPlayer"),
                    x => x.MatchLdfld<Player>("hair"),
                    x => x.MatchLdelemRef(),
                    x => x.MatchCallvirt<Asset<Texture2D>>("get_Value")))
            {
                // Create 2 new labels for branches
                ILLabel branch = c.DefineLabel();
                ILLabel end = c.DefineLabel();

                // Push the PLayerDrawSet to the stack
                c.Emit(OpCodes.Ldarg_0);

                // Call our delegate to figure out if we're currently using a character
                c.EmitDelegate<BoolAction<PlayerDrawSet>>(GenshinCharacterActive);

                // Evaluate the boolean on the stack => branch if needed.
                c.Emit(OpCodes.Brtrue, branch);

                // Move cursor
                c.Index += 6;

                // Go to the end label
                c.Emit(OpCodes.Br, end);

                // Mark the start of the branch label
                c.MarkLabel(branch);

                // Load our swapped texture
                c.Emit(OpCodes.Ldarg_0);
                c.Emit(OpCodes.Ldc_I4, (int)GenshinTextureType.Character_HairAlt);
                c.EmitDelegate<TexAction<PlayerDrawSet>>(GetCharacterTexture);
                c.Emit(OpCodes.Callvirt, typeof(Asset<Texture2D>).GetMethod("get_Value", BindingFlags.Public | BindingFlags.Instance));

                // Mark the start of the end label
                c.MarkLabel(end);
            }

            // Nothing found: Increase Index
            else
            {
                c.Index++;
            }
        }
    }

    private void IL_PlayerColors(ILContext il)
    {
        ILCursor c = new(il);

        while (c.Next != null)
        {
            if (c.Next.MatchLdarg(0)
                && c.Next.Next.MatchLdfld<PlayerDrawSet>("drawPlayer"))            
            {
                // Move cursor
                c.Index += 2;
                
                // Check next instructions
                if (!(c.Next.MatchLdfld<Player>("eyeColor")
                    || c.Next.MatchLdfld<Player>("skinColor")
                    || c.Next.MatchLdfld<Player>("shirtColor")
                    || c.Next.MatchLdfld<Player>("underShirtColor")
                    || c.Next.MatchLdfld<Player>("pantsColor")
                    || c.Next.MatchLdfld<Player>("shoeColor")))
                {
                    continue;
                }

                // Move cursor
                c.Index -= 2;

                // Create branches
                ILLabel branch = c.DefineLabel();
                ILLabel end = c.DefineLabel();

                // Push the PLayerDrawSet to the stack
                c.Emit(OpCodes.Ldarg_0);

                // Call our delegate to figure out if we're currently using a character
                c.EmitDelegate<BoolAction<PlayerDrawSet>>(GenshinCharacterActive);

                // Evaluate the boolean on the stack => branch if needed.
                c.Emit(OpCodes.Brtrue, branch);

                // Move cursor
                c.Index += 3;

                // Move to the end
                c.Emit(OpCodes.Br, end);

                // Mark the start of the branch label
                c.MarkLabel(branch);

                // Insert color.white
                c.Emit(OpCodes.Call, typeof(Color).GetMethod("get_White", BindingFlags.Public | BindingFlags.Static));

                // Mark the start of the end label
                c.MarkLabel(end);
            }
            else
            {
                c.Index++;
            }
        }
    }

    private void IL_PlayerHairColor(ILContext il)
    {
        ILCursor c = new(il);

        // Create a new Label to save the exit point for our if statement
        ILLabel bypassShaderLbl = null!;

        // Search for the piece of code we want to insert after
        if (c.TryGotoNext(
            x => x.MatchLdarg(0),
            x => x.MatchLdfld<HairShaderDataSet>("_shaderDataCount"),
            x => x.MatchBgt(out bypassShaderLbl)))
        {
            // Move the cursor behind these instructions
            c.Index += 3;

            // Push the player instance on the stack
            c.Emit(OpCodes.Ldarg, 2);

            // determine if the player is current using a character
            c.EmitDelegate(GenshinPlayerCharacterActive);

            // Branch to the saved label
            c.Emit(OpCodes.Brtrue, bypassShaderLbl);
        }

        // Search for the player haircolor 
        if (c.TryGotoNext(
            x => x.MatchLdarg(2),
            x => x.MatchLdflda<Player>("hairColor"),
            x => x.MatchCall<Color>("ToVector4")))
        {
            // Create new branch labels
            ILLabel branch = il.DefineLabel();
            ILLabel end = il.DefineLabel();

            // Push the player instance on the stack
            c.Emit(OpCodes.Ldarg, 2);

            // determine if the player is current using a character
            c.EmitDelegate(GenshinPlayerCharacterActive);

            // branch to our label
            c.Emit(OpCodes.Brtrue, branch);

            // Move cursor forward
            c.Index += 3;

            // Go to the end label
            c.Emit(OpCodes.Br, end);

            // Mark the branch label
            c.MarkLabel(branch);

            // Insert the color white and make it vector4
            c.EmitDelegate(Color.White.ToVector4);

            // Mark the end label
            c.MarkLabel(end);
        }
    }

    private void IL_PlayerShaders(ILContext il)
    {
        ILCursor c = new(il);

        while (c.Next != null)
        {
            if (c.Next.MatchLdarg(0)
                && c.Next.Next.MatchLdfld<PlayerDrawSet>("drawPlayer"))
            {
                // Move cursor
                c.Index += 2;

                // check if the next instruction is a LDFLD, if yes
                // export the FieldReference.
                if (!c.Next.MatchLdfld(out FieldReference field)
                    || field.Name == "lastVisualizedSelectedItem")
                {
                    continue;
                }

                // Move cursor
                c.Index -= 2;

                // Create new branch labels
                ILLabel branch = il.DefineLabel();
                ILLabel end = il.DefineLabel();

                // Push the player instance on the stack
                c.Emit(OpCodes.Ldarg, 0);

                // determine if the player is current using a character
                c.EmitDelegate<BoolAction<PlayerDrawSet>>(GenshinCharacterActive);

                // branch to our label
                c.Emit(OpCodes.Brtrue, branch);

                // Move cursor forward
                c.Index += 3;

                // Go to the end label
                c.Emit(OpCodes.Br, end);

                // Mark the branch label
                c.MarkLabel(branch);

                // Insert shader id 0
                c.Emit(OpCodes.Ldc_I4, 0);

                // Mark the end label
                c.MarkLabel(end);
            }
            else
            {
              c.Index++;
            }
        }
    }

    private void IL_PlayerArmor(ILContext il)
    {
        ILCursor c = new(il);

        FieldReference field = null;

        while (c.Next != null)
        {
            bool MainOrPlayer = false;

            int startIndex = c.Index;
            int instrAmount = 0;

            // Found an armor slot call with Main.player array
            if (c.Next.MatchLdsfld<Main>("player")
                && c.Next.Next.MatchLdsfld<Main>("myPlayer")
                && c.Next.Next.Next.MatchLdelemRef()
                && c.Next.Next.Next.Next.MatchLdfld<Player>("armor"))
            {
                MainOrPlayer = true;
                c.Index += 4;
            }

            // Found an armor slot call with the current player
            else if (c.Next.MatchLdarg(0)
                && c.Next.Next.MatchLdfld<Player>("armor"))
            {
                MainOrPlayer = false;
                c.Index += 2;
            }

            // Nothing found: continue
            else
            {
                c.Index++;
                continue;
            }

            // --- Apply IL patch ---

            // Found the armor number for the array, check if it's one we need to edit
            if (!c.Next.MatchLdcI4(out int slotNumber)
                || !(slotNumber == 0
                || slotNumber == 1
                || slotNumber == 2
                || slotNumber == 10
                || slotNumber == 11
                || slotNumber == 12))
            {
                continue;
            }

            // Everything found: Move cursor forward
            c.Index++;

            // Check the following instructions
            if (!(c.Next.MatchLdelemRef()
                && c.Next.Next.MatchLdfld(out field))
                || (c.Next.Next.Next.MatchLdcI4(0) 
                && c.Next.Next.Next.Next.MatchBlt(out _)))
            {
                continue;
            }

            // Everything found: Move cursor forward
            c.Index += 2;

            // Save the amount of instructions we need to move
            instrAmount = c.Index - startIndex;

            // Move the cursor back to the starting point
            c.Index = startIndex;

            GenshinEquipType type;

            switch (field.Name)
            {
                case "headSlot":
                    type = GenshinEquipType.Head;
                    break;

                case "bodySlot":
                    type = GenshinEquipType.Body;
                    break;

                case "legSlot":
                    type = GenshinEquipType.Legs;
                    break;

                default:
                    c.Index += instrAmount;
                    continue;
            }

            // Create new branch labels
            ILLabel branch = il.DefineLabel();
            ILLabel end = il.DefineLabel();

            // Push the player instance on the stack
            if (MainOrPlayer)
            {
                c.Emit(OpCodes.Ldsfld, typeof(Main).GetField("player", BindingFlags.Static | BindingFlags.Public));
                c.Emit(OpCodes.Ldsfld, typeof(Main).GetField("myPlayer", BindingFlags.Static | BindingFlags.Public));
                c.Emit(OpCodes.Ldelem_Ref);
            }
            else
            {
                c.Emit(OpCodes.Ldarg_0);
            }

            // determine if the player is current using a character
            c.EmitDelegate(GenshinPlayerCharacterActive);

            // branch to our label
            c.Emit(OpCodes.Brtrue, branch);

            // Move cursor forward
            c.Index += instrAmount;
            
            // Go to the end label
            c.Emit(OpCodes.Br, end);
            
            // Mark the branch label
            c.MarkLabel(branch);

            // Push the player object to the stack
            if (MainOrPlayer)
            {
                c.Emit(OpCodes.Ldsfld, typeof(Main).GetField("player", BindingFlags.Static | BindingFlags.Public));
                c.Emit(OpCodes.Ldsfld, typeof(Main).GetField("myPlayer", BindingFlags.Static | BindingFlags.Public));
                c.Emit(OpCodes.Ldelem_Ref);
            }
            else
            {
                c.Emit(OpCodes.Ldarg_0);
            }

            // Push the EquipType to the stack
            c.Emit(OpCodes.Ldc_I4, (int)type);

            // Get the equipslot of this piece from our character
            c.EmitDelegate(GetCharacterEquipmentSlot);

            // Mark the end label
            c.MarkLabel(end);
        }
    }

    private void IL_PlayerAccessories(ILContext il)
    {
        ILCursor c = new(il);

        while (c.Next != null)
        {
            if (c.Next.MatchLdarg(0)
                && c.Next.Next.MatchCall<Player>("UpdateVisibleAccessories"))
            {
                // Create new branch labels
                ILLabel branch = il.DefineLabel();
                ILLabel end = il.DefineLabel();

                // Push the player instance on the stack
                c.Emit(OpCodes.Ldarg_0);

                // determine if the player is current using a character
                c.EmitDelegate(GenshinPlayerCharacterActive);

                // branch to our label
                c.Emit(OpCodes.Brtrue, branch);

                // Move cursor forward
                c.Index += 2;

                // Go to the end
                c.Emit(OpCodes.Br, end);

                // Mark the branch label
                c.MarkLabel(branch);

                // Push the player object to the stack
                c.Emit(OpCodes.Ldarg_0);

                // Call our own drawing method
                c.EmitDelegate(UpdateVisibleCharacter);

                // Mark the end label
                c.MarkLabel(end);
            }
            else
            { 
                c.Index++;
            }
        }
    }

    private void IL_PlayerApplyOffsets(ILContext il)
    {
        ILCursor c = new(il);

        // Move the cursor to the end
        while (c.Next != null)
        {
            c.Index++;
        }

        // Move one back, just before the return instruction
        c.Index--;

        // Insert our offset code
        c.Emit(OpCodes.Ldarg, 0);
        c.EmitDelegate<VoidAction<PlayerDrawSet>>(CalculateDrawingOffsets);
    }

    private void IL_PlayerApplySkinOffset(ILContext il)
    {
        ILCursor c = new(il);

        if (c.TryGotoNext(
            x => x.MatchLdloca(0)))
        {
            c.Index++;

            if (c.TryGotoNext(
                x => x.MatchLdarg(0),
                x => x.MatchLdflda<PlayerDrawSet>("Position"),
                x => x.MatchLdfld<Vector2>("X")))
            {
                c.Index += 3;

                c.Emit(OpCodes.Ldarg_0);
                c.Emit(OpCodes.Ldfld, typeof(PlayerDrawSet).GetField("drawPlayer", BindingFlags.Public | BindingFlags.Instance));
                c.EmitDelegate(GetBodyOffsetX);
                c.Emit(OpCodes.Add);
            }

            if (c.TryGotoNext(
                x => x.MatchLdarg(0),
                x => x.MatchLdflda<PlayerDrawSet>("Position"),
                x => x.MatchLdfld<Vector2>("Y")))
            {
                c.Index += 3;

                c.Emit(OpCodes.Ldarg_0);
                c.Emit(OpCodes.Ldfld, typeof(PlayerDrawSet).GetField("drawPlayer", BindingFlags.Public | BindingFlags.Instance));
                c.EmitDelegate(GetBodyOffsetY);
                c.Emit(OpCodes.Add);
            }
        }

        if (c.TryGotoNext(
            x => x.MatchLdloca(2)))
        {
            c.Index++;

            if (c.TryGotoNext(
                x => x.MatchLdarg(0),
                x => x.MatchLdflda<PlayerDrawSet>("Position"),
                x => x.MatchLdfld<Vector2>("X")))
            {
                c.Index += 3;

                c.Emit(OpCodes.Ldarg_0);
                c.Emit(OpCodes.Ldfld, typeof(PlayerDrawSet).GetField("drawPlayer", BindingFlags.Public | BindingFlags.Instance));
                c.EmitDelegate(GetLegsOffsetX);
                c.Emit(OpCodes.Add);
            }

            if (c.TryGotoNext(
                x => x.MatchLdarg(0),
                x => x.MatchLdflda<PlayerDrawSet>("Position"),
                x => x.MatchLdfld<Vector2>("Y")))
            {
                c.Index += 3;

                c.Emit(OpCodes.Ldarg_0);
                c.Emit(OpCodes.Ldfld, typeof(PlayerDrawSet).GetField("drawPlayer", BindingFlags.Public | BindingFlags.Instance));
                c.EmitDelegate(GetLegsOffsetY);
                c.Emit(OpCodes.Add);
            }
        }
    }

    private void IL_PlayerApplySkinCompositeOffset(ILContext il)
    {
        ILCursor c = new(il);

        if (c.TryGotoNext(
            x => x.MatchLdarg(0),
            x => x.MatchLdflda<PlayerDrawSet>("Position"),
            x => x.MatchLdfld<Vector2>("X")))
        {
            c.Index += 3;

            c.Emit(OpCodes.Ldarg_0);
            c.Emit(OpCodes.Ldfld, typeof(PlayerDrawSet).GetField("drawPlayer", BindingFlags.Public | BindingFlags.Instance));
            c.EmitDelegate(GetBodyOffsetX);
            c.Emit(OpCodes.Add);
        }

        if (c.TryGotoNext(
            x => x.MatchLdarg(0),
            x => x.MatchLdflda<PlayerDrawSet>("Position"),
            x => x.MatchLdfld<Vector2>("Y")))
        {
            c.Index += 3;

            c.Emit(OpCodes.Ldarg_0);
            c.Emit(OpCodes.Ldfld, typeof(PlayerDrawSet).GetField("drawPlayer", BindingFlags.Public | BindingFlags.Instance));
            c.EmitDelegate(GetBodyOffsetY);
            c.Emit(OpCodes.Add);
        }

        if (c.TryGotoNext(
            x => x.MatchLdloca(7)))
        {
            c.Index++;

            if (c.TryGotoNext(
                x => x.MatchLdloc(0)))
            {
                c.Index++;

                c.Emit(OpCodes.Ldarg_0);
                c.Emit(OpCodes.Ldfld, typeof(PlayerDrawSet).GetField("drawPlayer", BindingFlags.Instance | BindingFlags.Public));
                c.EmitDelegate<Func<Vector2, Player, Vector2>>((position, player) =>
                {
                    if (player.TryGetModPlayer(out GenshinPlayer genshinPlayer))
                    {
                        position += genshinPlayer.HeadOffset;
                    }

                    return position;
                });
            }
        }

        if (c.TryGotoNext(
            x => x.MatchLdloca(8)))
        {
            c.Index++;

            if (c.TryGotoNext(
                x => x.MatchLdarg(0),
                x => x.MatchLdflda<PlayerDrawSet>("Position"),
                x => x.MatchLdfld<Vector2>("X")))
            {
                c.Index += 3;

                c.Emit(OpCodes.Ldarg_0);
                c.Emit(OpCodes.Ldfld, typeof(PlayerDrawSet).GetField("drawPlayer", BindingFlags.Public | BindingFlags.Instance));
                c.EmitDelegate(GetLegsOffsetX);
                c.Emit(OpCodes.Add);
            }

            if (c.TryGotoNext(
                x => x.MatchLdarg(0),
                x => x.MatchLdflda<PlayerDrawSet>("Position"),
                x => x.MatchLdfld<Vector2>("Y")))
            {
                c.Index += 3;

                c.Emit(OpCodes.Ldarg_0);
                c.Emit(OpCodes.Ldfld, typeof(PlayerDrawSet).GetField("drawPlayer", BindingFlags.Public | BindingFlags.Instance));
                c.EmitDelegate(GetLegsOffsetY);
                c.Emit(OpCodes.Add);
            }
        }

        GenshinMod.Instance.Logger.Debug(il.ToString());
    }

    private void IL_PlayerApplySkinCompositeBackArmShirtOffset(ILContext il)
    {
        ILCursor c = new(il);

        if (c.TryGotoNext(
                x => x.MatchLdarg(0),
                x => x.MatchLdflda<PlayerDrawSet>("Position"),
                x => x.MatchLdfld<Vector2>("X")))
        {
            c.Index += 3;

            c.Emit(OpCodes.Ldarg_0);
            c.Emit(OpCodes.Ldfld, typeof(PlayerDrawSet).GetField("drawPlayer", BindingFlags.Public | BindingFlags.Instance));
            c.EmitDelegate(GetBodyOffsetX);
            c.Emit(OpCodes.Add);
        }

        if (c.TryGotoNext(
            x => x.MatchLdarg(0),
            x => x.MatchLdflda<PlayerDrawSet>("Position"),
            x => x.MatchLdfld<Vector2>("Y")))
        {
            c.Index += 3;

            c.Emit(OpCodes.Ldarg_0);
            c.Emit(OpCodes.Ldfld, typeof(PlayerDrawSet).GetField("drawPlayer", BindingFlags.Public | BindingFlags.Instance));
            c.EmitDelegate(GetBodyOffsetY);
            c.Emit(OpCodes.Add);
        }
    }

    #endregion
    #endregion

    #region offsets
    private static void DrawPlayer_AddHeadOffset(ref PlayerDrawSet drawInfo)
    {
        if (drawInfo.drawPlayer.TryGetModPlayer(out GenshinPlayer genshinPlayer))
        {
            drawInfo.Position.X += genshinPlayer.HeadOffset.X * drawInfo.drawPlayer.direction;
            drawInfo.Position.Y += genshinPlayer.HeadOffset.Y * drawInfo.drawPlayer.gravDir;
        }
    }

    private static void DrawPlayer_RemoveHeadOffset(ref PlayerDrawSet drawInfo)
    {
        if (drawInfo.drawPlayer.TryGetModPlayer(out GenshinPlayer genshinPlayer))
        {
            drawInfo.Position.X -= genshinPlayer.HeadOffset.X * drawInfo.drawPlayer.direction;
            drawInfo.Position.Y -= genshinPlayer.HeadOffset.Y * drawInfo.drawPlayer.gravDir;
        }
    }

    private static void DrawPlayer_AddLegsOffset(ref PlayerDrawSet drawInfo)
    {
        if (drawInfo.drawPlayer.TryGetModPlayer(out GenshinPlayer genshinPlayer)
            && !drawInfo.isSitting)
        {
            drawInfo.Position.X += genshinPlayer.LegsOffset.X * drawInfo.drawPlayer.direction;
            drawInfo.Position.Y += genshinPlayer.LegsOffset.Y * drawInfo.drawPlayer.gravDir;
        }
    }

    private static void DrawPlayer_RemoveLegsOffset(ref PlayerDrawSet drawInfo)
    {
        if (drawInfo.drawPlayer.TryGetModPlayer(out GenshinPlayer genshinPlayer)
            && !drawInfo.isSitting)
        {
            drawInfo.Position.X -= genshinPlayer.LegsOffset.X * drawInfo.drawPlayer.direction;
            drawInfo.Position.Y -= genshinPlayer.LegsOffset.Y * drawInfo.drawPlayer.gravDir;
        }
    }

    private static void DrawPlayer_AddBodyOffset(ref PlayerDrawSet drawInfo)
    {
        if (drawInfo.drawPlayer.TryGetModPlayer(out GenshinPlayer genshinPlayer))
        {
            drawInfo.Position.X += genshinPlayer.BodyOffset.X * drawInfo.drawPlayer.direction;
            drawInfo.Position.Y += genshinPlayer.BodyOffset.Y * drawInfo.drawPlayer.gravDir;
        }
    }

    private static void DrawPlayer_RemoveBodyOffset(ref PlayerDrawSet drawInfo)
    {
        if (drawInfo.drawPlayer.TryGetModPlayer(out GenshinPlayer genshinPlayer))
        {
            drawInfo.Position.X -= genshinPlayer.BodyOffset.X * drawInfo.drawPlayer.direction;
            drawInfo.Position.Y -= genshinPlayer.BodyOffset.Y * drawInfo.drawPlayer.gravDir;
        }
    }

    private static void DrawPlayer_AddBackpackOffset(ref PlayerDrawSet drawInfo)
    {
        if (drawInfo.drawPlayer.TryGetModPlayer(out GenshinPlayer genshinPlayer))
        {
            drawInfo.Position.X += genshinPlayer.BackpackOffset.X * drawInfo.drawPlayer.direction;
            drawInfo.Position.Y += genshinPlayer.BackpackOffset.Y * drawInfo.drawPlayer.gravDir;
        }
    }

    private static void DrawPlayer_RemoveBackpackOffset(ref PlayerDrawSet drawInfo)
    {
        if (drawInfo.drawPlayer.TryGetModPlayer(out GenshinPlayer genshinPlayer))
        {
            drawInfo.Position.X -= genshinPlayer.BackpackOffset.X * drawInfo.drawPlayer.direction;
            drawInfo.Position.Y -= genshinPlayer.BackpackOffset.Y * drawInfo.drawPlayer.gravDir;
        }
    }

    private static void DrawPlayer_AddWingsOffset(ref PlayerDrawSet drawInfo)
    {
        if (drawInfo.drawPlayer.TryGetModPlayer(out GenshinPlayer genshinPlayer)
            && drawInfo.drawPlayer.wings != ArmorIDs.Wing.Hoverboard
            && drawInfo.drawPlayer.wings != ArmorIDs.Wing.LazuresBarrierPlatform
            && drawInfo.drawPlayer.wings != ArmorIDs.Wing.LongTrailRainbowWings)
        {
            drawInfo.Position.X += genshinPlayer.WingsOffset.X * drawInfo.drawPlayer.direction;
            drawInfo.Position.Y += genshinPlayer.WingsOffset.Y * drawInfo.drawPlayer.gravDir;
        }
    }

    private static void DrawPlayer_RemoveWingsOffset(ref PlayerDrawSet drawInfo)
    {
        if (drawInfo.drawPlayer.TryGetModPlayer(out GenshinPlayer genshinPlayer)
            && drawInfo.drawPlayer.wings != ArmorIDs.Wing.Hoverboard
            && drawInfo.drawPlayer.wings != ArmorIDs.Wing.LazuresBarrierPlatform
            && drawInfo.drawPlayer.wings != ArmorIDs.Wing.LongTrailRainbowWings)
        {
            drawInfo.Position.X -= genshinPlayer.WingsOffset.X * drawInfo.drawPlayer.direction;
            drawInfo.Position.Y -= genshinPlayer.WingsOffset.Y * drawInfo.drawPlayer.gravDir;
        }
    }

    private static void DrawPlayer_AddTailOffset(ref PlayerDrawSet drawInfo)
    {
        if (drawInfo.drawPlayer.TryGetModPlayer(out GenshinPlayer genshinPlayer))
        {
            drawInfo.Position.X += genshinPlayer.TailOffset.X * drawInfo.drawPlayer.direction;
            drawInfo.Position.Y += genshinPlayer.TailOffset.Y * drawInfo.drawPlayer.gravDir;
        }
    }

    private static void DrawPlayer_RemoveTailOffset(ref PlayerDrawSet drawInfo)
    {
        if (drawInfo.drawPlayer.TryGetModPlayer(out GenshinPlayer genshinPlayer))
        {
            drawInfo.Position.X -= genshinPlayer.TailOffset.X * drawInfo.drawPlayer.direction;
            drawInfo.Position.Y -= genshinPlayer.TailOffset.Y * drawInfo.drawPlayer.gravDir;
        }
    }

    private static float GetBodyOffsetX(Player player)
    {
        if (player.TryGetModPlayer(out GenshinPlayer genshinPlayer))
        {
            return genshinPlayer.BodyOffset.X * player.direction;
        }

        return 0f;
    }
    
    private static float GetBodyOffsetY(Player player)
    {
        if (player.TryGetModPlayer(out GenshinPlayer genshinPlayer))
        {
            return genshinPlayer.BodyOffset.Y * player.gravDir;
        }

        return 0f;
    }

    private static float GetLegsOffsetX(Player player)
    {
        if (player.TryGetModPlayer(out GenshinPlayer genshinPlayer))
        {
            return genshinPlayer.LegsOffset.X * player.direction;
        }

        return 0f;
    }

    private static float GetLegsOffsetY(Player player)
    {
        if (player.TryGetModPlayer(out GenshinPlayer genshinPlayer))
        {
            return genshinPlayer.LegsOffset.Y * player.gravDir;
        }

        return 0f;
    }
    #endregion
}

public class GenshinMultiTransform : PlayerDrawLayer.Transformation
{
    private PlayerDrawLayer.Transformation _VanillaTransform = null;
    private readonly List<LayerFunction> _PreDrawFunc = new();
    private readonly List<LayerFunction> _PostDrawFunc = new();

    public GenshinMultiTransform()
    {
        _PreDrawFunc ??= new();
        _PostDrawFunc ??= new();
    }

    public override PlayerDrawLayer.Transformation Parent => _VanillaTransform;

    public void SetVanillaTransform(PlayerDrawLayer.Transformation transform) => _VanillaTransform = transform;

    public PlayerDrawLayer.Transformation GetVanillaTransform() => _VanillaTransform;

    public void AddLayerFunctions(LayerFunction preFunc, LayerFunction PostFunc)
    {
        _PreDrawFunc.Add(preFunc);
        _PostDrawFunc.Add(PostFunc);
    }

    public void AddPreLayerFunction(LayerFunction function) => _PreDrawFunc.Add(function);

    public void AddPostLayerFunction(LayerFunction function) => _PostDrawFunc.Add(function);

    protected override void PreDraw(ref PlayerDrawSet drawInfo)
    {
        foreach (LayerFunction function in _PreDrawFunc)
        {
            function?.Invoke(ref drawInfo);
        }
    }

    protected override void PostDraw(ref PlayerDrawSet drawInfo)
    {
        foreach (LayerFunction function in _PostDrawFunc)
        {
            function?.Invoke(ref drawInfo);
        }
    }

    public delegate void LayerFunction(ref PlayerDrawSet info);
}

public struct GenshinEquipSettings
{
    public GenshinCharacterID characterID;
    public Dictionary<int, Vector2> headOffset;
    public Dictionary<int, Vector2> hatOffset;
    public Dictionary<int, Vector2> bodyOffset;
    public Dictionary<int, Vector2> legsOffset;
    public Dictionary<int, Vector2> backpackOffset;
    public Dictionary<int, Vector2> wingsOffset;
    public Dictionary<int, Vector2> tailOffset;
}

public enum GenshinTextureType
{
    None,

    // Textures used for the player
    Player_SkinHead,
    Player_SkinTorso,            // 3
    Player_SkinLegs,             // 10
    Player_SkinHandsBack,        // 5
    Player_SkinHandsFront,       // 9
    Player_SkinArmsAndHands,     // 7

    // Textures used for Mannequin/Womannequin
    Mannequin_SkinHead,
    Mannequin_SkinTorso,
    Mannequin_SkinHandsBack,
    Mannequin_SkinHandsFront, 
    Mannequin_SkinArmsAndHands,
    Mannequin_SkinLegs,

    // Textures used for the characters
    Character_SkinHead,          // 0
    Character_Hair,
    Character_HairAlt,
    Character_EyeWhite,          // 1
    Character_Eyes,              // 2
    Character_Eyelids,           // 15
    Character_SkinTorso,         // 3
    Character_SkinLegs,          // 10
    Character_SkinHandsBack,     // 5
    Character_SkinHandsFront,    // 9
    Character_SkinArmsAndHands,  // 7
}

public enum GenshinEquipType
{
    Head,
    Body,
    Legs,
    Handon,
    Handoff,
    Back,
    Front,
    Shoe,
    Waist,
    Shield,
    Neck,
    Face,
    Balloon,
    Backpack,
    Tail,
    FaceHead,
    FaceFlower,
    BalloonFront,
    Beard,
    Wings
}