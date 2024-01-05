using HarmonyLib;
using ModelReplacement;
using ModelReplacement.Modules;
using System;

[HarmonyPatch(typeof(StartOfRound))]
public class RepairBrokenBodyReplacementsPatch
{

    [HarmonyPatch("ReviveDeadPlayers")]
    [HarmonyPostfix]
    public static void ReviveDeadPlayersPatch(ref StartOfRound __instance)
    {

        foreach (GameNetcodeStuff.PlayerControllerB item in __instance.allPlayerScripts)
        {
            if (!item.isPlayerDead) { continue; } //player isn't dead
            ModelReplacementAPI.ResetPlayerModelReplacement(item);
        }
    }


}
