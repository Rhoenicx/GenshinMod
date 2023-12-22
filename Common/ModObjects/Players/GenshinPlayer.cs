using GenshinMod.Common.Configs;
using GenshinMod.Common.GameObjects;
using GenshinMod.Common.ModObjects.ModSystems;
using log4net.Repository.Hierarchy;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Terraria;
using Terraria.Chat;
using Terraria.DataStructures;
using Terraria.Enums;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using static Terraria.Player;

namespace GenshinMod.Common.ModObjects.Players
{
    public class GenshinPlayer : ModPlayer
    {
        //------------------------------------------------------------------------------------
        //----------------------------------- Variables --------------------------------------
        //------------------------------------------------------------------------------------
        #region Variables
        public bool GenshinModeEnabled = false;
        private bool _oldGenshinModeEnabled = false;
        private bool _JustJoinedMultiplayer = false;

        // The amount of time you need to wait before switching
        // characters again. Prevents spamming
        public const int SwitchCharacterCooldownTime = 60;
        public int SwitchCharacterCooldown = 0;

        #region TeamBuilding
        // The amount of characters that cen be controlled
        public static int MaxTeamSize
        {
            get => Main.netMode == NetmodeID.SinglePlayer
                ? GenshinMod.GenshinConfigClient.MaxTeamSize
                : GenshinMod.GenshinConfigServer.MaxTeamSize;
        }

        public int MaxControllableCharacters;

        //---------------------------------------------------------------------------------------
        // Public team/character variables used outside this class
        //---------------------------------------------------------------------------------------

        // If the player has a character active (not terrarian)
        public bool CharacterActive => _selectedCharacterID != GenshinCharacterID.Terrarian;

        // The CharacterID of the current character on field.
        public GenshinCharacterID CurrentCharacterID => _selectedCharacterID;

        // The index where the current character is located inside the team composition
        public int CurrentCharacterSlot => _teamComposition.IndexOf(_selectedCharacterID);

        // The current character's instance
        public GenshinCharacter CurrentCharacter => _characters[_selectedCharacterID];

        // The current team composition, read only!
        public IReadOnlyList<GenshinCharacterID> CurrentTeamComposition => _teamComposition.AsReadOnly();

        // All characters owned by this player, read only!
        public IReadOnlyDictionary<GenshinCharacterID, GenshinCharacter> ObtainedCharacters => _characters;

        //---------------------------------------------------------------------------------------
        // Private team/character variables used internally. DO NOT MODIFY OUTSIDE THIS CLASS
        //---------------------------------------------------------------------------------------

        // The CharacterID of the current character on field.
        private GenshinCharacterID _selectedCharacterID;
        private GenshinCharacterID _oldSelectedCharacterID;

        // Whether the character switch is a forced action.
        // For example if the selected character has been removed
        // from the team. In that case we need to skip all switch checks.
        private bool _forcedCharacterSwitch;

        // The dictionary containing all characters this player owns.
        private Dictionary<GenshinCharacterID, GenshinCharacter> _characters = new();

        // The active team composition.
        private List<GenshinCharacterID> _teamComposition = new();
        private List<GenshinCharacterID> _oldTeamComposition = new();

        //---------------------------------------------------------------------------------------
        // Pending variables, used to make character and team changes 
        //---------------------------------------------------------------------------------------

        // Pending team composition => change this to make the team composition change.
        public List<GenshinCharacterID> PendingTeamComposition = new();
        public bool ApplyPendingTeam = false;

        // Pending character for switch
        public GenshinCharacterID PendingCharacter;
        public const int PendingCharacterQueueTime = 60;
        public int PendingCharacterTimer;
        #endregion

        #region Drawing
        public Vector2 HeadOffset;
        public Vector2 BodyOffset;
        public Vector2 LegsOffset;
        public Vector2 BackpackOffset;
        public Vector2 WingsOffset;
        public Vector2 TailOffset;
        #endregion

        #region Developer_Debug
        public static bool TeamDebugger = false;
        #endregion

        #endregion

        //------------------------------------------------------------------------------------
        //---------------------------------- Load + data -------------------------------------
        //------------------------------------------------------------------------------------

        #region Loading
        public override void Load()
        {
            // Initialize data structures
            _characters ??= new();
            _teamComposition ??= new();
            _oldTeamComposition ??= new();
            PendingTeamComposition ??= new();
        }

        public override void Unload()
        {
            // Clear data structures
            _characters = null;
            _teamComposition = null;
            _oldTeamComposition = null;
            PendingTeamComposition = null;
        }

        public override void Initialize()
        {
            // Initialize data structures
            _characters ??= new();
            _teamComposition ??= new();
            _oldTeamComposition ??= new();
            PendingTeamComposition ??= new();

            VerifyTeam();
        }
        #endregion

        #region PlayerData
        // Saves data to this ModPlayer upon leaving a world in SP or MP.
        // Write data that you want to save into the TagCompound.
        public override void SaveData(TagCompound tag)
        {
            // Save Genshin player created status
            tag.Add("GenshinModeEnabled", GenshinModeEnabled);

            // Only run save if the current active player has been made using the mod
            if (!GenshinModeEnabled)
            {
                return;
            }

            // Save the current Character Team
            SaveCharacterDictionaryData(tag, ref _characters, "Characters");

            // Save the active team composition order
            SaveTeamComposition(tag, ref _teamComposition, "ActiveTeamComposition");

            // Save active character slot
            tag.Add("SelectedCharacter", _selectedCharacterID.ToString());
        }

        private static void SaveCharacterDictionaryData(TagCompound tag, ref Dictionary<GenshinCharacterID, GenshinCharacter> dictionary, string tagName)
        {
            // Save characters insdie this dictionary
            TagCompound collectionTag = new();
            List<string> characterList = new();

            // Loop over all the character currently in the player's team
            foreach (KeyValuePair<GenshinCharacterID, GenshinCharacter> kvpCharacter in dictionary)
            {
                // Adds the character to the List of characters
                characterList.Add(kvpCharacter.Key.ToString());

                // Create a new TagCompound for each character
                // to store its data
                TagCompound characterTag = new();

                // Run SaveData() hook of the GenshinCharacter object,
                // passes the TagCompound in which the data must be stored
                kvpCharacter.Value.SaveData(characterTag);

                // Protection against duplicate keys inside the TeamTag compound
                if (collectionTag.ContainsKey(kvpCharacter.Key.ToString()))
                {
                    // Duplicate: Overwrite the existing Tag and continue
                    collectionTag[kvpCharacter.Key.ToString()] = characterTag;
                    continue;
                }

                // Add the Character Tag to the Team Tag
                collectionTag.Add(kvpCharacter.Key.ToString(), characterTag);
            }
            // Add the list to the team compound
            collectionTag.Add("CharacterList", characterList);

            // Add the team compound to the base Tag
            tag.Add(tagName, collectionTag);
        }

