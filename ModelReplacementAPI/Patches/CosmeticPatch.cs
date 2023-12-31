using BepInEx.Bootstrap;
using HarmonyLib;
using MoreCompany.Cosmetics;
using UnityEngine;

namespace ModelReplacement.Patches
{
    // Token: 0x02000005 RID: 5
    [HarmonyPatch(typeof(HUDManager), "AddPlayerChatMessageClientRpc")]
    internal class CosmeticPatch
    {
        // Token: 0x06000005 RID: 5 RVA: 0x00002100 File Offset: 0x00000300
        private static void Postfix()
        {
            if (Chainloader.PluginInfos.ContainsKey("me.swipez.melonloader.morecompany"))
            {
                CosmeticApplication cosmeticApplication = UnityEngine.Object.FindObjectOfType<CosmeticApplication>();
                if (CosmeticRegistry.locallySelectedCosmetics.Count > 0 && cosmeticApplication.spawnedCosmetics.Count <= 0)
                {
                    foreach (string text in CosmeticRegistry.locallySelectedCosmetics)
                    {
                        cosmeticApplication.ApplyCosmetic(text, true);
                    }
                    foreach (CosmeticInstance cosmeticInstance in cosmeticApplication.spawnedCosmetics)
                    {
                        cosmeticInstance.transform.localScale *= 0.38f;
                        SetAllChildrenLayer(cosmeticInstance.transform, 3);
                    }
                }
            }
        }

        // Token: 0x06000006 RID: 6 RVA: 0x000021F0 File Offset: 0x000003F0
        private static void SetAllChildrenLayer(Transform transform, string layerName)
        {
            int num = LayerMask.NameToLayer(layerName);
            transform.gameObject.layer = num;
            foreach (object obj in transform)
            {
                SetAllChildrenLayer((Transform)obj, layerName);
            }
        }

        // Token: 0x06000007 RID: 7 RVA: 0x00002258 File Offset: 0x00000458
        private static void SetAllChildrenLayer(Transform transform, int layer)
        {
            transform.gameObject.layer = layer;
            foreach (object obj in transform)
            {
                SetAllChildrenLayer((Transform)obj, layer);
            }
        }
    }
}
