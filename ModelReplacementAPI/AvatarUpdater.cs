using GameNetcodeStuff;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace ModelReplacement.AvatarBodyUpdater
{

    public class AvatarUpdater
    {
        private SkinnedMeshRenderer playerModelRenderer = null;
        private Animator replacementAnimator = null;

        public Vector3 itemHolderPositionOffset { get; private set; } = Vector3.zero;
        public Quaternion itemHolderRotationOffset { get; private set; } = Quaternion.identity;
        public Transform itemHolderTransform { get; private set; } = null;

        bool hasUpperChest = false;


        public void AssignModelReplacement(GameObject player ,GameObject replacement)
        {
            var controller = player.GetComponent<PlayerControllerB>();
            if (controller)
            {
                playerModelRenderer = controller.thisPlayerModel;
            }
            else
            {
                playerModelRenderer = player.GetComponentInChildren<SkinnedMeshRenderer>();
            }

            if (playerModelRenderer == null)
            {
                Console.WriteLine("failed to start AvatarBodyUpdater");
                return;
            }

            replacementAnimator = replacement.GetComponentInChildren<Animator>();

            itemHolderTransform = replacementAnimator.GetBoneTransform(HumanBodyBones.RightHand);
            var ite = replacementAnimator.gameObject.GetComponent<ItemOffset>();
            itemHolderPositionOffset = ite.pOffset;
            itemHolderRotationOffset = ite.rOffset;

            Transform upperChestTransform = replacementAnimator.GetBoneTransform(HumanBodyBones.UpperChest);
            hasUpperChest = (upperChestTransform != null);
        }


        public bool CanUpdateModel()
        {
            if (playerModelRenderer == null) { return false; }
            if (replacementAnimator == null) { return false; }
            return true;
        }
        public void UpdateModel()
        {
            if (!CanUpdateModel()) { return; }

            foreach(Transform playerBone in playerModelRenderer.bones)
            {
                Transform modelBone = GetAvatarTransformFromBoneName(playerBone.name);
                if(modelBone == null) { continue; }

                modelBone.rotation = playerBone.rotation;
                var offset = modelBone.GetComponent<RotationOffset>();
                if (offset) { modelBone.rotation *= offset.offset; }

                if(playerBone.name == "spine")
                {
                    modelBone.position = playerBone.position;
                    var offset2 = modelBone.GetComponent<TranslationOffset>();
                    if (offset2) { modelBone.position += offset2.offset/50; }

                }
            }

        }

        public Transform GetAvatarTransformFromBoneName(string boneName)
        {
            //Special logic is required here. The player model has 5 central bones.
            // Spine, spine.001, spine.002, spine.003,   spine.004, corresponding to 
            // Hips   Spine      Chest      UpperChest   Head
            //However spine.002 practically doesn't move, and I wish to support mods that don't have an UpperChest bone. 
            //If they don't have an upperchest bone, instead map spine.003 to the Chest transform on the replacement model.
            if (boneName == "spine.002")
            {
                if (hasUpperChest) { return replacementAnimator.GetBoneTransform(HumanBodyBones.Chest); }
                else { return null; }
            }
            if (boneName == "spine.003")
            {
                if (hasUpperChest) { return replacementAnimator.GetBoneTransform(HumanBodyBones.UpperChest); }
                else { return replacementAnimator.GetBoneTransform(HumanBodyBones.Chest); }
            }
            if (modelToAvatarBone.ContainsKey(boneName))
            {
                return replacementAnimator.GetBoneTransform(modelToAvatarBone[boneName]);
            }
            return null;
        }

        //Remove spine.002 and .003 to implement logic
        private static Dictionary<string, HumanBodyBones> modelToAvatarBone = new Dictionary<string, HumanBodyBones>()
            {
                {"spine" , HumanBodyBones.Hips},
                {"spine.001" , HumanBodyBones.Spine},

                // {"spine.002" , HumanBodyBones.Chest},
                //{"spine.003" , HumanBodyBones.UpperChest},

                {"shoulder.L" , HumanBodyBones.LeftShoulder},
                {"arm.L_upper" , HumanBodyBones.LeftUpperArm},
                {"arm.L_lower" , HumanBodyBones.LeftLowerArm},
                {"hand.L" , HumanBodyBones.LeftHand},
                {"finger5.L" , HumanBodyBones.LeftLittleProximal},
                {"finger5.L.001" , HumanBodyBones.LeftLittleIntermediate},
                {"finger4.L" , HumanBodyBones.LeftRingProximal},
                {"finger4.L.001" , HumanBodyBones.LeftRingIntermediate},
                {"finger3.L" , HumanBodyBones.LeftMiddleProximal},
                {"finger3.L.001" , HumanBodyBones.LeftMiddleIntermediate},
                {"finger2.L" , HumanBodyBones.LeftIndexProximal},
                {"finger2.L.001" , HumanBodyBones.LeftIndexIntermediate},
                {"finger1.L" , HumanBodyBones.LeftThumbProximal},
                {"finger1.L.001" , HumanBodyBones.LeftThumbDistal},

                {"shoulder.R" , HumanBodyBones.RightShoulder},
                {"arm.R_upper" , HumanBodyBones.RightUpperArm},
                {"arm.R_lower" , HumanBodyBones.RightLowerArm},
                {"hand.R" , HumanBodyBones.RightHand},
                {"finger5.R" , HumanBodyBones.RightLittleProximal},
                {"finger5.R.001" , HumanBodyBones.RightLittleIntermediate},
                {"finger4.R" , HumanBodyBones.RightRingProximal},
                {"finger4.R.001" , HumanBodyBones.RightRingIntermediate},
                {"finger3.R" , HumanBodyBones.RightMiddleProximal},
                {"finger3.R.001" , HumanBodyBones.RightMiddleIntermediate},
                {"finger2.R" , HumanBodyBones.RightIndexProximal},
                {"finger2.R.001" , HumanBodyBones.RightIndexIntermediate},
                {"finger1.R" , HumanBodyBones.RightThumbProximal},
                {"finger1.R.001" , HumanBodyBones.RightThumbDistal},

                {"spine.004" , HumanBodyBones.Head},

                {"thigh.L" , HumanBodyBones.LeftUpperLeg},
                {"shin.L" , HumanBodyBones.LeftLowerLeg},
                {"foot.L" , HumanBodyBones.LeftFoot},
                {"toe.L" , HumanBodyBones.LeftToes},

                {"thigh.R" , HumanBodyBones.RightUpperLeg},
                {"shin.R" , HumanBodyBones.RightLowerLeg},
                {"foot.R" , HumanBodyBones.RightFoot},
                {"toe.R" , HumanBodyBones.RightToes},
        };
    }

    #region model setup classes
    public class TranslationOffset : MonoBehaviour
    {
        public Vector3 offset = Vector3.zero;

    }
    public class RotationOffset : MonoBehaviour
    {
        public Quaternion offset = Quaternion.identity;

    }
    public class ItemOffset : MonoBehaviour
    {
        public Quaternion rOffset = Quaternion.identity;
        public Vector3 pOffset = Vector3.zero;
    }
    #endregion
}
