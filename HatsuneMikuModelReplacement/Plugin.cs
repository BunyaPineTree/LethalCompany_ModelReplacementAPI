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



    [BepInPlugin("meow.MikuModelReplacement", "Miku Model", "0.1")]
    [BepInDependency("meow.ModelReplacementAPI", BepInDependency.DependencyFlags.HardDependency)]
    public class Plugin : BaseUnityPlugin
    {

        private void Awake()
        {
            // Plugin startup logic

            ModelReplacementAPI.RegisterSuitModelReplacement("Default",typeof(BodyReplacementMiku));
            //ModelReplacementAPI.RegisterSuitModelReplacement("Green Suit", typeof(BodyReplacementMikuEvil));

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
               // ModelReplacementAPI.SetPlayerModelReplacement(__instance, typeof(BodyReplacementMiku));

            }

        }



    }

    public static class Assets
    {
        public static AssetBundle MainAssetBundle = null;


        public static void PopulateAssets()
        {
            if (MainAssetBundle == null)
            {
                //projectAssemblyName.bundleName
                using (var assetStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("HatsuneMikuModelReplacement.mbundle"))
                {
                    MainAssetBundle = AssetBundle.LoadFromStream(assetStream);
                }

            }
        }
    }

}