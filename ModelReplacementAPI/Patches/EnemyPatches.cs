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
            if (playerWhoHit == null) return;

            BodyReplacementBase bodyReplacement = playerWhoHit.gameObject.GetComponent<BodyReplacementBase>();
            if (bodyReplacement)
            {
                bodyReplacement.OnHitEnemy(__instance.isEnemyDead);
            }
        }

    }
    [HarmonyPatch(typeof(MaskedPlayerEnemy))]
    public class MaskedPlayerEnemyPatch
    {
        [HarmonyPatch("FinishKillAnimation")]
        [HarmonyPrefix]
        public static void SetModelReplacement(ref MaskedPlayerEnemy __instance, bool killedPlayer)
        {
            try
            {
                BodyReplacementBase bodyReplacement = __instance.mimickingPlayer.gameObject.GetComponent<BodyReplacementBase>();
                if (!bodyReplacement) return;

                bodyReplacement.CreateAndParentMasked(__instance);



            }
            catch
            {

            }
            
        }

    }
}
