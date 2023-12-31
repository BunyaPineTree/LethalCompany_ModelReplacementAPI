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

        private CosmeticApplication application = null;
        private List<Tuple<CosmeticInstance, Transform>> cosmeticTransformPairs = new List<Tuple<CosmeticInstance, Transform>>();
        private void RefreshCosmetics()
        {
            if(application != null) { return; }
            CosmeticApplication[] applications = controller.gameObject.GetComponentsInChildren<CosmeticApplication>();
            if (applications.Any())
            {
                application = applications.First();
            }
            cosmeticTransformPairs.Clear();

            foreach (CosmeticInstance cosmeticInstance in application.spawnedCosmetics)
            {
                Transform transform = null;
                switch (cosmeticInstance.cosmeticType)
                {
                    case CosmeticType.HAT:
                        transform = cosmeticAvatar.GetAvatarTransformFromBoneName("spine.004");
                        break;
                    case CosmeticType.CHEST:
                        transform = cosmeticAvatar.GetAvatarTransformFromBoneName("spine.003");
                        break;
                    case CosmeticType.R_LOWER_ARM:
                        transform = cosmeticAvatar.GetAvatarTransformFromBoneName("arm.R_lower");
                        break;
                    case CosmeticType.HIP:
                        transform = cosmeticAvatar.GetAvatarTransformFromBoneName("spine");
                        break;
                    case CosmeticType.L_SHIN:
                        transform = cosmeticAvatar.GetAvatarTransformFromBoneName("shin.L");
                        break;
                    case CosmeticType.R_SHIN:
                        transform = cosmeticAvatar.GetAvatarTransformFromBoneName("shin.R");
                        break;
                }

                cosmeticTransformPairs.Add(new Tuple<CosmeticInstance,Transform>(cosmeticInstance, transform));

            }


        }

        private void DangerousRenderCosmetics(bool useAvatarTransforms)
        {
            RefreshCosmetics();

            if (useAvatarTransforms)
            {
                foreach (var pair in cosmeticTransformPairs)
                {
                    CosmeticInstance cosmeticInstance = pair.Item1;
                    Transform transform = pair.Item2;
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