        private static void SaveTeamComposition(TagCompound tag, ref List<GenshinCharacterID> list, string tagName)
        {
            // Create a new TagCompound
            TagCompound teamCompositionTag = new();

            // Check if the TeamComposition TagCompound is already present
            if (tag.ContainsKey("TeamComposition"))
            {
                // If yes grab that one instead
                teamCompositionTag = tag.GetCompound("TeamComposition");
            }

            // Create the list of Character names of this team composition
            List<string> teamCompositionList = new();

            // Loop throught the list of CharacterIDs
            for (int i = 0; i < list.Count; i++)
            {
                // Convert the Enum id to a string
                string characterName = list[i].ToString();

                // Add the CharacterName to the List
                if (!teamCompositionList.Contains(characterName))
                {
                    teamCompositionList.Add(characterName);
                }
            }

            // Check if the tagName already exist in the TeamComposition Tag
            if (teamCompositionTag.ContainsKey(tagName))
            {
                // If yes overwrite it
                teamCompositionTag.Remove(tagName);
            }

            teamCompositionTag.Add(tagName, teamCompositionList);

            // Check if the TeamComposition Tag already exist in the base tag
            if (tag.ContainsKey("TeamComposition"))
            {
                // If yes overwrite it
                tag.Remove("TeamComposition");
            }

            tag.Add("TeamComposition", teamCompositionTag);
        }

        public override void LoadData(TagCompound tag)
        {
            // Load Genshin player created status
            if (tag.ContainsKey("GenshinModeEnabled"))
            {
                GenshinModeEnabled = tag.GetBool("GenshinModeEnabled");
            }

            // Load the obtained characters
            LoadCharacterDictionaryData(tag, ref _characters, "Characters");

            // Load the active team composition order
            LoadTeamComposition(tag, ref _teamComposition, "ActiveTeamComposition");

            // Load active character slot
            if (tag.ContainsKey("SelectedCharacter")
                && Enum.TryParse(typeof(GenshinCharacterID), tag.GetString("SelectedCharacter"), out object id))
            {
                _selectedCharacterID = (GenshinCharacterID)id;
            }

            // Verify the integrity of the team
            VerifyTeam();
        }

        private static void LoadCharacterDictionaryData(TagCompound tag, ref Dictionary<GenshinCharacterID, GenshinCharacter> dictionary, string tagName)
        {
            // Try to find the TagCompound "CharacterTeam",
            // if it is present, continue
            if (!tag.ContainsKey(tagName))
            {
                return;
            }

            // Grab the entire Tag "CharacterTeam" from the base tag
            TagCompound characterTeamTag = tag.GetCompound(tagName);

            // Check if the tag "CharacterList" is present in the "CharacterTeam" Tagcompound
            if (!characterTeamTag.ContainsKey("CharacterList"))
            {
                return;
            }

            // Read the Character List from the Tag
            List<string> characterList = characterTeamTag.GetList<string>("CharacterList").ToList();

            // Loop throught the Character List.
            for (int i = 0; i < characterList.Count; i++)
            {
                // Read the CharacterName from the List at index i
                string characterName = characterList[i];

                // Try to parse the character's name against the available 
                // Characters in the CharacterID Enumerator. Then check
                // if we need to add this character to the team.
                if (Enum.TryParse(typeof(GenshinCharacterID), characterName, out object id)
                    && !dictionary.ContainsKey((GenshinCharacterID)id)
                    && characterTeamTag.ContainsKey(characterName))
                {
                    // We succesfully found the Character Tag => Read it from the Team Tag
                    TagCompound characterTag = characterTeamTag.GetCompound(characterName);

                    // Create a new instance of the character object based on the data read
                    GenshinCharacter loadedCharacter = GenshinCharacterSystem.GetGenshinCharacter((GenshinCharacterID)id, Main.LocalPlayer);

                    // Run the LoadData() hook of this character
                    loadedCharacter.LoadData(characterTag);

                    // Add the character to the team.
                    if (dictionary.ContainsKey((GenshinCharacterID)id))
                    {
                        dictionary[(GenshinCharacterID)id] = loadedCharacter;
                        continue;
                    }

                    dictionary.Add((GenshinCharacterID)id, loadedCharacter);
                }
            }
        }

        private static void LoadTeamComposition(TagCompound tag, ref List<GenshinCharacterID> list, string tagName)
        {
            // Check if the TeamComposition tag is present in the base tag
            if (!tag.ContainsKey("TeamComposition"))
            {
                return;
            }

            // Grab the TeamComposition tag from the base tag
            TagCompound teamCompositionTag = tag.GetCompound("TeamComposition");

            // Check if the ActiveTeamComposition tag is present in the TeamComposition tag
            if (!teamCompositionTag.ContainsKey(tagName))
            {
                return;
            }

            // Grab the ActiveTeamComposition list from the TeamComposition tag
            List<string> teamCompostionList = teamCompositionTag.GetList<string>(tagName).ToList();

            // Clear the existing TeamComposition List from this player
            list.Clear();

            // Loop throught the list to add the CharacterIDs in order
            for (int i = 0; i < teamCompostionList.Count; i++)
            {
                // Get the name of the current entry
                string characterName = teamCompostionList[i];

                // Verify the CharacterName of the List against the CharacterID Enumerator
                // Also check if we do not add an character multiple times.
                if (Enum.TryParse(typeof(GenshinCharacterID), characterName, out object id)
                    && !list.Contains((GenshinCharacterID)id))
                {
                    // Add the id to the current TeamComposition
                    list.Add((GenshinCharacterID)id);
                }
            }
        }
        #endregion

        //------------------------------------------------------------------------------------
        //---------------------------- ModPlayer Enter World ---------------------------------
        //------------------------------------------------------------------------------------
        #region Enter_World
        public override void OnEnterWorld()
        {
            // Flip this bool when entering a world; player ignored warnings etc
            if (!GenshinModeEnabled)
            {
                GenshinModeEnabled = true;
            }

            // Write the player's created status to the config
            GenshinMod.GenshinConfigClient.GenshinModeEnabled = GenshinModeEnabled;

            // Verify the integrity of the team
            VerifyTeam();

            // Run the OnEnterWorld() hook on all characters
            // Fixes wrong player assignments on characters
            foreach (GenshinCharacterID id in _characters.Keys)
            {
                _characters[id].OnEnterWorld(Player);
            }

            // When joining multiplayer: inform the server of our data
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                // Send all our data to the server
                SendAllCharacterData(-1, Main.myPlayer);

                // Also request all data from all other clients connected
                RequestAllCharacterData(-1, Main.myPlayer, -1);

                // Also fill the _old variables
                _oldTeamComposition = _teamComposition.ToList();
                _oldSelectedCharacterID = _selectedCharacterID;
            }
        }

