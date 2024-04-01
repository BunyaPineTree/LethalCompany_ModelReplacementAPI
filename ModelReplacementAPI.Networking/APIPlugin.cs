using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Logging;
using GameNetcodeStuff;
using HarmonyLib;
using System;
using System.Collections.Generic;
using LethalNetworkAPI;
using LethalNetworkAPI.Serializable;

namespace ModelReplacementNetworking
{

    public static class PluginInfo
    {
        public const string GUID = "meow.ModelReplacementAPI.Networking";
        public const string NAME = "ModelReplacementAPI.Networking";
        public const string VERSION = "1.0.0";
        public const string WEBSITE = "https://github.com/BunyaPineTree/LethalCompany_ModelReplacementAPI";
    }


    [BepInPlugin(PluginInfo.GUID, PluginInfo.NAME, PluginInfo.VERSION)]
    [BepInDependency("meow.ModelReplacementAPI", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("LethalNetworkAPI", BepInDependency.DependencyFlags.HardDependency)]
    public class ModelReplacementAPI_Networking : BaseUnityPlugin
    {

        private void Awake()
        {
            Logger = BepInEx.Logging.Logger.CreateLogSource(PluginInfo.GUID);
            // Plugin startup logic
            bool flag = ModelReplacementAPI_Networking.Instance == null;
            if (flag)
            {
                ModelReplacementAPI_Networking.Instance = this;
            }

            Harmony harmony = new Harmony(PluginInfo.GUID);
            harmony.PatchAll();
            Logger.LogInfo($"Plugin {PluginInfo.GUID} is loaded!");
        }

        //Other
        public static ModelReplacementAPI_Networking Instance = null;
        public new ManualLogSource Logger;
        private static int steamLobbyID => GameNetworkManager.Instance.currentLobby.HasValue ? (int)GameNetworkManager.Instance.currentLobby.Value.Id.Value : -1;
        public static bool IsLan => steamLobbyID == -1;

        [HarmonyPatch(typeof(EnemyAI))]
        public class EnemyAIPatch
        {
            [HarmonyPatch("HitEnemy")]
            [HarmonyPrefix]

            [HarmonyAfter()]
            public static void HitEnemy(ref EnemyAI __instance, int force = 1, PlayerControllerB playerWhoHit = null, bool playHitSFX = false)
            {

            }

        }
    }
}