using GameNetcodeStuff;
using ModelReplacement;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ModelReplacement.AvatarBodyUpdater;

namespace ModelReplacement.Scripts.Enemies
{

    public class MaskedAvatarUpdater
    {
        //Third Person Avatar
        protected SkinnedMeshRenderer enemyModelRenderer = null;
        protected Animator replacementAnimator = null;
        protected GameObject enemy = null;
        protected GameObject replacement = null;
        public Vector3 ItemHolderPositionOffset { get; private set; } = Vector3.zero;
        public Quaternion ItemHolderRotationOffset { get; private set; } = Quaternion.identity;
        public Transform ItemHolder { get; private set; } = null;

        protected bool hasUpperChest = false;
        protected Vector3 rootPositionOffset = Vector3.zero;



        public virtual void AssignModelReplacement(GameObject enemy, GameObject replacement)
        {
            MaskedPlayerEnemy enemyAI = enemy.GetComponent<MaskedPlayerEnemy>();
            enemyModelRenderer = enemyAI.rendererLOD0;


            this.enemy = enemy;
            replacementAnimator = replacement.GetComponentInChildren<Animator>();
            this.replacement = replacement;

            OffsetBuilder offsetBuilder = replacementAnimator.gameObject.GetComponent<OffsetBuilder>();
            ItemHolderPositionOffset = offsetBuilder.itemPositonOffset;
            ItemHolderRotationOffset = offsetBuilder.itemRotationOffset;
            ItemHolder = offsetBuilder.itemHolder.transform;
            rootPositionOffset = offsetBuilder.rootPositionOffset;


            Transform upperChestTransform = replacementAnimator.GetBoneTransform(HumanBodyBones.UpperChest);
            hasUpperChest = upperChestTransform != null;
        }

        protected virtual void UpdateModel()
        {
            foreach (Transform playerBone in enemyModelRenderer.bones)
            {
                Transform modelBone = GetAvatarTransformFromBoneName(playerBone.name);
                if (modelBone == null) { continue; }

                modelBone.rotation = playerBone.rotation;
                RotationOffset offset = modelBone.GetComponent<RotationOffset>();
                if (offset) { modelBone.rotation *= offset.offset; }
            }

            Transform rootBone = GetAvatarTransformFromBoneName("spine");
            Transform playerRootBone = GetPlayerTransformFromBoneName("spine");
            rootBone.position = playerRootBone.position + playerRootBone.TransformVector(rootPositionOffset);
        }



        public void Update()
        {
            if (enemyModelRenderer == null || replacementAnimator == null) { return; }
            UpdateModel();
        }

        public Transform GetAvatarTransformFromBoneName(string boneName)
        {
            if (boneName == "spine.002")
            {
                return hasUpperChest ? replacementAnimator.GetBoneTransform(HumanBodyBones.Chest) : null;
            }

            if (boneName == "spine.003")
            {
                return hasUpperChest ? replacementAnimator.GetBoneTransform(HumanBodyBones.UpperChest) : replacementAnimator.GetBoneTransform(HumanBodyBones.Chest);
            }

            return modelToAvatarBone.TryGetValue(boneName, out HumanBodyBones avatarBone)
                ? replacementAnimator.GetBoneTransform(avatarBone)
                : null; // throw new Exception($"Failed to find bone {boneName}");
        }

        public Transform GetPlayerTransformFromBoneName(string boneName)
        {
            IEnumerable<Transform> playerBones = enemyModelRenderer.bones.Where(x => x.name == boneName);

            if (playerBones.Any())
            {
                return playerBones.First();
            }

            return null;
        }


        //Remove spine.002 and .003 to implement logic
        protected static Dictionary<string, HumanBodyBones> modelToAvatarBone = new Dictionary<string, HumanBodyBones>()
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


}
