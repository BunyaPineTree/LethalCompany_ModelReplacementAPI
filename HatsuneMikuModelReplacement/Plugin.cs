using BepInEx;
using HarmonyLib;
using UnityEngine;
using System.Reflection;
using ModelReplacement;
using BepInEx.Configuration;
using System;

//using System.Numerics;

namespace HatsuneMikuModelReplacement
{




    [BepInPlugin("meow.MikuModelReplacement", "Miku Model", "1.4.1")]
    [BepInDependency("meow.ModelReplacementAPI", BepInDependency.DependencyFlags.HardDependency)]
    public class Plugin : BaseUnityPlugin
    {
        public static ConfigFile config;

        // Universal config options 
        public static ConfigEntry<bool> enableMikuForAllSuits { get; private set; }
        public static ConfigEntry<bool> enableMikuAsDefault { get; private set; }
        public static ConfigEntry<string> suitNamesToEnableMiku { get; private set; }
        
        // Miku model specific config options
        public static ConfigEntry<float> UpdateRate { get; private set; }
        public static ConfigEntry<float> distanceDisablePhysics { get; private set; }
        public static ConfigEntry<bool> disablePhysicsAtRange { get; private set; }

        private static void InitConfig()
        {
            enableMikuForAllSuits = config.Bind<bool>("Suits to Replace Settings", "Enable Miku for all Suits", false, "Enable to replace every suit with Miku. Set to false to specify suits");
            enableMikuAsDefault = config.Bind<bool>("Suits to Replace Settings", "Enable Miku as default", false, "Enable to replace every suit that hasn't been otherwise registered with Miku.");
            suitNamesToEnableMiku = config.Bind<string>("Suits to Replace Settings", "Suits to enable Miku for", "Default,Orange suit", "Enter a comma separated list of suit names.(Additionally, [Green suit,Pajama suit,Hazard suit])");

            UpdateRate = config.Bind<float>("Dynamic Bone Settings", "Update rate", 60, "Refreshes dynamic bones more times per second the higher the number");
            disablePhysicsAtRange = config.Bind<bool>("Dynamic Bone Settings", "Disable physics at range", false, "Enable to disable physics past the specified range");
            distanceDisablePhysics = config.Bind<float>("Dynamic Bone Settings", "Distance to disable physics", 20, "If Disable physics at range is enabled, this is the range after which physics is disabled.");
            
        }
        private void Awake()
        {
            config = base.Config;
            InitConfig();
            Assets.PopulateAssets();

            // Plugin startup logic


            if (enableMikuForAllSuits.Value)
            {
                ModelReplacementAPI.RegisterModelReplacementOverride(typeof(BodyReplacementMiku));

            }
            if (enableMikuAsDefault.Value)
            {
                ModelReplacementAPI.RegisterModelReplacementDefault(typeof(BodyReplacementMiku));

            }

            var commaSepList = suitNamesToEnableMiku.Value.Split(',');
            foreach (var item in commaSepList)
            {
                ModelReplacementAPI.RegisterSuitModelReplacement(item, typeof(BodyReplacementMiku));
            }
                

            Harmony harmony = new Harmony("meow.MikuModelReplacement");
            harmony.PatchAll();
            Logger.LogInfo($"Plugin {"meow.MikuModelReplacement"} is loaded!");
        }
    }
    public static class Assets
    {
        // Replace mbundle with the Asset Bundle Name from your unity project 
        public static string mainAssetBundleName = "mbundle";
        public static AssetBundle MainAssetBundle = null;

        private static string GetAssemblyName() => Assembly.GetExecutingAssembly().GetName().Name;
        public static void PopulateAssets()
        {
            if (MainAssetBundle == null)
            {
                Console.WriteLine(GetAssemblyName() + "." + mainAssetBundleName);
                using (var assetStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(GetAssemblyName() + "." + mainAssetBundleName))
                {
                    MainAssetBundle = AssetBundle.LoadFromStream(assetStream);
                }

            }
        }
    }

}