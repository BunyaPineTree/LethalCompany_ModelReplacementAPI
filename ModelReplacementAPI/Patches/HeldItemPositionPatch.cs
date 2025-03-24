using HarmonyLib;
using ModelReplacement.Monobehaviors;
using UnityEngine;

namespace ModelReplacement.Patches
{
    [HarmonyPatch(typeof(GrabbableObject))]
    public class LocateHeldObjectsOnModelReplacementPatch
    {

        [HarmonyPatch("LateUpdate")]
        [HarmonyPostfix]
        public static void LateUpdatePatch(ref GrabbableObject __instance)
        {
            if (__instance.parentObject == null || __instance.playerHeldBy == null) return;
            if (__instance.playerHeldBy.ItemSlots[__instance.playerHeldBy.currentItemSlot] != __instance) return;

            BodyReplacementBase bodyReplacement = __instance.playerHeldBy.gameObject.GetComponent<BodyReplacementBase>();
            if (!bodyReplacement) return;

            if (bodyReplacement.heldItem != __instance)
                bodyReplacement.heldItem = __instance;
        }
    }
}