using HarmonyLib;
using ModelReplacement.Monobehaviors;
using UnityEngine;

namespace ModelReplacement.Patches
{
    [HarmonyPatch(typeof(GrabbableObject))]
    public class LocateHeldObjectsOnModelReplacementPatch
    {

        [HarmonyPatch("LateUpdate")]
        [HarmonyPrefix]
        public static bool LateUpdatePatch(ref GrabbableObject __instance)
        {
            if (__instance.parentObject == null || __instance.playerHeldBy == null) return true;
            if (__instance.playerHeldBy.ItemSlots[__instance.playerHeldBy.currentItemSlot] != __instance) return true;
            BodyReplacementBase bodyReplacement = __instance.playerHeldBy.gameObject.GetComponent<BodyReplacementBase>();
            if (!bodyReplacement) return true;
            if(bodyReplacement.viewState.GetViewState() != ViewState.FirstPerson)
            {
                bodyReplacement.heldItem = null;
                return true;
            }
            if (!bodyReplacement.CanPositionItemOnCustomViewModel)
            {
                bodyReplacement.heldItem = null;
                return true;
            }

            bodyReplacement.heldItem = __instance;
            //bodyReplacement.UpdateItemTransform();
            return false;

        }
    }
}