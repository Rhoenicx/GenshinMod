using GenshinMod.Common.Configs;
using GenshinMod.Common.ModObjects;
using GenshinMod.Common.ModObjects.ModSystems;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Terraria;
using Terraria.Chat;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace GenshinMod;

public class GenshinMod : Mod
{
    internal static GenshinMod Instance { get; private set; }

    internal static GenshinConfigClient GenshinConfigClient;
    internal static GenshinConfigServer GenshinConfigServer;

    // Pass the instance of the mod
    public GenshinMod()
    {
        Instance = this;
    }

    // Makes things happen when the mod is loaded by tML
    public override void Load()
    {
        base.Load();
    }

    // Makes things happen when the mod is unloaded
    public override void Unload()
    {
        GenshinConfigClient = null;
        GenshinConfigServer = null;

        Instance = null;

        base.Unload();
    }

    public override void HandlePacket(BinaryReader reader, int whoAmI)
    {
        GenshinModMessageType msgType = (GenshinModMessageType)reader.ReadByte();

        if (Main.netMode == NetmodeID.Server)
        {
            ChatHelper.BroadcastChatMessage(NetworkText.FromLiteral("SERVER RECEIVED: " + msgType + " FROM " + Main.player[whoAmI].name), Color.Red);
        }

        switch (msgType)
        {
            // Client => server => other clients (Has server-side verification)
            case GenshinModMessageType.SendGenshinMode:
            case GenshinModMessageType.SendCharacter:
            case GenshinModMessageType.SendTeamComposition:
            case GenshinModMessageType.SendSelectedCharacter:
            case GenshinModMessageType.SendTalent:
            case GenshinModMessageType.SendArtifact:
                {
                    // Read the player ids from the packet
                    int toPlayer = reader.ReadInt32();
                    int fromPlayer = reader.ReadInt32();

                    // Packet is from another client
                    if (fromPlayer != Main.myPlayer
                        && fromPlayer > -1
                        && fromPlayer < 255)
                    {
                        // Pass this packet to the corresponding player object
                        Main.player[fromPlayer].GetModPlayer<GenshinPlayer>().HandlePacket(reader, msgType, toPlayer, fromPlayer);
                    }
                    
                    // Packet is from server and needs to go to our client
                    else if (Main.netMode == NetmodeID.MultiplayerClient
                        && fromPlayer == 255
                        && toPlayer == Main.myPlayer)
                    {
                        Main.LocalPlayer.GetModPlayer<GenshinPlayer>().HandlePacket(reader, msgType, Main.myPlayer, 255);
                    }
                }
                break;


            case GenshinModMessageType.RequestAllCharacterData:
            case GenshinModMessageType.RequestTeamComposition:
            case GenshinModMessageType.RequestSelectedCharacter:
            case GenshinModMessageType.RequestCharacter:
            case GenshinModMessageType.RequestWeapon:
            case GenshinModMessageType.RequestTalent:
            case GenshinModMessageType.RequestArtifact:
                {
                    // Read the player ids from the packet
                    int toPlayer = reader.ReadInt32();
                    int fromPlayer = reader.ReadInt32();
                    int requestPlayer = reader.ReadInt32();

                    // Request packet received on a multiplayer client
                    if (Main.netMode == NetmodeID.MultiplayerClient)
                    {
                        // The packet is from the server, directed at our client ID
                        if (fromPlayer == 255
                            && (toPlayer == Main.myPlayer || toPlayer == -1)
                            && (requestPlayer == Main.myPlayer || requestPlayer == -1))
                        {
                            // Request came from server, in this case send the packet to all clients
                            Main.LocalPlayer.GetModPlayer<GenshinPlayer>().
                                HandlePacket(reader, msgType, -1, Main.myPlayer, requestPlayer);
                        }

                        // Packet comes from another client and is send to our client ID
                        else if (fromPlayer != Main.myPlayer
                            && fromPlayer > -1
                            && (toPlayer == Main.myPlayer || toPlayer == -1)
                            && (requestPlayer == Main.myPlayer || requestPlayer == -1))
                        {
                            Main.LocalPlayer.GetModPlayer<GenshinPlayer>().
                                HandlePacket(reader, msgType, fromPlayer, Main.myPlayer, requestPlayer);
                        }
                    }

                    // Packet is received on the server and came from a valid client ID
                    else if (Main.netMode == NetmodeID.Server
                        && fromPlayer > -1
                        && fromPlayer < Main.myPlayer)
                    {
                        // Packet is directed at another client, relay the message
                        if (toPlayer != Main.myPlayer)
                        {
                            Main.player[fromPlayer].GetModPlayer<GenshinPlayer>().
                                HandlePacket(reader, msgType, toPlayer, fromPlayer, requestPlayer);
                        }

                        // The packet is directed at server, send the client the requested data.
                        else if (toPlayer == Main.myPlayer
                            && requestPlayer > 0
                            && requestPlayer < Main.myPlayer)
                        {
                            Main.player[requestPlayer].GetModPlayer<GenshinPlayer>().
                                HandlePacket(reader, msgType, toPlayer, fromPlayer, requestPlayer);
                        }

                        // The packet is directed at server, the requested data is -1: all players
                        else if (toPlayer == Main.myPlayer
                            && requestPlayer == -1)
                        {
                            for (int i = 0; i < Main.myPlayer; i++)
                            {
                                if (Main.player[i].active)
                                {
                                    Main.player[i].GetModPlayer<GenshinPlayer>().
                                        HandlePacket(reader, msgType, toPlayer, fromPlayer, requestPlayer);
                                }
                            }
                        }
                    }
                }
                break;

        }
    }
}

public enum GenshinModMessageType : byte
{
    // If genshin mode is enabled on the player
    SendGenshinMode,

    // Used to sync character  and team data:
    // client => server => other client
    SendCharacter,
    SendWeapon,
    SendTalent,
    SendArtifact,
    SendTeamComposition,
    SendSelectedCharacter,

    // Used for verification, if another client detects something invalid.
    // other client => server => client
    RequestAllCharacterData,
    RequestCharacter,
    RequestWeapon,
    RequestTalent,
    RequestArtifact,
    RequestTeamComposition,
    RequestSelectedCharacter,    

    // Other
    OnJoinParty,

    // Custom buff system - TODO
    AddBuff,
    UpdateBuff,
    ClearBuff
}
