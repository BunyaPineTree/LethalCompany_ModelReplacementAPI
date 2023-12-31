using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Logging;
using GameNetcodeStuff;
using HarmonyLib;
using System;
using System.Collections.Generic;

namespace ModelReplacement
{

    public static class PluginInfo
    {
        public const string GUID = "meow.ModelReplacementAPI";
        public const string NAME = "ModelReplacementAPI";
        public const string VERSION = "2.2.0";
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

        private void Awake()
        {
            Logger = BepInEx.Logging.Logger.CreateLogSource(PluginInfo.GUID);
            // Plugin startup logic
            bool flag = ModelReplacementAPI.Instance == null;
            if (flag)
            {
                ModelReplacementAPI.Instance = this;
            }

            moreCompanyPresent = Chainloader.PluginInfos.ContainsKey("me.swipez.melonloader.morecompany");
            thirdPersonPresent = Chainloader.PluginInfos.ContainsKey("verity.3rdperson");
            LCthirdPersonPresent = Chainloader.PluginInfos.ContainsKey("LCThirdPerson");
            mirrorDecorPresent = Chainloader.PluginInfos.ContainsKey("quackandcheese.mirrordecor");
            tooManyEmotesPresent = Chainloader.PluginInfos.ContainsKey("FlipMods.TooManyEmotes");
            recordingCameraPresent = Chainloader.PluginInfos.ContainsKey("com.graze.gorillatag.placeablecamera");


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


        //Other
        public static ModelReplacementAPI Instance;
        public new ManualLogSource Logger;
        private static int steamLobbyID => GameNetworkManager.Instance.currentLobby.HasValue ? (int)GameNetworkManager.Instance.currentLobby.Value.Id.Value : -1;
        public static bool isLan => steamLobbyID == -1;

        #region Registry and API methods

        private static List<Type> RegisteredModelReplacementExceptions = new List<Type>();
        private static Dictionary<string, Type> RegisteredModelReplacements = new Dictionary<string, Type>();
        private static Type RegisteredModelReplacementOverride = null;
        private static Type RegisteredModelReplacementDefault = null;

        /// <summary>
        /// Registers a body replacement class to default. All players with unregistered suits will appear with this body replacement, if not null. 
        /// </summary>
        /// <param name="type"></param>
        public static void RegisterModelReplacementDefault(Type type)
        {
            if (!type.IsSubclassOf(typeof(BodyReplacementBase)))
            {
                Instance.Logger.LogError($"Cannot register body replacement default type {type}, must inherit from BodyReplacementBase");
                return;
            }
            if (RegisteredModelReplacementOverride != null)
            {
                Instance.Logger.LogError($"Cannot register body replacement default, already registered to {RegisteredModelReplacementDefault}.");
                return;
            }

            Instance.Logger.LogInfo($"Registering body replacement default type {type}.");

            RegisteredModelReplacementDefault = type;
        }
        /// <summary>
        /// Registers a body replacement class to override. All players will have their model replaced. 
        /// </summary>
        /// <param name="type"></param>
        public static void RegisterModelReplacementOverride(Type type)
        {
            if (!type.IsSubclassOf(typeof(BodyReplacementBase)))
            {
                Instance.Logger.LogError($"Cannot register body replacement override type {type}, must inherit from BodyReplacementBase");
                return;
            }
            if (RegisteredModelReplacementOverride != null)
            {
                Instance.Logger.LogError($"Cannot register body replacement override, already registered to {RegisteredModelReplacementOverride}.");
                return;
            }

            Instance.Logger.LogInfo($"Registering body replacement override type {type}.");

            RegisteredModelReplacementOverride = type;
        }
        /// <summary>
        /// Registers a body replacement class as an exception . Players who have their model set to this class will not have it automatically changed. 
        /// </summary>
        /// <param name="type"></param>
        public static void RegisterModelReplacementException(Type type)
        {
            if (!type.IsSubclassOf(typeof(BodyReplacementBase)))
            {
                Instance.Logger.LogError($"Cannot register body replacement exception type {type}, must inherit from BodyReplacementBase");
                return;
            }

            Instance.Logger.LogInfo($"Registering body replacement exception type {type}.");

            if (!RegisteredModelReplacementExceptions.Contains(type))
            {
                RegisteredModelReplacementExceptions.Add(type);
            }
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

        /// <summary>
        /// Sets a body replacement for the specified player, removes existing body replacement if it is a different type than the specified
        /// </summary>
        /// <param name="player"></param>
        /// <param name="type">typeof body replacement class. Must inherit from BodyReplacementBase</param>
        public static void SetPlayerModelReplacement(PlayerControllerB player, Type type)
        {
            if (!type.IsSubclassOf(typeof(BodyReplacementBase)))
            {
                Instance.Logger.LogError($"Cannot set body replacement of type {type.Name}, must inherit from BodyReplacementBase");
                return;
            }
            if (!isLan && (player.playerSteamId == 0))
            {
                return;
            }
            BodyReplacementBase a = player.thisPlayerBody.gameObject.GetComponent<BodyReplacementBase>();
            int suitID = player.currentSuitID;
            string suitName = StartOfRound.Instance.unlockablesList.unlockables[suitID].unlockableName;
            if (a != null)
            {



                if (a.GetType() == type) //Suit has not changed model
                {

                    if (a.suitName != suitName)
                    {
                        Console.WriteLine($"Suit Change detected {a.suitName} => {suitName}, Replacing {type}.");
                        Destroy(a); //Suit name changed, may represent change in skin of model replacement, destroy
                    }
                    else
                    {
                        return;//No need to add a body replacement, the suit has not changed and the model has not changed
                    }
                }
                else //Suit has changed model
                {
                    Console.WriteLine($"Model Replacement Change detected {a.GetType()} => {type}, changing model.");
                    Destroy(a); //Destroy the existing body replacement
                }
            }

            BodyReplacementBase replacecment = player.thisPlayerBody.gameObject.AddComponent(type) as BodyReplacementBase;
            replacecment.suitName = suitName;
        }
        /// <summary>
        /// Removes any existing body replacement 
        /// </summary>
        /// <param name="player"></param>
        public static void RemovePlayerModelReplacement(PlayerControllerB player)
        {
            BodyReplacementBase a = player.thisPlayerBody.gameObject.GetComponent<BodyReplacementBase>();
            if (a)
            {
                Destroy(a);
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
                    BodyReplacementBase a = __instance.thisPlayerBody.gameObject.GetComponent<BodyReplacementBase>();
                    if ((a != null) && RegisteredModelReplacementExceptions.Contains(a.GetType()))
                    {
                        return;
                    }

                    if (RegisteredModelReplacementOverride != null)
                    {
                        SetPlayerModelReplacement(__instance, RegisteredModelReplacementOverride);
                        return;
                    }

                    int suitID = __instance.currentSuitID;
                    string suitName = StartOfRound.Instance.unlockablesList.unlockables[suitID].unlockableName;
                    suitName = suitName.ToLower().Replace(" ", "");

                    if (RegisteredModelReplacements.ContainsKey(suitName))
                    {
                        SetPlayerModelReplacement(__instance, RegisteredModelReplacements[suitName]);
                    }
                    else if (RegisteredModelReplacementDefault != null)
                    {
                        SetPlayerModelReplacement(__instance, RegisteredModelReplacementDefault);
                    }
                    else
                    {
                        RemovePlayerModelReplacement(__instance);
                    }
                }
                catch (Exception e) { ModelReplacementAPI.Instance.Logger.LogWarning(e); }
            }
        }
        #endregion


    }
}