        /// <summary>
        /// This hook runs on the connecting client's player object in multiplayer.
        /// From testing the Player object has not been set up entirely, things
        /// like Player.whoAmI are not populated yet. Use to initialize
        /// variables and data structures on a connecting player object.
        /// </summary>
        public override void PlayerConnect()
        {
            // Verify the team, and add base characters if empty
            VerifyTeam(true);
            _JustJoinedMultiplayer = true;
        }
        #endregion

        //------------------------------------------------------------------------------------
        //------------------------------- ModPlayer Drawing ----------------------------------
        //------------------------------------------------------------------------------------
        #region Modplayer_drawing

        public override void ModifyDrawInfo(ref PlayerDrawSet drawInfo)
        {
            // Run the ModifyDrawInfo hook for the current active character
            if (GenshinPlayerSystem.GenshinCharacterActive(ref drawInfo))
            {
                CurrentCharacter.ModifyDrawInfo(ref drawInfo);
            }
        }

        public override void DrawEffects(PlayerDrawSet drawInfo, ref float r, ref float g, ref float b, ref float a, ref bool fullBright)
        {
            // Run the DrawEffects hook for the current active character
            if (GenshinPlayerSystem.GenshinCharacterActive(ref drawInfo))
            {
                //CurrentCharacter.DrawEffects(ref drawInfo);
            }
        }

        #endregion

        //------------------------------------------------------------------------------------
        //------------------------------- ModPlayer Update -----------------------------------
        //------------------------------------------------------------------------------------
        #region ModPlayer_Update
        // Hooks in order of execution in vanilla code:
        public override void PreUpdate()
        {
            // Calculate the amount of characters we may control
            MaxControllableCharacters = GetControllableCharacterAmount();

            // Run Pending team
            ApplyPendingTeamComposition();

            // Count down the switch cooldown timer
            if (SwitchCharacterCooldown > 0)
            {
                SwitchCharacterCooldown--;
            }

            // Pending character switch
            if (PendingCharacterTimer > 0)
            {
                SwitchCharacter(PendingCharacter);

                PendingCharacterTimer--;
            }

            // When the amount of controllable characters changes because of 
            // another player that joined multiplayer or the config was changed,
            // swap the characters in the team
            if (Player.whoAmI == Main.myPlayer && CurrentCharacterSlot >= MaxControllableCharacters)
            {
                _teamComposition.Swap(0, CurrentCharacterSlot);
            }

            // Check if the characters in the team exceeds the maximum team size
            if (_teamComposition.Count > MaxTeamSize)
            {
                _teamComposition.RemoveRange(MaxTeamSize, _teamComposition.Count - MaxTeamSize);

                // Fail-safe
                if (!_teamComposition.Contains(CurrentCharacterID))
                {
                    _selectedCharacterID = _teamComposition[0];
                }
            }

            // Execute the Disable() hook on characters that are not controllable.
            for (int i = MaxControllableCharacters; i < _teamComposition.Count; i++)
            {
                _characters[_teamComposition[i]].Disable();
            }

            // Execute PreUpdate() hook on all character that can be controlled
            for (int i = 0; i < _teamComposition.Count && i < MaxControllableCharacters; i++)
            {
                _characters[_teamComposition[i]].PreUpdate();
            }

            // print a debug message in chat about the team
            if (TeamDebugger)
            {
                string debug = Player.name + " - " + MaxControllableCharacters + " - " + _selectedCharacterID.ToString() + " ||| ";
                for (int i = 0; i < _teamComposition.Count; i++)
                {
                    debug += _teamComposition[i].ToString();
                    if (i < _teamComposition.Count - 1)
                        debug += " - ";
                }
                debug += " ||| " + _characters.Count;
                Main.NewText(debug);
            }
        }

        public override void UpdateDead()
        {

        }

        public override void ProcessTriggers(TriggersSet triggersSet)
        {

        }

        public override void SetControls()
        {
        }

        public override void ResetEffects()
        {
            // Call ResetEffects on all controllable characters in the team
            for (int i = 0; i < _teamComposition.Count && i < MaxControllableCharacters; i++)
            {
                _characters[_teamComposition[i]].ResetEffects();
            }
        }

        public override void UpdateDyes()
        {

        }

        public override void PreUpdateBuffs()
        {

        }

        public override void PostUpdateBuffs()
        {
        }

        public override void UpdateEquips()
        {

        }

        public override void PostUpdateEquips()
        {

        }

        public override void PostUpdateMiscEffects()
        {

        }

        public override void UpdateBadLifeRegen()
        {

        }

        public override void UpdateLifeRegen()
        {

        }

        public override void PostUpdateRunSpeeds()
        {

        }

        public override void PreUpdateMovement()
        {

        }

        public override bool PreItemCheck()
        {
            return base.PreItemCheck();
        }

        public override bool? CanAutoReuseItem(Item item)
        {
            return base.CanAutoReuseItem(item);
        }

        public override bool CanUseItem(Item item)
        {
            return base.CanUseItem(item);
        }

        public override void PostItemCheck()
        {

        }

        public override void FrameEffects()
        {

        }

        public override void PostUpdate()
        {

        }

        #endregion

        //------------------------------------------------------------------------------------
        //-------------------------------- ModPlayer Hits ------------------------------------
        //------------------------------------------------------------------------------------
        #region ModPlayer_Hits

        #endregion

        //------------------------------------------------------------------------------------
        //-------------------------- Character team functions --------------------------------
        //------------------------------------------------------------------------------------
        #region Character_team

