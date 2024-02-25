using GameNetcodeStuff;
using MoreCompany.Cosmetics;
using System;
using System.Collections.Generic;
using UnityEngine;
using ModelReplacement.AvatarBodyUpdater;

namespace ModelReplacement.Modules
{
    public class MoreCompanyCosmeticManager
    {
        private BodyReplacementBase bodyReplacement;
        private PlayerControllerB controller;
        private GameObject replacementModel;
        private AvatarUpdater cosmeticAvatar => bodyReplacement.cosmeticAvatar;

        public MoreCompanyCosmeticManager(BodyReplacementBase bodyreplacement)
        {
            this.bodyReplacement = bodyreplacement;
            controller = bodyreplacement.controller;
            replacementModel = bodyreplacement.replacementModel;
        }


        public void Update(bool useAvatarTransforms)
        {
            if (ModelReplacementAPI.moreCompanyPresent) { SafeRenderCosmetics(useAvatarTransforms); }
        }

        private void SafeRenderCosmetics(bool useAvatarTransforms)
        {
            try
            {
                DangerousRenderCosmetics(useAvatarTransforms);
            }
            catch (Exception e)
            {
                //Console.WriteLine($"Exception in MoreCompanyRenderCosmetcs on {controller.name}  {e}");
            }
            
        }

        // Token: 0x02000034 RID: 52
        public enum CosmeticType2
        {
            // Token: 0x04000028 RID: 40
            HAT,
            // Token: 0x04000029 RID: 41
            WRIST,
            // Token: 0x0400002A RID: 42
            CHEST,
            // Token: 0x0400002B RID: 43
            R_LOWER_ARM,
            // Token: 0x0400002C RID: 44
            HIP,
            // Token: 0x0400002D RID: 45
            L_SHIN,
            // Token: 0x0400002E RID: 46
            R_SHIN
        }

        private Dictionary<CosmeticType2, Transform> cosmeticTransformPairs = new Dictionary<CosmeticType2, Transform>();
        private void RefreshCosmetics()
        {
            if (cosmeticTransformPairs.Count > 0) {
                cosmeticTransformPairs.Add(CosmeticType2.HAT, cosmeticAvatar.GetAvatarTransformFromBoneName("spine.004"));
                cosmeticTransformPairs.Add(CosmeticType2.CHEST, cosmeticAvatar.GetAvatarTransformFromBoneName("spine.003"));
                cosmeticTransformPairs.Add(CosmeticType2.R_LOWER_ARM, cosmeticAvatar.GetAvatarTransformFromBoneName("arm.R_lower"));
                cosmeticTransformPairs.Add(CosmeticType2.HIP, cosmeticAvatar.GetAvatarTransformFromBoneName("spine"));
                cosmeticTransformPairs.Add(CosmeticType2.L_SHIN, cosmeticAvatar.GetAvatarTransformFromBoneName("shin.L"));
                cosmeticTransformPairs.Add(CosmeticType2.R_SHIN, cosmeticAvatar.GetAvatarTransformFromBoneName("shin.R"));
            }
        }

        private void DangerousRenderCosmetics(bool useAvatarTransforms)
        {
            RefreshCosmetics();

            CosmeticApplication application = controller.gameObject.GetComponentInChildren<CosmeticApplication>();
            if (application == null) 
            { 
                return; 
            }

            if (useAvatarTransforms)
            {
                foreach (CosmeticInstance cosmeticInstance in application.spawnedCosmetics)
                {
                    Transform transform = GetCosmeticTransform(cosmeticInstance.cosmeticType);
                    if (transform != null)
                    {
                        ApplyTransformToCosmeticInstance(cosmeticInstance, transform);
                    }
                }
            }
            else
            {
                application.RefreshAllCosmeticPositions();
            }
        }

        private Transform GetCosmeticTransform(CosmeticType cosmeticType)
        {
            return cosmeticTransformPairs.TryGetValue((CosmeticType2)cosmeticType, out Transform transform) ? transform : throw new Exception($"Could not find the cosmetic transform");
        }

        private void ApplyTransformToCosmeticInstance(CosmeticInstance cosmeticInstance, Transform transform)
        {
            cosmeticInstance.transform.parent = null;
            cosmeticInstance.transform.SetPositionAndRotation(transform.position, transform.rotation);
            SetAllChildrenLayer(cosmeticInstance.transform, ViewStateManager.modelLayer);
        }

        private static void SetAllChildrenLayer(Transform transform, int layer)
        {
            transform.gameObject.layer = layer;
            foreach (Transform childTransform in transform)
            {
                SetAllChildrenLayer(childTransform, layer);
            }
        }
    }
}
