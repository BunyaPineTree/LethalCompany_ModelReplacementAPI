using GameNetcodeStuff;
using ModelReplacement.AvatarBodyUpdater;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace ModelReplacement.Scripts.Player
{
    public class ViewModelUpdater
    {
        protected GameObject replacementViewModel = null;
        private Transform armsMetarig;
        private Animator viewModelAnimator;
        public Transform ItemHolderViewModel { get; protected set; } = null;

        private bool hasShoulder = true;
        public virtual void AssignViewModelReplacement(GameObject player, GameObject replacementViewModel)
        {
            if (replacementViewModel == null) return;
            PlayerControllerB controller = player.GetComponent<PlayerControllerB>();
            this.replacementViewModel = replacementViewModel;
            viewModelAnimator = replacementViewModel.GetComponentInChildren<Animator>();
            OffsetBuilder offsetBuilder = viewModelAnimator.gameObject.GetComponent<OffsetBuilder>();
            ItemHolderViewModel = offsetBuilder.itemHolder.transform;
            armsMetarig = controller.playerModelArmsMetarig;

            //Scale by arm length
            List<string> armNames = new List<string>()
            {
                "shoulder.L",
                "arm.L_upper",
                "arm.L_lower",
                "hand.L",
            };
            if (GetViewModelTransformFromBoneName("shoulder.L") == null || GetViewModelTransformFromBoneName("shoulder.R") == null) { hasShoulder = false; }

            float armLength = 0;
            float viewModelLength = 0;
            for (int i = hasShoulder ? 0 : 1; i < armNames.Count - 1; i++)
            {
                Transform armBoneBase = GetArmTransformFromBoneName(armNames[i]);
                Transform armBoneEnd = GetArmTransformFromBoneName(armNames[i + 1]);
                armLength += (armBoneEnd.position - armBoneBase.position).magnitude;

                Transform viewModelBoneBase = GetViewModelTransformFromBoneName(armNames[i]);
                Transform viewModelBoneEnd = GetViewModelTransformFromBoneName(armNames[i + 1]);
                viewModelLength += (viewModelBoneEnd.position - viewModelBoneBase.position).magnitude;
            }

            replacementViewModel.transform.localScale *= armLength / viewModelLength;
            replacementViewModel.SetActive(true);
        }

        protected virtual void UpdateViewModel()
        {
            GetViewModelTransformFromBoneName("arm.L_upper").position = GetArmTransformFromBoneName("arm.L_upper").position;
            GetViewModelTransformFromBoneName("arm.R_upper").position = GetArmTransformFromBoneName("arm.R_upper").position;
            if (hasShoulder)
            {
                GetViewModelTransformFromBoneName("shoulder.L").position = GetArmTransformFromBoneName("shoulder.L").position;
                GetViewModelTransformFromBoneName("shoulder.R").position = GetArmTransformFromBoneName("shoulder.R").position;
            }

            foreach (string boneName in ArmTransformNames)
            {
                Transform armBone = GetArmTransformFromBoneName(boneName);
                Transform viewModelBone = GetViewModelTransformFromBoneName(boneName);
                if (!armBone || !viewModelBone) continue;

                viewModelBone.rotation = armBone.rotation;
                RotationOffset offset = viewModelBone.GetComponent<RotationOffset>();
                if (offset) { viewModelBone.rotation *= offset.offset; }
            }

        }

        public void Update()
        {
            if (replacementViewModel == null) { return; }
            UpdateViewModel();
        }

        public Transform GetViewModelTransformFromBoneName(string boneName)
        {
            return modelToAvatarBone.TryGetValue(boneName, out HumanBodyBones avatarBone)
                ? viewModelAnimator.GetBoneTransform(avatarBone)
                : null; // throw new Exception($"Failed to find bone {boneName}");
        }

        public Transform GetArmTransformFromBoneName(string boneName)
        {
            IEnumerable<Transform> playerBones = armsMetarig.gameObject.GetComponentsInChildren<Transform>().Where(x => x.name == boneName);
            return playerBones.Any() ? playerBones.First() : null;
        }

        protected static Dictionary<string, HumanBodyBones> modelToAvatarBone = new Dictionary<string, HumanBodyBones>()
            {
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
        };

        protected static List<string> ArmTransformNames = new List<string>()
        {
                "shoulder.L",
                "arm.L_upper",
                "arm.L_lower",
                "hand.L",
                "finger5.L" ,
                "finger5.L.001" ,
                "finger4.L" ,
                "finger4.L.001" ,
                "finger3.L" ,
                "finger3.L.001" ,
                "finger2.L" ,
                "finger2.L.001" ,
                "finger1.L" ,
                "finger1.L.001" ,

                "shoulder.R",
                "arm.R_upper",
                "arm.R_lower",
                "hand.R",
                "finger5.R" ,
                "finger5.R.001" ,
                "finger4.R" ,
                "finger4.R.001" ,
                "finger3.R" ,
                "finger3.R.001" ,
                "finger2.R" ,
                "finger2.R.001" ,
                "finger1.R" ,
                "finger1.R.001"
        };

        public static List<HumanBodyBones> ViewModelHumanBones = new List<HumanBodyBones>()
        {
                 HumanBodyBones.LeftShoulder,
                 HumanBodyBones.LeftUpperArm,
                 HumanBodyBones.LeftLowerArm,
                 HumanBodyBones.LeftHand,
                 HumanBodyBones.LeftLittleProximal,
                 HumanBodyBones.LeftLittleIntermediate,
                 HumanBodyBones.LeftRingProximal,
                 HumanBodyBones.LeftRingIntermediate,
                 HumanBodyBones.LeftMiddleProximal,
                 HumanBodyBones.LeftMiddleIntermediate,
                 HumanBodyBones.LeftIndexProximal,
                 HumanBodyBones.LeftIndexIntermediate,
                 HumanBodyBones.LeftThumbProximal,
                 HumanBodyBones.LeftThumbDistal,

                 HumanBodyBones.RightShoulder,
                 HumanBodyBones.RightUpperArm,
                 HumanBodyBones.RightLowerArm,
                 HumanBodyBones.RightHand,
                 HumanBodyBones.RightLittleProximal,
                 HumanBodyBones.RightLittleIntermediate,
                 HumanBodyBones.RightRingProximal,
                 HumanBodyBones.RightRingIntermediate,
                 HumanBodyBones.RightMiddleProximal,
                 HumanBodyBones.RightMiddleIntermediate,
                 HumanBodyBones.RightIndexProximal,
                 HumanBodyBones.RightIndexIntermediate,
                 HumanBodyBones.RightThumbProximal,
                 HumanBodyBones.RightThumbDistal,
        };
    }
}
