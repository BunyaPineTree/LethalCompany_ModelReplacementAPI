using GameNetcodeStuff;
using HarmonyLib;
using System;
using UnityEngine.Rendering;
using UnityEngine;
using LCThirdPerson;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine.Events;

namespace ModelReplacement.Patches
{
    [HarmonyPatch(typeof(PlayerControllerB))]
    public class PlayerControllerBPatch
    {
        [HarmonyPatch("Start")]
        [HarmonyPrefix]
        public static void StartPatch(ref PlayerControllerB __instance)
        {
            __instance.gameObject.AddComponent<ViewStateManager>();
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
            BodyReplacementBase a = _playerWhoHit.gameObject.GetComponent<BodyReplacementBase>();
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
            BodyReplacementBase a = __instance.gameObject.GetComponent<BodyReplacementBase>();
   
            if (a) { a.OnDamageTaken(__instance.isPlayerDead); }
        }


        [HarmonyPatch("SetHoverTipAndCurrentInteractTrigger")]
        [HarmonyPostfix]
        public static void SetHoverTipAndCurrentInteractTriggerPatch(ref PlayerControllerB __instance)
        {
            if (!__instance.isGrabbingObjectAnimation)
            {
                RaycastHit hit;
                if (!__instance.isFreeCamera && Physics.Raycast(__instance.interactRay, out hit, 5, 8388608))
                {
                    var component3 = hit.collider.gameObject.GetComponent<BodyReplacementBase.RaycastTarget>();
                    if (component3 != null)
                    {
                        component3.controller.ShowNameBillboard();
                    }
                }

            }

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
                GameObject.Find("Systems/Rendering/PlayerHUDHelmetModel/").GetComponentInChildren<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
                PlayerControllerB playerControllerB = __instance;
                playerControllerB.thisPlayerModel.shadowCastingMode = ShadowCastingMode.On;
                playerControllerB.thisPlayerModel.gameObject.layer = ViewStateManager.modelLayer;
                playerControllerB.thisPlayerModelLOD1.gameObject.layer = ViewStateManager.modelLayer;
                playerControllerB.thisPlayerModelLOD2.gameObject.layer = ViewStateManager.modelLayer;
                playerControllerB.thisPlayerModelArms.gameObject.layer = ViewStateManager.armsLayer;
                playerControllerB.gameplayCamera.cullingMask = ViewStateManager.CullingMaskFirstPerson;
            }
        }

    }
}