        /// <summary>
        /// Verifies all the team related variables. If there are problems with it tries to fix them.
        /// Should only run after loading player data or on another player's object in multiplayer
        /// upon connecting to the server.
        /// </summary>
        private void VerifyTeam(bool otherPlayer = false)
        {
            // Check if the Character dictionary has at least 1 character
            if (!_characters.Any())
            {
                AddStartingCharacter(otherPlayer);
            }

            // Remove duplicate entries from CharacterTeam
            _teamComposition = _teamComposition.Distinct().ToList();

            // Verify the length of the _teamComposition, this cannot exceed MaxTeamSize
            if (_teamComposition.Count > MaxTeamSize)
            {
                _teamComposition.RemoveRange(MaxTeamSize, _teamComposition.Count - MaxTeamSize);
            }

            // Verify if the characters in the _teamComposition are contained in Characters.
            for (int i = _teamComposition.Count - 1; i >= 0; i--)
            {
                // If an entry cannot be found, remove this character id from the team
                if (!_characters.ContainsKey(_teamComposition[i]))
                {
                    _teamComposition.RemoveAt(i);
                }
            }

            //// Change the team depending on the GenshinMode of the player
            //if (GenshinModeEnabled)
            //{
            //    // The setting is enabled.
            //    // Remove the Terrarian from the characters.
            //    _characters.Remove(GenshinCharacterID.Terrarian);
            //    _teamComposition.RemoveAll(x => x == GenshinCharacterID.Terrarian);
            //}
            //else
            //{
            //    // The setting is disabled.
            //    // Clear the team and only allow the Terrarian
            //    _teamComposition.Clear();
            //    _teamComposition.Add(GenshinCharacterID.Terrarian);
            //}

            // Verify if the current team composition is empty => need at least 1 character always.
            if (!_teamComposition.Any())
            {
                // Fall back to Lumine or Aether, these should always be present in Characters because of the code above.
                if (!otherPlayer && GenshinModeEnabled)
                {
                    _teamComposition.Add(_characters.First(c => c.Key == GenshinCharacterID.Lumine || c.Key == GenshinCharacterID.Aether).Key);
                }
                else
                {
                    _teamComposition.Add(_characters.First(c => c.Key == GenshinCharacterID.Terrarian).Key);
                }
            }

            // Verify if the selected Character is contained in the _teamComposition
            if (!_teamComposition.Contains(_selectedCharacterID))
            {
                _selectedCharacterID = _teamComposition[0];
            }
        }

        /// <summary>
        /// Adds a starting character to the player's obtained characters. Used directly
        /// after loading the player; we can't have empty dictionaries/lists.
        /// Will add Lumine or Aether based on the player's gender.
        /// </summary>
        private void AddStartingCharacter(bool otherPlayer = false)
        {
            // For our own player add Lumine or Aether based on the players gender.
            if (!otherPlayer
                && GenshinModeEnabled
                && !(_characters.ContainsKey(GenshinCharacterID.Lumine)
                || _characters.ContainsKey(GenshinCharacterID.Aether)))
            {
                GenshinCharacterID id = Player.Male ? GenshinCharacterID.Aether : GenshinCharacterID.Lumine;
                _characters.TryAdd(id, GenshinCharacterSystem.GetGenshinCharacter(id, Player));
            }

            // When connecting in multiplayer our client runs VerifyTeam() for all players,
            // in the first couple of gameticks we'll not have data from the other players
            // yet => Fall back to an empty / dummy character as fail-safe.
            else
            {
                _characters.TryAdd(GenshinCharacterID.Terrarian, GenshinCharacterSystem.GetGenshinCharacter(GenshinCharacterID.Terrarian, Player));
            }
        }

        /// <summary>
        /// Try to make the player switch character to 
        /// the given slot. If the checks fail disallow
        /// the switch. This runs only on owner clients!
        /// </summary>
        public bool SwitchCharacter(GenshinCharacterID id)
        {
            // Check whether the character we want to switch to is valid.
            if (Main.myPlayer == Player.whoAmI
                && !_forcedCharacterSwitch
                && (!_teamComposition.Contains(id)
                || !_characters.ContainsKey(id)
                || _characters[id] == null
                || _teamComposition.IndexOf(id) >= MaxControllableCharacters))
            {
                return false;
            }

            if (!_forcedCharacterSwitch
                && (_selectedCharacterID == id
                || _characters[id].IsDummyCharacter
                || !_characters[id].IsAlive
                || _characters[id].CharacterID != id))
            {
                return false;
            }

            // Additional client only logic
            if (Main.myPlayer == Player.whoAmI
                && !_forcedCharacterSwitch)
            {
                // Character switching is on cooldown
                if (SwitchCharacterCooldown > 0
                    || !CanSwitchCharacter())
                {
                    return false;
                }

                // If another character slot is requested
                // during a pending change => update
                if (PendingCharacterTimer > 0
                    && PendingCharacter != id)
                {
                    PendingCharacter = id;
                    PendingCharacterTimer = PendingCharacterQueueTime;
                }

                // When the current character stays in use
                if (PlayerInUse() || CurrentCharacter.InUse())
                {
                    // Current character is in use,
                    // queue the character switch
                    // as long as it remains available
                    if (PendingCharacterTimer <= 0)
                    {
                        PendingCharacter = id;
                        PendingCharacterTimer = PendingCharacterQueueTime;
                    }

                    return false;
                }
            }

            // In multiplayer characters get removed from _characters when
            // a new team composition is send, since players can't have
            // empty character variables this can create a situation
            // where the old 

            // Additional check for multiplayer
            if (!_forcedCharacterSwitch
                && _characters.ContainsKey(id)
                && _teamComposition.Contains(id)
                && CurrentCharacterSlot < MaxControllableCharacters)
            {
                // Make the old character leave the field
                CurrentCharacter.OnLeaveFieldSwitch(id);
                CurrentCharacter.OnLeaveField();

                GenshinCharacterID oldid = _selectedCharacterID;

                // Switch the active slot
                _selectedCharacterID = id;

                // Make the new character join the field
                CurrentCharacter.OnJoinFieldSwitch(oldid);
            }

            // Change selected character and run OnJoinField()
            _selectedCharacterID = id;

            // In multiplayer prevent running OnJoinField() hook on 
            // players/characters that just joined the MP server/instance.
            if (!_JustJoinedMultiplayer)
            {
                CurrentCharacter.OnJoinField();
            }

            // Only owner client from here
            if (Player.whoAmI == Main.myPlayer)
            {
                // Apply cooldown
                SwitchCharacterCooldown = SwitchCharacterCooldownTime;

                // Reset Pending timer
                PendingCharacterTimer = 0;
            }

            // Make the forced variable low, but not in MP on our own client.
            if (Main.netMode != NetmodeID.MultiplayerClient
                || Player.whoAmI != Main.myPlayer)
            {
                _forcedCharacterSwitch = false;
            }

            // Character switch successfully executed!
            return true;
        }

        /// <summary>
        /// Determines if the player is currently using the current character.
        /// If yes, it prevents the character switch. Return a boolean.
        /// </summary>
        private bool PlayerInUse()
        {
            return Player.ItemAnimationActive
                || Player.ItemAnimationJustStarted
                || !Player.mount.Active && Player.velocity.Y != 0f
                || Player.channel
                || Player.heldProj != -1;
        }

        /// <summary>
        /// Detemines if the player is allowed to switch character. Can be
        /// used to block character switches in certain challenges.
        /// </summary>
        public bool CanSwitchCharacter()
        {
            // Example:
            // return ChallangeActive;
            return true;
        }

