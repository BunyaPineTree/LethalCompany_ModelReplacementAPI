﻿using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Logging;
using GameNetcodeStuff;
using HarmonyLib;
using ModelReplacement.AvatarBodyUpdater;
using System;
using System.Collections.Generic;
using UnityEngine;
using System.Reflection;
using ModelReplacement;
using BepInEx.Configuration;

using UnityEngine.TextCore.Text;
using static UnityEngine.ParticleSystem.PlaybackState;
using UnityEngine.Rendering;
//using System.Numerics;
//using static System.Net.Mime.MediaTypeNames;
//using System.Numerics;

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



        public static ModelReplacementAPI Instance;
        public new ManualLogSource Logger;

        //Other
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
            if (!(type.IsSubclassOf(typeof(BodyReplacementBase))))
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
            if (!(type.IsSubclassOf(typeof(BodyReplacementBase))))
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
            if (!(type.IsSubclassOf(typeof(BodyReplacementBase))))
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
            if (!(type.IsSubclassOf(typeof(BodyReplacementBase))))
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
            if (!(type.IsSubclassOf(typeof(BodyReplacementBase))))
            {
                Instance.Logger.LogError($"Cannot set body replacement of type {type.Name}, must inherit from BodyReplacementBase");
                return;
            }
            if (!isLan && (player.playerSteamId == 0))
            {
                return;
            }
            var a = player.thisPlayerBody.gameObject.GetComponent<BodyReplacementBase>();
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

            var replacecment = player.thisPlayerBody.gameObject.AddComponent(type) as BodyReplacementBase;
            replacecment.suitName = suitName;
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
        #endregion

        [HarmonyPatch(typeof(GrabbableObject))]
        public class LocateHeldObjectsOnModelReplacementPatch
        {

            [HarmonyPatch("LateUpdate")]
            [HarmonyPostfix]
            public static void LateUpdatePatch(ref GrabbableObject __instance)
            {
                if (__instance.parentObject == null) { return; }
                if (__instance.playerHeldBy == null) { return; }
                var a = __instance.playerHeldBy.gameObject.GetComponent<BodyReplacementBase>();
                if (a == null) { return; }
                if (a.GetViewState() == ViewState.ThirdPerson)
                {
                    Transform parentObject = a.avatar.itemHolder;

                    parentObject.localPosition = a.avatar.itemHolderPositionOffset;
                    Transform playerItemHolder = a.avatar.GetPlayerItemHolder();

                    __instance.transform.rotation = playerItemHolder.rotation;
                    __instance.transform.Rotate(__instance.itemProperties.rotationOffset);
                    __instance.transform.position = parentObject.position;
                    Vector3 vector = __instance.itemProperties.positionOffset;
                    vector = playerItemHolder.rotation * vector;
                    __instance.transform.position += vector;

                }

            }
        }



        [HarmonyPatch(typeof(StartOfRound))]
        public class RepairBrokenBodyReplacementsPatch
        {

            [HarmonyPatch("ReviveDeadPlayers")]
            [HarmonyPostfix]
            public static void ReviveDeadPlayersPatch(ref StartOfRound __instance)
            {

                foreach (var item in __instance.allPlayerScripts)
                {
                    if (!item.isPlayerDead) { continue; } //player isn't dead
                    if (item.gameObject.GetComponent<BodyReplacementBase>() == null) { continue; } //player doesn't have a body replacement

                    Console.WriteLine($"Reinstantiating model replacement for {item.playerUsername} ");
                    Type BodyReplacementType = item.gameObject.GetComponent<BodyReplacementBase>().GetType();
                    Destroy(item.gameObject.GetComponent<BodyReplacementBase>());
                    item.gameObject.AddComponent(BodyReplacementType);
                }
            }
        }



        [HarmonyPatch(typeof(PlayerControllerB))]
        public class PlayerControllerBPatch
        {

            [HarmonyPatch("Update")]
            [HarmonyPostfix]
            public static void ManageRegistryBodyReplacements(ref PlayerControllerB __instance)
            {
                try
                {
                    var a = __instance.thisPlayerBody.gameObject.GetComponent<BodyReplacementBase>();
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

            [HarmonyPatch("DamagePlayerFromOtherClientClientRpc")]
            [HarmonyPrefix]
            public static void DamagePlayerFromOtherClientClientRpc(ref PlayerControllerB __instance, int damageAmount, Vector3 hitDirection, int playerWhoHit, int newHealthAmount)
            {
                PlayerControllerB _playerWhoHit = __instance.playersManager.allPlayerScripts[playerWhoHit];
                if (_playerWhoHit == null)
                {
                    return;
                }
                var a = _playerWhoHit.thisPlayerBody.gameObject.GetComponent<BodyReplacementBase>();
                if (a) { a.OnHitAlly(__instance, __instance.isPlayerDead); }
            }

            [HarmonyPatch("DamagePlayerClientRpc")]
            [HarmonyPrefix]
            public static void DamagePlayerClientRpc(ref PlayerControllerB __instance, int damageNumber, int newHealthAmount)
            {
                if (__instance == null)
                {
                    return;
                }
                var a = __instance.thisPlayerBody.gameObject.GetComponent<BodyReplacementBase>();
                Console.WriteLine($"PLAYER TAKE DAMAGE {__instance.playerUsername}");
                if (a) { a.OnDamageTaken(__instance.isPlayerDead); }
            }

        }

        [HarmonyPatch(typeof(EnemyAI))]
        public class EnemyAIPatch
        {
            [HarmonyPatch("HitEnemy")]
            [HarmonyPrefix]

            [HarmonyAfter()]
            public static void HitEnemy(ref EnemyAI __instance, int force = 1, PlayerControllerB playerWhoHit = null, bool playHitSFX = false)
            {
                if (playerWhoHit == null)
                {
                    return;
                }

                var a = playerWhoHit.thisPlayerBody.gameObject.GetComponent<BodyReplacementBase>();
                if (a) { a.OnHitEnemy(__instance.isEnemyDead); }
            }

        }
        [HarmonyPatch(typeof(MaskedPlayerEnemy))]
        public class MaskedPlayerEnemyPatch
        {
            [HarmonyPatch("SetSuit")]
            [HarmonyPrefix]
            public static void SetModelReplacement(ref MaskedPlayerEnemy __instance, int suitId)
            {
                var a = __instance.mimickingPlayer.thisPlayerBody.gameObject.GetComponent<BodyReplacementBase>();
                if(a == null) { return; }

            }

        }

        [HarmonyPatch(typeof(PlayerControllerB), "SpawnPlayerAnimation")]
        internal class PlayerPatch
        {
            // Token: 0x0600000D RID: 13 RVA: 0x00002370 File Offset: 0x00000570
            [HarmonyAfter(new string[] { "quackandcheese.mirrordecor" })]
            private static void Postfix(ref PlayerControllerB __instance)
            {
                if (__instance == GameNetworkManager.Instance.localPlayerController)
                {
                    PlayerControllerB playerControllerB = __instance;
                    playerControllerB.thisPlayerModel.shadowCastingMode = ShadowCastingMode.On;
                    playerControllerB.thisPlayerModel.gameObject.layer = BodyReplacementBase.modelLayer;
                    playerControllerB.thisPlayerModelArms.gameObject.layer = BodyReplacementBase.armsLayer;
                    playerControllerB.gameplayCamera.cullingMask = BodyReplacementBase.CullingMaskFirstPerson;
                }
            }
        }
    }
}