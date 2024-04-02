using GameNetcodeStuff;
using MoreCompany.Cosmetics;
using System;
using System.Collections.Generic;
using UnityEngine;
using ModelReplacement.Scripts;
using ModelReplacement.Scripts.Player;
using ModelReplacement.AvatarBodyUpdater;

namespace ModelReplacement.Monobehaviors
{
    public class MoreCompanyCosmeticManager : ManagerBase
    {
        private BodyReplacementBase bodyReplacement;
        private PlayerControllerB controller;
        private AvatarUpdater cosmeticAvatar => bodyReplacement.cosmeticAvatar;

        private string lastException = "";


        public override void UpdatePlayer()
        {
            if (ModelReplacementAPI.moreCompanyPresent) { SafeRenderCosmetics(false); }
        }

        public override void UpdateModelReplacement()
        {
            if (ModelReplacementAPI.moreCompanyPresent) { SafeRenderCosmetics(true); }
        }

        private void SafeRenderCosmetics(bool useAvatarTransforms)
        {
            try
            {
                DangerousRenderCosmetics(useAvatarTransforms);
            }
            catch (Exception e)
            {
                lastException = e.Message;
            }

        }

        private Dictionary<CosmeticType2, Transform> cosmeticTransformPairs = new Dictionary<CosmeticType2, Transform>();
        private List<CosmeticInstance2> cosmeticInstances = new List<CosmeticInstance2>();
        private void RefreshCosmetics()
        {
            if (cosmeticTransformPairs.Count == 0)
            {
                cosmeticTransformPairs.Add(CosmeticType2.HAT, cosmeticAvatar.GetAvatarTransformFromBoneName("spine.004"));
                cosmeticTransformPairs.Add(CosmeticType2.CHEST, cosmeticAvatar.GetAvatarTransformFromBoneName("spine.003"));
                cosmeticTransformPairs.Add(CosmeticType2.R_LOWER_ARM, cosmeticAvatar.GetAvatarTransformFromBoneName("arm.R_lower"));
                cosmeticTransformPairs.Add(CosmeticType2.HIP, cosmeticAvatar.GetAvatarTransformFromBoneName("spine"));
                cosmeticTransformPairs.Add(CosmeticType2.L_SHIN, cosmeticAvatar.GetAvatarTransformFromBoneName("shin.L"));
                cosmeticTransformPairs.Add(CosmeticType2.R_SHIN, cosmeticAvatar.GetAvatarTransformFromBoneName("shin.R"));
            }

            if (cosmeticInstances.Count == 0)
            {
                CosmeticApplication application = controller.gameObject.GetComponentInChildren<CosmeticApplication>();
                if (application == null)
                {
                    return;
                }
                foreach (CosmeticInstance cosmeticInstance in application.spawnedCosmetics)
                {
                    Transform transform = GetCosmeticTransform(cosmeticInstance.cosmeticType);

                    CosmeticInstance2 instance2 = new CosmeticInstance2();
                    instance2.modelParent = transform;
                    instance2.cosmetic = cosmeticInstance.gameObject;
                    instance2.DoRender = false;
                    if (transform)
                    {
                        var MMCOffset = transform.GetComponent<RotationOffset>();
                        if (MMCOffset)
                        {
                            var offset = instance2.modelOffset = new Transform();
                            instance2.DoRender = MMCOffset.RenderMCC;
                            offset.position = MMCOffset.MCCPosition;
                            offset.rotation = MMCOffset.MCCRotation;
                            offset.localScale = MMCOffset.MCCScale;
                        }
                    }
                }

            }
        }

        private void DangerousRenderCosmetics(bool useAvatarTransforms)
        {
            RefreshCosmetics();

            if (useAvatarTransforms)
            {
                foreach (CosmeticInstance2 cosmeticInstance in cosmeticInstances)
                {
                    if (cosmeticInstance.DoRender)
                    {
                        cosmeticInstance.cosmetic.SetActive(true);
                        cosmeticInstance.cosmetic.transform.parent = null;
                        cosmeticInstance.cosmetic.transform.localScale = cosmeticInstance.modelOffset.localScale;

                        Vector3 cosmeticPosition = cosmeticInstance.modelParent.transform.position + cosmeticInstance.modelOffset.position;
                        Quaternion cosmeticRotation = cosmeticInstance.modelParent.transform.rotation * cosmeticInstance.modelOffset.rotation;


                        cosmeticInstance.cosmetic.transform.SetPositionAndRotation(cosmeticPosition, cosmeticRotation);
                        SetAllChildrenLayer(cosmeticInstance.cosmetic.transform, ViewStateManager.modelLayer);
                    }
                    else
                    {
                        cosmeticInstance.cosmetic.SetActive(false);
                    }
                }
            }
            else
            {
                CosmeticApplication application = controller.gameObject.GetComponentInChildren<CosmeticApplication>();
                application.RefreshAllCosmeticPositions();
                foreach (CosmeticInstance cosmeticInstance in application.spawnedCosmetics)
                {
                    cosmeticInstance.transform.localScale = Vector3.one;
                    cosmeticInstance.gameObject.SetActive(true);
                }
            }
        }

        private Transform GetCosmeticTransform(CosmeticType cosmeticType)
        {
            return cosmeticTransformPairs.TryGetValue((CosmeticType2)cosmeticType, out Transform transform) ? transform : null;
        }

        private static void SetAllChildrenLayer(Transform transform, int layer)
        {
            transform.gameObject.layer = layer;
            foreach (Transform childTransform in transform)
            {
                SetAllChildrenLayer(childTransform, layer);
            }
        }

        internal class CosmeticInstance2
        {
            public Transform modelParent = null;
            public GameObject cosmetic = null;

            public Transform modelOffset = null;
            public bool DoRender = false;

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


    }
}
