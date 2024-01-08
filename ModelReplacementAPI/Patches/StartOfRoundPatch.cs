using HarmonyLib;
using ModelReplacement;

[HarmonyPatch(typeof(StartOfRound))]
public class RepairBrokenBodyReplacementsPatch
{

    [HarmonyPatch("ReviveDeadPlayers")]
    [HarmonyPostfix]
    public static void ReviveDeadPlayersPatch(ref StartOfRound __instance)
    {

        foreach (GameNetcodeStuff.PlayerControllerB item in __instance.allPlayerScripts)
        {
            if (!item.isPlayerDead) continue;
            ModelReplacementAPI.ResetPlayerModelReplacement(item);
        }
    }
}
