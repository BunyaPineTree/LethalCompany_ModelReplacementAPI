using GameNetcodeStuff;
using HarmonyLib;
using System;
using UnityEngine.Rendering;
using UnityEngine;
using ModelReplacement.Modules;

namespace ModelReplacement.Patches
{
    [HarmonyPatch(typeof(PlayerControllerB), "SpawnPlayerAnimation")]
    internal class PlayerPatch
    {
        [HarmonyPatch(typeof(PlayerControllerB))]
        public class PlayerControllerBPatch
        {
            [HarmonyPatch("DamagePlayerFromOtherClientClientRpc")]
            [HarmonyPrefix]
            public static void DamagePlayerFromOtherClientClientRpc(ref PlayerControllerB __instance, int damageAmount, Vector3 hitDirection, int playerWhoHit, int newHealthAmount)
            {
                PlayerControllerB _playerWhoHit = __instance.playersManager.allPlayerScripts[playerWhoHit];
                if (_playerWhoHit == null)
                {
                    return;
                }
                BodyReplacementBase a = _playerWhoHit.thisPlayerBody.gameObject.GetComponent<BodyReplacementBase>();
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
                BodyReplacementBase a = __instance.thisPlayerBody.gameObject.GetComponent<BodyReplacementBase>();
                Console.WriteLine($"PLAYER TAKE DAMAGE {__instance.playerUsername}");
                if (a) { a.OnDamageTaken(__instance.isPlayerDead); }
            }

            /*
            [HarmonyPatch("SetHoverTipAndCurrentInteractTrigger")]
            [HarmonyPostfix]
            public static void SetHoverTipAndCurrentInteractTriggerPatch(ref PlayerControllerB __instance)
            {
                if (!__instance.isGrabbingObjectAnimation)
                {
                    if (!__instance.isFreeCamera && Physics.Raycast(__instance.interactRay, out __instance.hit, 5f))
                    {
                        PlayerControllerB component3 = this.hit.collider.gameObject.GetComponent<PlayerControllerB>();
                        if (component3 != null)
                        {
                            component3.ShowNameBillboard();
                        }
                    }

                }

            }
            */






        }

        // Token: 0x0600000D RID: 13 RVA: 0x00002370 File Offset: 0x00000570
        [HarmonyAfter(new string[] { "quackandcheese.mirrordecor" })]
        private static void Postfix(ref PlayerControllerB __instance)
        {
            if (__instance == GameNetworkManager.Instance.localPlayerController)
            {
                PlayerControllerB playerControllerB = __instance;
                playerControllerB.thisPlayerModel.shadowCastingMode = ShadowCastingMode.On;
                playerControllerB.thisPlayerModel.gameObject.layer = ViewStateManager.modelLayer;
                playerControllerB.thisPlayerModelArms.gameObject.layer = ViewStateManager.armsLayer;
                playerControllerB.gameplayCamera.cullingMask = ViewStateManager.CullingMaskFirstPerson;
            }
        }
    }
}
