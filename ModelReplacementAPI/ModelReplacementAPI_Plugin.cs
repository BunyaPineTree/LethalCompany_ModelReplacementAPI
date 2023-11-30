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
using MoreCompany;
using Unity.Netcode;
//using System.Numerics;

namespace ModelReplacement
{



    [BepInPlugin("meow.ModelReplacementAPI", "ModelReplacementAPI", "0.9")]
    [BepInDependency("me.swipez.melonloader.morecompany", BepInDependency.DependencyFlags.SoftDependency)]
    public class ModelReplacementAPI : BaseUnityPlugin
    {

        private void Awake()
        {
            Logger = BepInEx.Logging.Logger.CreateLogSource("ModelReplacementAPI");
            // Plugin startup logic
            bool flag = ModelReplacementAPI.Instance == null;
            if (flag)
            {
                ModelReplacementAPI.Instance = this;
            }


            Harmony harmony = new Harmony("meow.ModelReplacementAPI");
            harmony.PatchAll();
            Logger.LogInfo($"Plugin {"meow.ModelReplacementAPI"} is loaded!");
        }
        public static ModelReplacementAPI Instance;
        public new ManualLogSource Logger;
        private static Dictionary<string, Type> RegisteredModelReplacements = new Dictionary<string, Type>();

        /// <summary>
        /// Registers a specified body replacement class to a specified suit name. All players wearing a suit with the specified name will have their model replaced. 
        /// </summary>
        /// <param name="suitNameToReplace"></param>
        /// <param name="type"></param>
        public static void RegisterSuitModelReplacement(string suitNameToReplace, Type type)
        {
            if (!(type.IsSubclassOf(typeof(BodyReplacementBase))))
            {
                Instance.Logger.LogError($"Cannot register body replacement type {type.Name}, must inherit from BodyReplacementBase");
                return;
            }
            if (RegisteredModelReplacements.ContainsKey(suitNameToReplace))
            {
                Instance.Logger.LogError($"Cannot register body replacement type {type.Name}, suit name to replace {suitNameToReplace} is already registered.");
                return;
            }

            Instance.Logger.LogInfo($"Registering body replacement type {type.Name} to suit name {suitNameToReplace}.");
            RegisteredModelReplacements.Add(suitNameToReplace, type);
        }

        /// <summary>
        /// Sets a body replacement for the specified player, removes existing body replacement if it is a different type than the specified
        /// </summary>
        /// <param name="player"></param>
        /// <param name="type">typeof body replacement class. Must inherit from BodyReplacementBase</param>
        public static void SetPlayerModelReplacement(PlayerControllerB player, Type type)
        {
            if (!(type.IsSubclassOf(typeof(BodyReplacementBase))))
            {
                Instance.Logger.LogError($"Cannot set body replacement of type {type.Name}, must inherit from BodyReplacementBase");
                return;
            }
            var a = player.thisPlayerBody.gameObject.GetComponent<BodyReplacementBase>();
            if (a != null)
            {
                if (a.GetType() == type)
                {
                    return; //No need to add a body replacement, there is already a body replacement of this type
                }
                if (a)
                {
                    Destroy(a); //Destroy the existing body replacement
                }
            }

            player.thisPlayerBody.gameObject.AddComponent(type);
        }
        /// <summary>
        /// Removes any existing body replacement 
        /// </summary>
        /// <param name="player"></param>
        public static void RemovePlayerModelReplacement(PlayerControllerB player)
        {
            var a = player.thisPlayerBody.gameObject.GetComponent<BodyReplacementBase>();
            if (a)
            {
                Destroy(a);
            }
        }


        [HarmonyPatch(typeof(UnlockableSuit))]
        internal class UnlockableSuitPatch
        {
            [HarmonyPatch("SwitchSuitForPlayer")]
            [HarmonyPostfix]
            private static void SwitchSuitModelReplacementPatch(PlayerControllerB player, int suitID, bool playAudio = true)
            {
                if (player.playerSteamId == 0) { return; }
                Console.WriteLine(string.Format("player change suit {0} suitID {1} ({2})", player.playerUsername, suitID, StartOfRound.Instance.unlockablesList.unlockables[suitID].unlockableName));

                string suitName = StartOfRound.Instance.unlockablesList.unlockables[suitID].unlockableName;

                if (RegisteredModelReplacements.ContainsKey(suitName))
                {
                    Type type = RegisteredModelReplacements[suitName];
                    SetPlayerModelReplacement(player, type);
                }
                else
                {
                    RemovePlayerModelReplacement(player);
                }

            }
        }





    }
}