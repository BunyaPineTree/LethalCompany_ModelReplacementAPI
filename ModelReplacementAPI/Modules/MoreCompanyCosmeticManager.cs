using GameNetcodeStuff;
using MoreCompany.Cosmetics;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
            DangerousRenderCosmetics(useAvatarTransforms);
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
            if(cosmeticTransformPairs.Count != 0) { return; }


            cosmeticTransformPairs.Add(CosmeticType2.HAT, cosmeticAvatar.GetAvatarTransformFromBoneName("spine.004"));
            cosmeticTransformPairs.Add(CosmeticType2.CHEST, cosmeticAvatar.GetAvatarTransformFromBoneName("spine.003"));
            cosmeticTransformPairs.Add(CosmeticType2.R_LOWER_ARM, cosmeticAvatar.GetAvatarTransformFromBoneName("arm.R_lower"));
            cosmeticTransformPairs.Add(CosmeticType2.HIP, cosmeticAvatar.GetAvatarTransformFromBoneName("spine"));
            cosmeticTransformPairs.Add(CosmeticType2.L_SHIN, cosmeticAvatar.GetAvatarTransformFromBoneName("shin.L"));
            cosmeticTransformPairs.Add(CosmeticType2.R_SHIN, cosmeticAvatar.GetAvatarTransformFromBoneName("shin.R"));


        }

        private void DangerousRenderCosmetics(bool useAvatarTransforms)
        {
            RefreshCosmetics();

            var application = controller.gameObject.GetComponentInChildren<CosmeticApplication>();
            if(application == null) { return; }
            if (useAvatarTransforms)
            {
                foreach (CosmeticInstance cosmeticInstance in application.spawnedCosmetics)
                {
                    Transform transform = null;
                    switch (cosmeticInstance.cosmeticType)
                    {
                        case CosmeticType.HAT:
                            transform = cosmeticTransformPairs[CosmeticType2.HAT];
                            break;
                        case CosmeticType.CHEST:
                            transform = cosmeticTransformPairs[CosmeticType2.CHEST];
                            break;
                        case CosmeticType.R_LOWER_ARM:
                            transform = cosmeticTransformPairs[CosmeticType2.R_LOWER_ARM];
                            break;
                        case CosmeticType.HIP:
                            transform = cosmeticTransformPairs[CosmeticType2.HIP];
                            break;
                        case CosmeticType.L_SHIN:
                            transform = cosmeticTransformPairs[CosmeticType2.L_SHIN];
                            break;
                        case CosmeticType.R_SHIN:
                            transform = cosmeticTransformPairs[CosmeticType2.R_SHIN];
                            break;
                    }
                    cosmeticInstance.transform.parent = null;
                    cosmeticInstance.transform.SetPositionAndRotation(transform.position, transform.rotation);

                }
            }
            else
            {
                application.RefreshAllCosmeticPositions();
            }

        }

    }
}
