using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Logging;
using GameNetcodeStuff;
using HarmonyLib;
using System;
using System.Collections.Generic;

namespace ModelReplacementVRM
{

    public static class PluginInfo
    {
        public const string GUID = "meow.ModelReplacementAPI.VRMSupport";
        public const string NAME = "ModelReplacementAPI.VRMSupport";
        public const string VERSION = "0.0.1";
        public const string WEBSITE = "https://github.com/BunyaPineTree/LethalCompany_ModelReplacementAPI";
    }


    [BepInPlugin(PluginInfo.GUID, PluginInfo.NAME, PluginInfo.VERSION)]
    [BepInDependency("meow.ModelReplacementAPI", BepInDependency.DependencyFlags.HardDependency)]
    public class ModelReplacementAPI_VRM : BaseUnityPlugin
    {

        private void Awake()
        {
            Logger = BepInEx.Logging.Logger.CreateLogSource(PluginInfo.GUID);
            // Plugin startup logic
            bool flag = ModelReplacementAPI_VRM.Instance == null;
            if (flag)
            {
                ModelReplacementAPI_VRM.Instance = this;
            }

            Harmony harmony = new Harmony(PluginInfo.GUID);
            harmony.PatchAll();
            Logger.LogInfo($"Plugin {PluginInfo.GUID} is loaded!");
        }

        //Other
        public static ModelReplacementAPI_VRM Instance = null;
        public new ManualLogSource Logger;

    }
}