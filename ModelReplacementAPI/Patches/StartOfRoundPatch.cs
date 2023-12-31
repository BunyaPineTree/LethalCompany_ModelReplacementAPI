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
            if (item.gameObject.GetComponent<BodyReplacementBase>() == null) { continue; } //player doesn't have a body replacement

            Console.WriteLine($"Reinstantiating model replacement for {item.playerUsername} ");
            Type BodyReplacementType = item.gameObject.GetComponent<BodyReplacementBase>().GetType();
            UnityEngine.Object.Destroy(item.gameObject.GetComponent<BodyReplacementBase>());
            item.gameObject.AddComponent(BodyReplacementType);
        }
    }


}
