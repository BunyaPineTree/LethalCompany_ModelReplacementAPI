using GameNetcodeStuff;
using HarmonyLib;
using UnityEngine.Rendering;
using UnityEngine;
using ModelReplacement.Monobehaviors;

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
            __instance.gameObject.AddComponent<MoreCompanyCosmeticManager>();

            __instance.gameObject.transform.Find("ScavengerModel").GetComponent<LODGroup>().enabled = false;
        }


        [HarmonyPatch("DamagePlayerFromOtherClientClientRpc")]
        [HarmonyPostfix]
        public static void DamagePlayerFromOtherClientClientRpc(ref PlayerControllerB __instance, int damageAmount, Vector3 hitDirection, int playerWhoHit, int newHealthAmount)
        {
            PlayerControllerB _playerWhoHit = __instance.playersManager.allPlayerScripts[playerWhoHit];
            if (!_playerWhoHit) return;

            BodyReplacementBase bodyReplacement = _playerWhoHit.gameObject.GetComponent<BodyReplacementBase>();
            if (bodyReplacement)
            {
                bodyReplacement.OnHitAlly(__instance, __instance.isPlayerDead);
            }
        }

        [HarmonyPatch("DamagePlayerClientRpc")]
        [HarmonyPostfix]
        public static void DamagePlayerClientRpc(ref PlayerControllerB __instance, int damageNumber, int newHealthAmount)
        {
            if (!__instance) return;

            BodyReplacementBase bodyReplacement = __instance.gameObject.GetComponent<BodyReplacementBase>();
            if (bodyReplacement)
            {
                bodyReplacement.OnDamageTaken(__instance.isPlayerDead);
            }
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
                    BodyReplacementBase.RaycastTarget rayTarget = hit.collider.gameObject.GetComponent<BodyReplacementBase.RaycastTarget>();
                    if (!rayTarget) return;
                    rayTarget.controller.ShowNameBillboard();
                }
            }
        }
        [HarmonyPatch("SpawnDeadBody")]
        [HarmonyPostfix]
        public static void SpawnDeadBody(int playerId, Vector3 bodyVelocity, int causeOfDeath, PlayerControllerB deadPlayerController, int deathAnimation = 0, Transform overridePosition = null)
        {
            var renderers = deadPlayerController.deadBody.gameObject.GetComponentsInChildren<SkinnedMeshRenderer>();
            foreach (var renderer in renderers)
            {
                renderer.gameObject.layer = ViewStateManager.visibleLayer;
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
                playerControllerB.thisPlayerModelLOD1.gameObject.layer = ViewStateManager.visibleLayer;
                playerControllerB.thisPlayerModelLOD2.gameObject.layer = ViewStateManager.visibleLayer;
                playerControllerB.thisPlayerModelArms.gameObject.layer = ViewStateManager.armsLayer;
                playerControllerB.gameplayCamera.cullingMask = ViewStateManager.CullingMaskFirstPerson;
            }
        }

    }
}

