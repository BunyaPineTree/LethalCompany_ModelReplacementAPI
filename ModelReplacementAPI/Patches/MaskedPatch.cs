using HarmonyLib;
using ModelReplacement.Monobehaviors.Enemies;
using System;
using System.Collections.Generic;
using System.Text;
using static UnityEngine.UIElements.UIR.Implementation.UIRStylePainter;

namespace ModelReplacement.Patches
{
    [HarmonyPatch(typeof(MaskedPlayerEnemy))]
    public class MaskedPlayerEnemyPatch
    {

        [HarmonyPatch("Start")]
        [HarmonyPostfix]
        public static void ManageRegistryBodyReplacements(ref EnemyAI __instance)
        {
            try
            {
                __instance.gameObject.AddComponent<MaskedReplacementBase>();
            }
            catch (Exception e)
            {
                ModelReplacementAPI.Instance.Logger.LogError($"Exception in Masked ManageRegistryBodyReplacements: {e}");
            }
        }

    }
}
