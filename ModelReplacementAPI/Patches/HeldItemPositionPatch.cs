using HarmonyLib;
using ModelReplacement;
using ModelReplacement.Modules;
using UnityEngine;

[HarmonyPatch(typeof(GrabbableObject))]
public class LocateHeldObjectsOnModelReplacementPatch
{

    [HarmonyPatch("LateUpdate")]
    [HarmonyPostfix]
    public static void LateUpdatePatch(ref GrabbableObject __instance)
    {
        if (__instance.parentObject == null) { return; }
        if (__instance.playerHeldBy == null) { return; }
        BodyReplacementBase a = __instance.playerHeldBy.gameObject.GetComponent<BodyReplacementBase>();
        if (a == null) { return; }
        if (a.viewState.GetViewState() == ViewState.ThirdPerson)
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