        /// <summary>
        /// Determines if the character is allowed to change the team
        /// composition. Can be used to block edits to the current team.
        /// </summary>
        /// <returns></returns>
        public bool CanEditTeam()
        {
            // Example:
            // return ChallangeActive;
            return true;
        }

        /// <summary>
        /// Calculates how many characters this player may control
        /// in multiplayer. Lower player.whoAmI gets more slots.
        /// Returns int with amount of characters
        /// </summary>
        public int GetControllableCharacterAmount()
        {
            // Running in singleplayer, here we are always allowed
            // to control the maximum amount
            if (Main.netMode == NetmodeID.SinglePlayer)
            {
                return MaxTeamSize;
            }

            // In multiplayer count the amount of players active
            int myPlayerNumber = -1;
            int activePlayerAmount = 0;

            foreach (Player plr in Main.player)
            {
                // Found an active player
                if (plr.active)
                {
                    activePlayerAmount++;

                    // Found our own player id
                    if (plr.whoAmI == Player.whoAmI)
                    {
                        myPlayerNumber = activePlayerAmount;
                    }
                }
            }

            // We have no active players in this session
            if (activePlayerAmount <= 0
                || myPlayerNumber == -1
                || myPlayerNumber > activePlayerAmount)
            {
                return MaxTeamSize;
            }

            // Now determine how many characters we may control
            int charactersPerPlayer = MaxTeamSize / activePlayerAmount;
            int remainingCharacters = MaxTeamSize % activePlayerAmount;

            // Cannot evenly distribute the amount of characters,
            // give players with lower ID's an additional character
            if (myPlayerNumber <= remainingCharacters)
            {
                charactersPerPlayer += 1;
            }

            // The amount of characters of this player is lower
            // then the configured minimum amount.
            if (charactersPerPlayer < GenshinMod.GenshinConfigServer.MinCharactersPerPlayer)
            {
                charactersPerPlayer = GenshinMod.GenshinConfigServer.MinCharactersPerPlayer;
            }

            // The amount of characters of this player exceeds
            // the maximum amount of characters that are 
            // allowed in a player's team.
            if (charactersPerPlayer > MaxTeamSize)
            {
                charactersPerPlayer = MaxTeamSize;
            }

            return charactersPerPlayer;
        }

        /// <summary>
        /// Tries to apply the pending team composition chosen in
        /// the TeamBuilding UI. Runs when ApplyPendingTeam bool
        /// is set to true.
        /// </summary>
        public void ApplyPendingTeamComposition()
        {
            if (!ApplyPendingTeam)
            {
                return;
            }

            // Makes sure there are no duplicates in the pending team and active team
            PendingTeamComposition = PendingTeamComposition.Distinct().ToList();
            _teamComposition = _teamComposition.Distinct().ToList();

            // Trim the PendingTeamComposition - just in case
            if (PendingTeamComposition.Count > MaxTeamSize)
            {
                PendingTeamComposition.RemoveRange(MaxTeamSize, PendingTeamComposition.Count - MaxTeamSize);
            }

            // Team is the same - we don't have to change anything
            if (_teamComposition.SequenceEqual(PendingTeamComposition)
                || !PendingTeamComposition.Any())
            {
                ApplyPendingTeam = false;
                return;
            }

            // Verify if the chosen characters are available in the _character dictionary
            // if not remove them from the pending team composition.
            for (int i = PendingTeamComposition.Count - 1; i >= 0; i--)
            {
                if (!_characters.ContainsKey(PendingTeamComposition[i])
                    || _characters[PendingTeamComposition[i]] == null)
                {
                    PendingTeamComposition.RemoveAt(i);

                    if (_characters.ContainsKey(PendingTeamComposition[i]))
                    {
                        _characters.Remove(PendingTeamComposition[i]);
                    }
                }
            }

            // Pending team must have at least one character
            if (!PendingTeamComposition.Any())
            {
                ApplyPendingTeam = false;
                return;
            }

            // --------------------------------
            // Checks completed, begin applying the new team

            // Get the slot number of the current selected character
            int slot = _teamComposition.IndexOf(_selectedCharacterID);

            // Check differences between teams to run Party join and leave hooks
            CheckForTeamChanges(_teamComposition, PendingTeamComposition);

            // Apply the pending team composition
            _teamComposition = PendingTeamComposition;

            // If the new team composition does not contain the
            // current selected character, use slot number closest
            // to the previously selected slot.
            if (!_teamComposition.Contains(_selectedCharacterID))
            {
                // Verify the slot of the previous selected character
                if (slot >= 0)
                {
                    // Reverse loop over the new _teamComposition until
                    // we find a filled slot
                    for (int i = slot; i >= 0; i--)
                    {
                        // Filled slot found
                        if (i < _teamComposition.Count)
                        {
                            // Assign selected character
                            _forcedCharacterSwitch = true;
                            SwitchCharacter(_teamComposition[i]);
                            break;
                        }
                    }
                }
                else
                {
                    // fail-safe, assign first
                    _forcedCharacterSwitch = true;
                    SwitchCharacter(_teamComposition.First());
                }
            }

            // Done!
            ApplyPendingTeam = false;
        }

        private void CheckForTeamChanges(List<GenshinCharacterID> currentTeam, List<GenshinCharacterID> pendingTeam)
        {
            // If there are no changes to the team => return
            if (_JustJoinedMultiplayer || currentTeam.SequenceEqual(pendingTeam))
            {
                return;
            }

            // Get the changes between old and new team composition
            List<GenshinCharacterID> changes = currentTeam.Except(pendingTeam).ToList();

            // Loop throught the changes
            foreach (GenshinCharacterID id in changes)
            {
                // Character left the team
                if (currentTeam.Contains(id))
                {
                    _characters[id].OnLeaveTeam();
                }

                // Character joined the team
                if (pendingTeam.Contains(id))
                {
                    _characters[id].OnJoinTeam();
                }
            }
        }

        /// <summary>
        /// Use this hook to add a new character to the player's obtained characters.
        /// If the player already owns this character, add a constellation point.
        /// If it is the first time the player obtains this character, add it to the team.
        /// </summary>
        public void AddCharacter(GenshinCharacterID id)
        {
            // check if the player already owns this character
            if (_characters.ContainsKey(id))
            {
                if (Player.whoAmI == Main.myPlayer)
                {
                    // Player owns the added character => add constellation point
                    _characters[id].AddConstellationPoint();
                }
                return;
            }

            // Player does not own this character, add it
            _characters.Add(id, GenshinCharacterSystem.GetGenshinCharacter(id, Player));

            // If there is a free slot in the player's team composition, add it there as well. 
            // (only if this client is the owner)
            if (Player.whoAmI == Main.myPlayer
                && _teamComposition.Count < MaxTeamSize
                && !_teamComposition.Contains(id))
            {
                _teamComposition.Add(id);
            }
        }

