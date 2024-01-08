using GameNetcodeStuff;
using HarmonyLib;

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
            if (playerWhoHit == null)
            {
                return;
            }

            BodyReplacementBase a = playerWhoHit.gameObject.GetComponent<BodyReplacementBase>();
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
            BodyReplacementBase a = __instance.mimickingPlayer.gameObject.GetComponent<BodyReplacementBase>();
            if (a == null) { return; }

        }

    }
}
