using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using BepInEx.Logging;
using GameNetcodeStuff;
using HarmonyLib;
using MirrorDecor;
using ModelReplacement.Monobehaviors;
using ModelReplacement.Monobehaviors.Enemies;
using System;
using System.Collections.Generic;

namespace ModelReplacement
{

    public static class PluginInfo
    {
        public const string GUID = "meow.ModelReplacementAPI";
        public const string NAME = "ModelReplacementAPI";
        public const string VERSION = "2.4.12";
        public const string WEBSITE = "https://github.com/BunyaPineTree/LethalCompany_ModelReplacementAPI";
    }


    [BepInPlugin(PluginInfo.GUID, PluginInfo.NAME, PluginInfo.VERSION)]
    [BepInDependency("me.swipez.melonloader.morecompany", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("verity.3rdperson", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("LCThirdPerson", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("quackandcheese.mirrordecor", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("FlipMods.TooManyEmotes", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("com.graze.gorillatag.placeablecamera", BepInDependency.DependencyFlags.SoftDependency)]


    public class ModelReplacementAPI : BaseUnityPlugin
    {
        public static ConfigFile config;

        // Universal config options 
        public static ConfigEntry<bool> EnforceViewModelGeneration { get; private set; }

        private static void InitConfig()
        {
            EnforceViewModelGeneration = config.Bind<bool>("Debug Settings", "Generate Viewmodels by default", true, "Enable to generate a viewmodel for all model replacements, regardless of the individual model's settings.");
        }
        private void Awake()
        {
            config = base.Config;
            InitConfig();

            Logger = BepInEx.Logging.Logger.CreateLogSource(PluginInfo.GUID);
            // Plugin startup logic
            bool flag = ModelReplacementAPI.Instance == null;
            if (flag)
            {
                ModelReplacementAPI.Instance = this;
            }

            moreCompanyPresent = IsPluginPresent("me.swipez.melonloader.morecompany");
            thirdPersonPresent = IsPluginPresent("verity.3rdperson");
            LCthirdPersonPresent = IsPluginPresent("LCThirdPerson");
            mirrorDecorPresent = IsPluginPresent("quackandcheese.mirrordecor");
            tooManyEmotesPresent = IsPluginPresent("FlipMods.TooManyEmotes");
            recordingCameraPresent = IsPluginPresent("com.graze.gorillatag.placeablecamera");

            MRAPI_NetworkingPresent = IsPluginPresent("meow.ModelReplacementAPI.Networking");

            Harmony harmony = new Harmony(PluginInfo.GUID);
            harmony.PatchAll();
            Logger.LogInfo($"Plugin {PluginInfo.GUID} is loaded!");
        }

        //soft dependencies
        public static bool moreCompanyPresent;
        public static bool thirdPersonPresent;
        public static bool LCthirdPersonPresent;
        public static bool mirrorDecorPresent;
        public static bool tooManyEmotesPresent;
        public static bool recordingCameraPresent;

        public static bool MRAPI_NetworkingPresent;


        //Other
        public static ModelReplacementAPI Instance = null;
        public new ManualLogSource Logger;
        private static int steamLobbyID => GameNetworkManager.Instance.currentLobby.HasValue ? (int)GameNetworkManager.Instance.currentLobby.Value.Id.Value : -1;
        public static bool IsLan => steamLobbyID == -1;

        #region Registry and API methods

        private static List<Type> RegisteredModelReplacementExceptions = new List<Type>();
        private static Dictionary<string, Type> RegisteredModelReplacements = new Dictionary<string, Type>();
        private static Type RegisteredModelReplacementOverride = null;
        private static Type RegisteredModelReplacementDefault = null;
        private static HashSet<ulong> blackListedSteamIDs = new HashSet<ulong>();

        private static bool IsPluginPresent(string pluginName)
        {
            return Chainloader.PluginInfos.ContainsKey(pluginName);
        }

        /// <summary>
        /// Registers a body replacement class to default. All players with unregistered suits will appear with this body replacement, if not null. 
        /// </summary>
        /// <param name="type"></param>
        public static void RegisterModelReplacementDefault(Type type)
        {
            RegisterModelReplacement(type, ref RegisteredModelReplacementDefault, "default");
        }

        /// <summary>
        /// Registers a body replacement class to override. All players will have their model replaced. 
        /// </summary>
        /// <param name="type"></param>
        public static void RegisterModelReplacementOverride(Type type)
        {
            RegisterModelReplacement(type, ref RegisteredModelReplacementOverride, "override");
        }

        /// <summary>
        /// Registers a body replacement class as an exception. Players who have their model set to this class will not have it automatically changed. 
        /// </summary>
        /// <param name="type"></param>
        public static void RegisterModelReplacementException(Type type)
        {
            RegisterModelReplacement(type, RegisteredModelReplacementExceptions, "exception");
        }

        /// <summary>
        /// Registers a specified body replacement class to a specified suit name. All players wearing a suit with the specified name will have their model replaced. 
        /// </summary>
        /// <param name="suitNameToReplace"></param>
        /// <param name="type"></param>
        public static void RegisterSuitModelReplacement(string suitNameToReplace, Type type)
        {
            suitNameToReplace = suitNameToReplace.ToLower().Replace(" ", "");
            if (!type.IsSubclassOf(typeof(BodyReplacementBase)))
            {
                Instance.Logger.LogError($"Cannot register body replacement type {type}, must inherit from BodyReplacementBase");
                return;
            }

            if (RegisteredModelReplacements.ContainsKey(suitNameToReplace))
            {
                Instance.Logger.LogError($"Cannot register body replacement type {type}, suit name to replace {suitNameToReplace} is already registered.");
                return;
            }

            Instance.Logger.LogInfo($"Registering body replacement type {type} to suit name {suitNameToReplace}.");
            RegisteredModelReplacements.Add(suitNameToReplace, type);
        }


        private static void RegisterModelReplacement(Type type, ref Type registeredType, string logType)
        {
            if (!type.IsSubclassOf(typeof(BodyReplacementBase)))
            {
                Instance.Logger.LogError($"Cannot register body replacement {logType} type {type}, must inherit from BodyReplacementBase");
                return;
            }

            if (registeredType != null)
            {
                Instance.Logger.LogError($"Cannot register body replacement {logType}, already registered to {registeredType}.");
                return;
            }

            Instance.Logger.LogInfo($"Registering body replacement {logType} type {type}.");
            registeredType = type;
        }

        private static void RegisterModelReplacement(Type type, List<Type> registeredList, string logType)
        {
            if (!type.IsSubclassOf(typeof(BodyReplacementBase)))
            {
                Instance.Logger.LogError($"Cannot register body replacement {logType} type {type}, must inherit from BodyReplacementBase");
                return;
            }

            Instance.Logger.LogInfo($"Registering body replacement {logType} type {type}.");
            if (!registeredList.Contains(type))
            {
                registeredList.Add(type);
            }
        }


        /// <summary>
        /// Registers a steamID to be blacklisted from SetPlayerModelReplacement
        /// </summary>
        public static void RegisterPlayerBlackList(ulong steamID, bool blackListed)
        {
            if (blackListed)
            {
                blackListedSteamIDs.Add(steamID);
                return;
            }
            
            if (blackListedSteamIDs.Contains(steamID))
            {
                blackListedSteamIDs.Remove(steamID);
            }
        }

        /// <summary>
        /// Registers a player's steamID to be blacklisted from SetPlayerModelReplacement, and removes any active model replacement
        /// </summary>
        public static void RegisterPlayerBlackList(PlayerControllerB player, bool blackListed)
        {
            RegisterPlayerBlackList(player.playerSteamId, blackListed);
            RemovePlayerModelReplacement(player);
        }

        /// <summary>
        /// Destroys and reinstantiates a player's model replacement. Does nothing if they did not have a model replacement.
        /// </summary>
        public static void ResetPlayerModelReplacement(PlayerControllerB player)
        {
            BodyReplacementBase bodyReplacement = player.gameObject.GetComponent<BodyReplacementBase>();
            if (bodyReplacement == null)
            {
                return; // Player doesn't have a body replacement
            }

            Instance.Logger.LogInfo($"Reinstantiating model replacement for {player.playerUsername}");
            Type bodyReplacementType = bodyReplacement.GetType();
            bodyReplacement.IsActive = false;
            Destroy(bodyReplacement);
            player.gameObject.AddComponent(bodyReplacementType);
        }

        /// <summary>
        /// Sets a body replacement for the specified player, removes existing body replacement if it is a different type than the specified
        /// </summary>
        public static void SetPlayerModelReplacement(PlayerControllerB player, Type type)
        {
            if (!type.IsSubclassOf(typeof(BodyReplacementBase)))
            {
                Instance.Logger.LogError($"Cannot set body replacement of type {type.Name}, must inherit from BodyReplacementBase");
                return;
            }

            if (!IsLan && player.playerSteamId == 0)
            {
                return;
            }

            if (blackListedSteamIDs.Contains(player.playerSteamId))
            {
                return;
            }

            bool hasExistingReplacement = player.gameObject.TryGetComponent(out BodyReplacementBase existingReplacement);
            int suitID = player.currentSuitID;
            string suitName = StartOfRound.Instance.unlockablesList.unlockables[suitID].unlockableName;

            if (hasExistingReplacement)
            {
                if (existingReplacement.GetType() == type && existingReplacement.suitName == suitName)
                {
                    return; // No need to add a body replacement, the suit and model have not changed
                }

                Instance.Logger.LogInfo($"Model Replacement Change detected {existingReplacement.GetType()} => {type}, changing model.");
                existingReplacement.IsActive = false;
                Destroy(existingReplacement); // Destroy the existing body replacement
            }

            Instance.Logger.LogInfo($"Suit Change detected {existingReplacement?.suitName} => {suitName}, Replacing {type}.");
            BodyReplacementBase replacement = player.gameObject.AddComponent(type) as BodyReplacementBase;
            replacement.suitName = suitName;
        }

        /// <summary>
        /// Returns true if a player has an active model replacement.
        /// </summary>
        public static bool GetPlayerModelReplacement(PlayerControllerB player, out BodyReplacementBase modelReplacement)
        {
            try
            {
                modelReplacement = player.gameObject.GetComponent<BodyReplacementBase>();
                return (modelReplacement != null);
            }
            catch (Exception e)
            {
                Instance.Logger.LogError($"Error in GetPlayerModelReplacement, returning false {e}");
                modelReplacement = null;
                return false;
            }
        }

        /// <summary>
        /// Returns true if a player has an active model replacement.
        /// </summary>
        public static bool GetPlayerModelReplacement(PlayerControllerB player)
        {
            try
            {
                return (player.gameObject.GetComponent<BodyReplacementBase>() != null);
            }
            catch (Exception e)
            {
                Instance.Logger.LogError($"Error in GetPlayerModelReplacement, returning false {e}");

                return false;
            }
        }

        /// <summary>
        /// Removes any existing body replacement 
        /// </summary>
        public static void RemovePlayerModelReplacement(PlayerControllerB player)
        {
            BodyReplacementBase existingReplacement = player.gameObject.GetComponent<BodyReplacementBase>();
            if (existingReplacement)
            {
                existingReplacement.IsActive = false;
                Destroy(existingReplacement);
            }
        }
        #endregion

        #region Registry Patch
        [HarmonyPatch(typeof(PlayerControllerB))]
        public class PlayerControllerBPatch
        {

            [HarmonyPatch("Update")]
            [HarmonyPostfix]
            public static void ManageRegistryBodyReplacements(ref PlayerControllerB __instance)
            {
                try
                {
                    ManageBodyReplacements(__instance);
                }
                catch (Exception e)
                {
                    Instance.Logger.LogError($"Exception in ManageRegistryBodyReplacements: {e}");
                }
            }

            private static void ManageBodyReplacements(PlayerControllerB player)
            {
                BodyReplacementBase currentReplacement = player.thisPlayerBody.gameObject.GetComponent<BodyReplacementBase>();

                if (currentReplacement != null && RegisteredModelReplacementExceptions.Contains(currentReplacement.GetType()))
                {
                    return;
                }

                if (RegisteredModelReplacementOverride != null)
                {
                    SetPlayerModelReplacement(player, RegisteredModelReplacementOverride);
                    return;
                }


                int suitID = player.currentSuitID;
                var suitList = StartOfRound.Instance.unlockablesList.unlockables;

                if (suitID > suitList.Count)
                {
                    throw new Exception($"Suit ID {suitID} is out of range of the suit list, which has {suitList.Count} elements.");
                }

                if (suitID >= suitList.Count) { suitID = 0; }

                string suitName = suitList[suitID].unlockableName.ToLower().Replace(" ", "");

                if (RegisteredModelReplacements.ContainsKey(suitName))
                {
                    SetPlayerModelReplacement(player, RegisteredModelReplacements[suitName]);
                    return;
                }

                if (RegisteredModelReplacementDefault != null)
                {
                    SetPlayerModelReplacement(player, RegisteredModelReplacementDefault);
                    return;
                }

                RemovePlayerModelReplacement(player);
            }
        }
        #endregion

      


    }
}