        /// <summary>
        /// Use this hook to permanently remove a character from the player's obtained
        /// characters. Only runs through cheats on own player, or in multiplayer
        /// when this player belongs to another client.
        /// Also removes the Character from the Team and resets selected character.
        /// </summary>
        public void RemoveCharacter(GenshinCharacterID id)
        {
            // Run leave party on owner player before removing
            if (Player.whoAmI == Main.myPlayer
                && _characters.Count > 1)
            {
                _characters[id].OnLeaveTeam();
            }

            // Check if this player owns the given character id
            // We cannot remove Lumine or Aether, from lore POV
            // and code-wise; removing all character breaks EVERYTHING!
            if (_characters.ContainsKey(id)
                && (id != GenshinCharacterID.Lumine
                && id != GenshinCharacterID.Aether
                || _characters.ContainsKey(GenshinCharacterID.Lumine)
                && _characters.ContainsKey(GenshinCharacterID.Aether)
                || Player.whoAmI != Main.myPlayer))
            {
                _characters.Remove(id);
            }

            // Remove the character from the Team Composition
            _teamComposition.RemoveAll(i => i == id);

            // Check if there is at least one character in the team
            if (!_teamComposition.Any())
            {
                // If not, grab someone from the obtained characters
                // Preferably not the character that currently needs to
                // be removed (in case of Lumine or Aether)
                if (Player.whoAmI == Main.myPlayer
                    && _characters.Count > 1
                    && _characters.Any(kvp => kvp.Key != id))
                {
                    _teamComposition.Add(_characters.First(kvp => kvp.Key != id).Key);
                }
                else
                {
                    _teamComposition.Add(_characters.First().Key);
                }

                _selectedCharacterID = _teamComposition.First();

                // Run OnJoinTeam for the fall-back character that is
                // added to the team.
                if (Player.whoAmI == Main.myPlayer)
                {
                    _characters[_selectedCharacterID].OnJoinTeam();
                }

                // On our own client when we're not changing to the same
                // character, run OnJoinField() hook. In multiplayer
                // this hook already runs on other clients when they receive
                // the updated team composition.
                if (Player.whoAmI == Main.myPlayer && _selectedCharacterID != id)
                {
                    _forcedCharacterSwitch = true;
                    SwitchCharacter(_selectedCharacterID);
                }
                return;
            }

            // The removed character was the selected character:
            if (Player.whoAmI == Main.myPlayer && _selectedCharacterID == id)
            {
                // fail-safe: set to the first character in the team
                _forcedCharacterSwitch = true;
                SwitchCharacter(_teamComposition.First());
            }
        }
        #endregion

        //------------------------------------------------------------------------------------
        //---------------------------- Multiplayer functions ---------------------------------
        //------------------------------------------------------------------------------------
        #region Multiplayer_functions

        /// <summary>
        /// Use this hook to detect changes on this player. Compare variables that
        /// need to be send in multiplayer if they've updated.
        /// => Only gets called on the client owning this player.
        /// => Runs just before CopyClientState() hook.
        /// </summary>
        public override void SendClientChanges(ModPlayer clientPlayer)
        {
            // return if not the right player
            if (Player.whoAmI != Main.myPlayer)
            {
                return;
            }

            // The genshin mode setting is changed in-game
            if (GenshinModeEnabled != _oldGenshinModeEnabled)
            {
                SendGenshinMode(-1, Main.myPlayer);
            }

            // If the team composition changed, send packet of team and possibliy new characters
            if (!_oldTeamComposition.SequenceEqual(_teamComposition))
            {
                List<GenshinCharacterID> changes = _teamComposition.Except(_oldTeamComposition).ToList();
                foreach (GenshinCharacterID id in changes)
                {
                    if (_teamComposition.Contains(id))
                    {
                        SendCharacter(id, -1, Main.myPlayer);
                    }
                }

                SendTeamComposition(-1, Main.myPlayer);
            }

            // If the selected character is changed => send an update
            if (_oldSelectedCharacterID != _selectedCharacterID)
            {
                SendSelectedCharacter(-1, Main.myPlayer, _forcedCharacterSwitch);
            }

            // In MP make the forced switch variable low on our client
            _forcedCharacterSwitch = false;
        }

        /// <summary>
        /// Use this hook to copy data to another location so it can be compared
        /// against changed variables in the next game tick.
        /// => Only gets called on the client owning this player.
        /// => Runs after SendClientChanges() hook.
        /// </summary>
        public override void CopyClientState(ModPlayer targetCopy)
        {
            // Save the current genshin mode status
            _oldGenshinModeEnabled = GenshinModeEnabled;

            // return if not the right player
            if (Player.whoAmI != Main.myPlayer)
            {
                return;
            }

            // Save the team composition
            _oldTeamComposition = _teamComposition.ToList();

            // Save the selected character
            _oldSelectedCharacterID = _selectedCharacterID;
        }

        /// <summary>
        /// This hook is called when another player connects multiplayer,
        /// the connecting player will not have our data yet, so sync it.
        /// </summary>
        private void SendAllCharacterData(int toPlayer = -1, int fromPlayer = -1)
        {
            // Send genshin mode config setting of the player
            SendGenshinMode(toPlayer, fromPlayer);

            // Send all the characters of this player
            foreach (GenshinCharacterID id in _teamComposition)
            {
                SendCharacter(id, toPlayer, fromPlayer);
            }

            // Send the current team composition
            SendTeamComposition(toPlayer, fromPlayer);

            // Send the selected character
            SendSelectedCharacter(toPlayer, fromPlayer, true);
        }

        private void SendGenshinMode(int toPlayer = -1, int fromPlayer = -1)
        {
            // Create a request packet and send it.
            ModPacket packet = GenshinMod.Instance.GetPacket();
            packet.Write((byte)GenshinModMessageType.SendGenshinMode);
            packet.Write(toPlayer);
            packet.Write(fromPlayer);
            packet.Write(GenshinModeEnabled);
            packet.Send(toPlayer, fromPlayer);
        }

        /// <summary>
        /// This hook is used to send a request to all other clients/server
        /// to synchronize their characters. Gets called by a joining
        /// client in multiplayer (During OnEnterWorld() hook).
        /// </summary>
        private void RequestAllCharacterData(int toPlayer = -1, int fromPlayer = -1, int requestPlayer = -1)
        {
            // Create a request packet and send it.
            ModPacket packet = GenshinMod.Instance.GetPacket();
            packet.Write((byte)GenshinModMessageType.RequestAllCharacterData);
            packet.Write(toPlayer);
            packet.Write(fromPlayer);
            packet.Write(requestPlayer);
            packet.Send(toPlayer, fromPlayer);
        }

