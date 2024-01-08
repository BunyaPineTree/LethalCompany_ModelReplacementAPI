using HarmonyLib;
using ModelReplacement;
using UnityEngine;

[HarmonyPatch(typeof(GrabbableObject))]
public class LocateHeldObjectsOnModelReplacementPatch
{

    [HarmonyPatch("LateUpdate")]
    [HarmonyPostfix]
    public static void LateUpdatePatch(ref GrabbableObject __instance)
    {
        if (__instance.parentObject == null || __instance.playerHeldBy == null) return;

        BodyReplacementBase bodyReplacement = __instance.playerHeldBy.gameObject.GetComponent<BodyReplacementBase>();
        if (!bodyReplacement) return;

        if (bodyReplacement.viewState.GetViewState() == ViewState.ThirdPerson)
        {
            Transform parentObject = bodyReplacement.avatar.ItemHolder;

            parentObject.localPosition = bodyReplacement.avatar.ItemHolderPositionOffset;
            Transform playerItemHolder = bodyReplacement.avatar.GetPlayerItemHolder();

            __instance.transform.rotation = playerItemHolder.rotation;
            __instance.transform.Rotate(__instance.itemProperties.rotationOffset);
            __instance.transform.position = parentObject.position;
            Vector3 vector = __instance.itemProperties.positionOffset;
            vector = playerItemHolder.rotation * vector;
            __instance.transform.position += vector;
        }
    }
}