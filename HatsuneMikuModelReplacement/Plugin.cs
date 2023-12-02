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
using ModelReplacement;

//using System.Numerics;

namespace HatsuneMikuModelReplacement
{




    [BepInPlugin("meow.MikuModelReplacement", "Miku Model", "1.0")]
    [BepInDependency("meow.ModelReplacementAPI", BepInDependency.DependencyFlags.HardDependency)]
    public class Plugin : BaseUnityPlugin
    {

        private void Awake()
        {
            // Plugin startup logic

            ModelReplacementAPI.RegisterSuitModelReplacement("Green suit", typeof(BodyReplacementMiku));
            ModelReplacementAPI.RegisterSuitModelReplacement("Default", typeof(BodyReplacementMiku));
            ModelReplacementAPI.RegisterSuitModelReplacement("Orange suit", typeof(BodyReplacementMiku));
            ModelReplacementAPI.RegisterSuitModelReplacement("Pajama suit", typeof(BodyReplacementMiku));

            Assets.PopulateAssets();

            Harmony harmony = new Harmony("meow.MikuModelReplacement");
            harmony.PatchAll();
            Logger.LogInfo($"Plugin {"meow.MikuModelReplacement"} is loaded!");
        }



        [HarmonyPatch(typeof(PlayerControllerB))]
        public class PlayerControllerBPatch
        {

            [HarmonyPatch("Update")]
            [HarmonyPostfix]
            public static void UpdatePatch(ref PlayerControllerB __instance)
            {
                if (__instance.playerSteamId == 0) { return; }
                //ModelReplacementAPI.SetPlayerModelReplacement(__instance, typeof(BodyReplacementMinahoshi));

            }

        }



    }
    public static class Assets
    {
        public static string mainAssetBundleName = "mbundle";
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