        /// <summary>
        /// This hook takes care of sending all the player's data in multiplayer.
        /// Keep in mind this does not account for weapons, talents or artifacts;
        /// those need to be manually synched inside the character objects after
        /// this hook has finished execution.
        /// </summary>
        private void SendCharacter(GenshinCharacterID id, int toPlayer = -1, int fromPlayer = -1)
        {
            // Check if the requested character to send is present
            // in the dictionary. otherwise we cannot send it's data.
            if (!_characters.ContainsKey(id)
                || _characters[id] == null
                || _characters[id].CharacterID != id)
            {
                return;
            }

            // Call the SendCharacter() hook on the character's instance.
            _characters[id].SendCharacter(toPlayer, fromPlayer, true);
        }

        /// <summary>
        /// This hook sends the request packet for a character sync in multiplayer.
        /// </summary>
        private void RequestCharacter(GenshinCharacterID id, int toPlayer = -1, int fromPlayer = -1, int requestPlayer = -1)
        {
            // Create a request packet and send it.
            ModPacket packet = GenshinMod.Instance.GetPacket();
            packet.Write((byte)GenshinModMessageType.RequestCharacter);
            packet.Write(toPlayer);
            packet.Write(fromPlayer);
            packet.Write(requestPlayer);
            packet.Write((int)id);
            packet.Send(toPlayer, fromPlayer);
        }

        /// <summary>
        /// This hook sends the packet for the current team composition in multiplayer.
        /// </summary>
        private void SendTeamComposition(int toPlayer = -1, int fromPlayer = -1)
        {
            // Checks if the team composition contains values
            if (!_teamComposition.Any())
            {
                return;
            }

            // Prepare and send the packet
            ModPacket packet = Mod.GetPacket();
            packet.Write((byte)GenshinModMessageType.SendTeamComposition);
            packet.Write(toPlayer);
            packet.Write(fromPlayer);
            packet.Write(_teamComposition.Count);
            for (int i = 0; i < _teamComposition.Count; i++)
            {
                packet.Write((int)_teamComposition[i]);
            }
            packet.Send(toPlayer, fromPlayer);
        }

        /// <summary>
        /// This hook sends the request packet for the team composition in multiplayer.
        /// </summary>
        private void RequestTeamComposition(int toPlayer = -1, int fromPlayer = -1, int requestPlayer = -1)
        {
            // Create a request packet and send it.
            ModPacket packet = GenshinMod.Instance.GetPacket();
            packet.Write((byte)GenshinModMessageType.RequestTeamComposition);
            packet.Write(toPlayer);
            packet.Write(fromPlayer);
            packet.Write(requestPlayer);
            packet.Send(toPlayer, fromPlayer);
        }

        /// <summary>
        /// This hook sends the packet for changing the selected character in multiplayer.
        /// The forced boolean determines whether this is a forced character switch.
        /// </summary>
        private void SendSelectedCharacter(int toPlayer = -1, int fromPlayer = -1, bool forced = false)
        {
            if (!_teamComposition.Contains(_selectedCharacterID)
                || !_characters.ContainsKey(_selectedCharacterID))
            {
                return;
            }

            // Prepare and send the packet
            ModPacket packet = Mod.GetPacket();
            packet.Write((byte)GenshinModMessageType.SendSelectedCharacter);
            packet.Write(toPlayer);
            packet.Write(fromPlayer);
            packet.Write((int)_selectedCharacterID);
            packet.Write(forced);
            packet.Send(toPlayer, fromPlayer);
        }

        /// <summary>
        /// This hook sends the request packet for the current selected character in multiplayer.
        /// </summary>
        private void RequestSelectedCharacter(int toPlayer = -1, int fromPlayer = -1, int requestPlayer = -1)
        {
            // Create a request packet and send it.
            ModPacket packet = GenshinMod.Instance.GetPacket();
            packet.Write((byte)GenshinModMessageType.RequestSelectedCharacter);
            packet.Write(toPlayer);
            packet.Write(fromPlayer);
            packet.Write(requestPlayer);
            packet.Send(toPlayer, fromPlayer);
        }

