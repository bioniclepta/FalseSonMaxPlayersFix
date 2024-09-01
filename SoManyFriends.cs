using BepInEx;
using BepInEx.Configuration;
using R2API.Utils;
using RoR2;
using RoR2.Networking;
using UnityEngine;
using UnityEngine.Networking;

namespace TooManyFriends
{
    [BepInDependency("com.bepis.r2api")]
    [BepInPlugin("dev.wildbook.toomanyfriends", "TooManyFriends", "1.1.0")]
    //[R2APISubmoduleDependency(nameof(CommandHelper))]
    [NetworkCompatibility(CompatibilityLevel.NoNeedForSync)]
    public class TooManyFriends : BaseUnityPlugin
    {
        private static (int maxPlayers, int hardMaxPlayers, int maxLocalPlayers) _default;

        private static ConfigEntry<int> LobbySizeConfig { get; set; }

        public static int LobbySize
        {
            get => LobbySizeConfig.Value;
            protected set => LobbySizeConfig.Value = value;
        }

        public TooManyFriends()
        {
            //CommandHelper.AddToConsoleWhenReady();

            _default = (
                RoR2Application.maxPlayers,
                RoR2Application.hardMaxPlayers,
                RoR2Application.maxLocalPlayers
            );

            LobbySizeConfig = Config.Bind("Game", "LobbySize", 16, "Sets the max size of custom game lobbies");
            LobbySizeConfig.SettingChanged += (sender, args) => SetLobbySize(LobbySize);
        }

        public void OnEnable() {
            SetLobbySize(LobbySize);
            On.RoR2.MeridianEventTriggerInteraction.HandleInitializeMeridianEvent += NewMeridianEventInitializer;
        }

        public void OnDisable() {
            SetLobbySize(_default.maxPlayers, _default.hardMaxPlayers, _default.maxLocalPlayers);
            On.RoR2.MeridianEventTriggerInteraction.HandleInitializeMeridianEvent -= NewMeridianEventInitializer;
        }

        public void SetLobbySize(int maxPlayers, int? hardMaxPlayers = null, int? maxLocalPlayers = null)
        {
            RoR2Application.maxPlayers = maxPlayers;
            RoR2Application.hardMaxPlayers = hardMaxPlayers ?? maxPlayers;
            RoR2Application.maxLocalPlayers = maxLocalPlayers ?? maxPlayers;

            LobbyManager.cvSteamLobbyMaxMembers.defaultValue = maxPlayers.ToString();
            LobbyManager.cvSteamLobbyMaxMembers.SetPropertyValue("value", maxPlayers);

            NetworkManagerSystem.SvMaxPlayersConVar.instance.SetString(maxPlayers.ToString());
        }

        [ConCommand(commandName = "mod_tmf", flags = ConVarFlags.None, helpText = "Lets you change the max size of custom game lobbies.")]
        private static void CCSetMaxLobbySize(ConCommandArgs args)
        {
            args.CheckArgumentCount(1);

            if (int.TryParse(args[0], out var lobbySize))
            {
                LobbySizeConfig.Value = lobbySize;
                Debug.Log($"Lobby max size set to {LobbySizeConfig.Value}.");
            }
            else
                Debug.Log("Invalid argument.");
        }

        private static void NewMeridianEventInitializer(On.RoR2.MeridianEventTriggerInteraction.orig_HandleInitializeMeridianEvent orig, MeridianEventTriggerInteraction self)
        {
            //Gets the number of players
            int playerCount = PlayerCharacterMasterController.instances.Count;
            //If we have 4 or less players, we can use the original method
            if (playerCount <= 4)
            {
                //runs the original script
                Debug.Log("PANKER MOD: RUNNING ORIGINAL SCRIPT.");
                orig(self);
            }
            else
            {
                Debug.Log("PANKER MOD: RUNNING NEW SCRIPT.");
                //Incremental counter
                int mysillynumber = 0;
                //Creates a new tp location array with the number of players in the lobby
                GameObject[] newPlayerTPLocs = new GameObject[playerCount];
                //replaces the original methods array with the new one
                self.playerTPLocs = newPlayerTPLocs;
                foreach (PlayerCharacterMasterController instance in PlayerCharacterMasterController.instances)
                {
                    if (instance == null) { continue; }
                    switch (mysillynumber)
                    {
                        case 0:
                            self.playerTPLocs[0] = self.playerTPLoc1;
                            break;
                        case 1:
                            self.playerTPLocs[1] = self.playerTPLoc2;
                            break;
                        case 2:
                            self.playerTPLocs[2] = self.playerTPLoc3;
                            break;
                        case 3:
                            self.playerTPLocs[3] = self.playerTPLoc4;
                            break;
                        default:
                            //Going past the 4th location will just default to location 4
                            self.playerTPLocs[mysillynumber] = self.playerTPLoc4;
                            break;
                    }
                    mysillynumber++;
                }
                //After the setting of our new locations, we can defer back to the old script
                orig(self);
            }
        }
    }
}