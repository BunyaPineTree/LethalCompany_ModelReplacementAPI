using GameNetcodeStuff;
using HarmonyLib;
using ModelReplacement.Monobehaviors;

namespace ModelReplacement.Patches
{

    [HarmonyPatch(typeof(EnemyAI))]
    public class EnemyAIPatch
    {
        [HarmonyPatch("HitEnemy")]
        [HarmonyPrefix]

        [HarmonyAfter()]
        public static void HitEnemy(ref EnemyAI __instance, int force = 1, PlayerControllerB playerWhoHit = null, bool playHitSFX = false)
        {
            if (playerWhoHit == null) return;

            BodyReplacementBase bodyReplacement = playerWhoHit.gameObject.GetComponent<BodyReplacementBase>();
            if (bodyReplacement)
            {
                bodyReplacement.OnHitEnemy(__instance.isEnemyDead);
            }
        }

    }
}