        /// <summary>
        /// Handlepacket of this player instance. Received packet which are designated
        /// for this player's id end up here.
        /// </summary>
        public void HandlePacket(BinaryReader reader, GenshinModMessageType msgType, int toPlayer, int fromPlayer, int requestPlayer = -1)
        {
            switch (msgType)
            {
                case GenshinModMessageType.SendGenshinMode:
                    {
                        // Apply the received mode
                        GenshinModeEnabled = reader.ReadBoolean();

                        // Resend packet to other clients if this is the server
                        if (Main.netMode == NetmodeID.Server && toPlayer != Main.myPlayer)
                        {
                            SendGenshinMode(toPlayer, fromPlayer);
                        }
                    }
                    break;

                case GenshinModMessageType.RequestAllCharacterData:
                    {
                        // Packet is received on a server
                        if (Main.netMode == NetmodeID.Server)
                        {
                            // The packet is not directed at us.
                            // Relay the message to the other clients.
                            if (toPlayer != Main.myPlayer)
                            {
                                RequestAllCharacterData(toPlayer, fromPlayer, requestPlayer);
                            }

                            // The packet is directed at us.
                            // Send the data the server has about this player.
                            else
                            {
                                SendAllCharacterData(fromPlayer, Player.whoAmI);
                            }

                            break;
                        }

                        // Packet is received on a client, send 
                        SendAllCharacterData(toPlayer, fromPlayer);
                    }
                    break;


                case GenshinModMessageType.SendCharacter:
                    {
                        // Read the character id from the packet
                        int id = reader.ReadInt32();

                        // Check if the received id is part of the CharacterID enumerator
                        if (!Enum.IsDefined(typeof(GenshinCharacterID), id))
                        {
                            break;
                        }

                        // Add the character if we don't have the character in the dictionary
                        if (!_characters.ContainsKey((GenshinCharacterID)id))
                        {
                            _characters.Add((GenshinCharacterID)id, GenshinCharacterSystem.GetGenshinCharacter((GenshinCharacterID)id, Player));
                        }

                        // Somehow the key is present but the value is null, overwrite it
                        else if (_characters[(GenshinCharacterID)id] == null)
                        {
                            _characters[(GenshinCharacterID)id] = GenshinCharacterSystem.GetGenshinCharacter((GenshinCharacterID)id, Player);
                        }

                        // Pass this packet to the character's ReceivePacket hook.
                        _characters[(GenshinCharacterID)id].ReceiveCharacter(reader);

                        // This is a server, relay the message to other clients.
                        // Only if the packet is not directly send to the server ID.
                        if (Main.netMode == NetmodeID.Server && toPlayer != Main.myPlayer)
                        {
                            SendCharacter((GenshinCharacterID)id, toPlayer, fromPlayer);
                        }
                    }
                    break;


                case GenshinModMessageType.RequestCharacter:
                    {
                        int id = reader.ReadInt32();

                        // Check if the given id is part of the enumerator
                        if (!Enum.IsDefined(typeof(GenshinCharacterID), id))
                        {
                            break;
                        }

                        // Packet is received on a server
                        if (Main.netMode == NetmodeID.Server)
                        {
                            // If this is a server and the packet is not directed at us
                            // relay the message to the other clients
                            if (toPlayer != Main.myPlayer)
                            {
                                RequestCharacter((GenshinCharacterID)id, toPlayer, fromPlayer, requestPlayer);
                            }

                            // If this is a server and the packet is directed at us
                            // Send the data the server has about this player.
                            else
                            {
                                SendCharacter((GenshinCharacterID)id, fromPlayer, Player.whoAmI);
                            }

                            break;
                        }

                        // Check if the requested character is not present
                        if (!_characters.ContainsKey((GenshinCharacterID)id))
                        {
                            break;
                        }

                        // Send the requested character
                        SendCharacter((GenshinCharacterID)id, toPlayer, fromPlayer);
                    }
                    break;


                case GenshinModMessageType.SendTeamComposition:
                    {
                        int count = reader.ReadInt32();

                        List<GenshinCharacterID> pendingTeam = new();

                        bool invalid = false;

                        for (int i = 0; i < count; i++)
                        {
                            int id = reader.ReadInt32();
                            if (!Enum.IsDefined(typeof(GenshinCharacterID), id))
                            {
                                invalid = true;
                            }

                            if (Enum.IsDefined(typeof(GenshinCharacterID), id)
                                && !_characters.ContainsKey((GenshinCharacterID)id))
                            {
                                RequestCharacter((GenshinCharacterID)id, fromPlayer, Main.myPlayer, Player.whoAmI);
                                invalid = true;
                            }

                            if (!invalid)
                            {
                                pendingTeam.Add((GenshinCharacterID)id);
                            }
                        }

                        if (!pendingTeam.Any())
                        {
                            invalid = true;
                        }

                        if (invalid)
                        {
                            RequestTeamComposition(fromPlayer, Main.myPlayer, Player.whoAmI);
                            RequestSelectedCharacter(fromPlayer, Main.myPlayer, Player.whoAmI);
                            break;
                        }

                        // Run the changes between team hook. To run other client's
                        // OnJoinTeam and OnLeaveTeam hooks
                        CheckForTeamChanges(_teamComposition, pendingTeam);

                        // Apply the new team
                        _teamComposition = pendingTeam;

                        // Remove unnecessary character objects from _character dictionary
                        foreach (GenshinCharacterID id in _characters.Keys.ToList())
                        {
                            if (!_teamComposition.Contains(id))
                            {
                                // Remove the character from our dictionary
                                RemoveCharacter(id);
                            }
                        }

                        // Resend the packet if we're the server
                        if (Main.netMode == NetmodeID.Server && toPlayer != Main.myPlayer)
                        {
                            SendTeamComposition(toPlayer, fromPlayer);
                        }
                    }
                    break;


                case GenshinModMessageType.RequestTeamComposition:
                    {
                        // Packet is received on a server
                        if (Main.netMode == NetmodeID.Server)
                        {
                            // If this is a server and the packet is not directed at us
                            // relay the message to the other clients
                            if (toPlayer != Main.myPlayer)
                            {
                                RequestTeamComposition(toPlayer, fromPlayer, requestPlayer);
                            }

                            // If this is a server and the packet is directed at us
                            // Send the data the server has about this player.
                            else
                            {
                                SendTeamComposition(fromPlayer, Player.whoAmI);
                            }

                            break;
                        }

                        SendTeamComposition(toPlayer, fromPlayer);
                    }
                    break;


                case GenshinModMessageType.SendSelectedCharacter:
                    {
                        int id = reader.ReadInt32();
                        bool forced = reader.ReadBoolean();

                        bool invalid = false;

                        if (!Enum.IsDefined(typeof(GenshinCharacterID), id))
                        {
                            invalid = true;
                        }

                        if (Enum.IsDefined(typeof(GenshinCharacterID), id)
                            && (!_characters.ContainsKey((GenshinCharacterID)id)
                            || _characters[(GenshinCharacterID)id] == null))
                        {
                            RequestCharacter((GenshinCharacterID)id, fromPlayer, Main.myPlayer, Player.whoAmI);
                            invalid = true;
                        }

                        if (Enum.IsDefined(typeof(GenshinCharacterID), id)
                            && !_teamComposition.Contains((GenshinCharacterID)id))
                        {
                            RequestTeamComposition(fromPlayer, Main.myPlayer, Player.whoAmI);
                            invalid = true;
                        }

                        if (invalid)
                        {
                            RequestSelectedCharacter(fromPlayer, Main.myPlayer, Player.whoAmI);
                            break;
                        }

                        // Run switch character code
                        if (_selectedCharacterID != (GenshinCharacterID)id)
                        {
                            // Set forced to false
                            _forcedCharacterSwitch = forced;
                            SwitchCharacter((GenshinCharacterID)id);
                        }

                        // Player succesfully joined multiplayer,
                        // make this boolean false
                        _JustJoinedMultiplayer = false;

                        // Resend packet on multiplayer server
                        if (Main.netMode == NetmodeID.Server && toPlayer != Main.myPlayer)
                        {
                            SendSelectedCharacter(toPlayer, fromPlayer, forced);
                        }
                    }
                    break;


                case GenshinModMessageType.RequestSelectedCharacter:
                    {
                        // Packet is received on a server
                        if (Main.netMode == NetmodeID.Server)
                        {
                            // If this is a server and the packet is not directed at us
                            // relay the message to the other clients
                            if (toPlayer != Main.myPlayer)
                            {
                                RequestAllCharacterData(toPlayer, fromPlayer, requestPlayer);
                            }

                            // If this is a server and the packet is directed at us
                            // Send the data the server has about this player.
                            else
                            {
                                SendSelectedCharacter(fromPlayer, Player.whoAmI);
                            }

                            break;
                        }

                        SendSelectedCharacter(toPlayer, fromPlayer);
                    }
                    break;
            }
        }
        #endregion
    }
}
