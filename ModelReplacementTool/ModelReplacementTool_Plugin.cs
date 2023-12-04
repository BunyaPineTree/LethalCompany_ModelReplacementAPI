using BepInEx;
using System.Linq;
using System.Runtime.InteropServices;
using Zeekerss;
using Zeekerss.Core;
using Zeekerss.Core.Singletons;
using System;
using HarmonyLib;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using System.Reflection;
using GameNetcodeStuff;
using BepInEx.Logging;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem.XR;
using ModelReplacementTool;
//using static System.Net.Mime.MediaTypeNames;
//using System.Numerics;
using ModelReplacement;

namespace ModelReplacementTool
{



    [BepInPlugin("meow.ModelReplacementTool", "ModelReplacementTool", "1.0")]
    [BepInDependency("meow.ModelReplacementAPI", BepInDependency.DependencyFlags.HardDependency)]
    public class ModelReplacementTool : BaseUnityPlugin
    {

        private void Awake()
        {
            Assets.PopulateAssets();
            Logger = BepInEx.Logging.Logger.CreateLogSource("ModelReplacementTool");
            // Plugin startup logic
            bool flag = ModelReplacementTool.Instance == null;
            if (flag)
            {
                ModelReplacementTool.Instance = this;
            }


            Harmony harmony = new Harmony("meow.ModelReplacementTool");
            harmony.PatchAll();
            Logger.LogInfo($"Plugin {"meow.ModelReplacementTool"} is loaded!");
        }
        public static ModelReplacementTool Instance;
        public new ManualLogSource Logger;


        [HarmonyPatch(typeof(PlayerControllerB))]
        public class PlayerControllerBPatch
        {

            [HarmonyPatch("Update")]
            [HarmonyPostfix]
            public static void UpdatePatch(ref PlayerControllerB __instance)
            {
                bool localPlayer = (ulong)StartOfRound.Instance.thisClientPlayerId == __instance.playerClientId;
                if (!localPlayer) { return; }
                
                BodyReplacementBase component = __instance.gameObject.GetComponent<BodyReplacementBase>();
                if (component == null) { return; }
                if (__instance.gameObject.GetComponent<ToolComponent>()) { return; }
                __instance.gameObject.AddComponent<ToolComponent>();


            }



        }

        
    }

    public static class Assets
    {
        public static string mainAssetBundleName = "tbundle";
        public static AssetBundle MainAssetBundle = null;

        private static string GetAssemblyName() => Assembly.GetExecutingAssembly().FullName.Split(',')[0];
        public static void PopulateAssets()
        {
            if (MainAssetBundle == null)
            {
                using (var assetStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(GetAssemblyName() + "." + mainAssetBundleName))
                {
                    MainAssetBundle = AssetBundle.LoadFromStream(assetStream);
                }

            }
        }
    }
}