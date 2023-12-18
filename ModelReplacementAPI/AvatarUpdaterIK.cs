using GameNetcodeStuff;
using ModelReplacement.AvatarBodyUpdater;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem.XR;
///
///
/// All code in this AvatarUpdaterIK.cs has been translated from the LethalCreatures repository at https://github.com/DarnHyena/LethalCreatures
///
///
namespace ModelReplacement.AvatarBodyUpdater
{
    public class AvatarUpdaterIK : AvatarUpdater
    {

        public override void AssignModelReplacement(GameObject player, GameObject replacement)
        {
            base.AssignModelReplacement(player, replacement);




            replacement.transform.SetParent(GetPlayerTransformFromBoneName("spine.001"));
            replacement.transform.localPosition = new Vector3(0, 0f, 0);
            replacement.transform.localEulerAngles = Vector3.zero;


            //Dark magik IK voodoo//========

            var anim = replacement.GetComponentInChildren<Animator>();
           // anim.runtimeAnimatorController = LC_API.BundleAPI.BundleLoader.GetLoadedAsset<RuntimeAnimatorController>("assets/creaturecontrol.controller");
            var ikController = anim.gameObject.AddComponent<IKController>();

            var lfoot = GetPlayerTransformFromBoneName("foot.L");
            var rfoot = GetPlayerTransformFromBoneName("foot.R");
            var lHand = GetPlayerTransformFromBoneName("hand.L");
            var rHand = GetPlayerTransformFromBoneName("hand.R");

            //IK Offsets for limbs//=========//Specific to LethalCreature

            GameObject lFootOffset = new("IK Offset");
            lFootOffset.transform.SetParent(lfoot, false); // X Y Z
            lFootOffset.transform.localPosition = new Vector3(0f, -0.1f, 0f);
            GameObject rFootOffset = new("IK Offset");
            rFootOffset.transform.SetParent(rfoot, false); // X Y Z
            rFootOffset.transform.localPosition = new Vector3(0f, -0.1f, 0f);
            GameObject lHandOffset = new("IK Offset");
            lHandOffset.transform.SetParent(lHand, false); // Z Y
            lHandOffset.transform.localPosition = new Vector3(0.05f, 0f, 0f);
            GameObject rHandOffset = new("IK Offset");
            rHandOffset.transform.SetParent(rHand, false); // Z Y
            rHandOffset.transform.localPosition = new Vector3(-0.05f, 0f, 0f);

            ikController.leftLegTarget = lFootOffset.transform;
            ikController.rightLegTarget = rFootOffset.transform;
            ikController.leftHandTarget = lHandOffset.transform;
            ikController.rightHandTarget = rHandOffset.transform;
            ikController.ikActive = true;

        }

        protected override void UpdateModel()
        {
            replacement.transform.localPosition = new Vector3(0, -0.15f, 0);
            GetAvatarTransformFromBoneName("spine.003").localEulerAngles = GetPlayerTransformFromBoneName("spine.003").localEulerAngles;
        }

    }

    public class IKController : MonoBehaviour
    {
        protected Animator animator;

        public bool ikActive = false;
        public Transform leftLegTarget = null;
        public Transform rightLegTarget = null;
        public Transform leftHandTarget = null;
        public Transform rightHandTarget = null;

        void Start()
        {
            animator = GetComponent<Animator>();
        }

        void OnAnimatorIK()
        {
            if (animator)
            {
                if (ikActive)
                {
                    if (leftLegTarget != null)
                    {
                        animator.SetIKPositionWeight(AvatarIKGoal.LeftFoot, 1);
                        animator.SetIKPosition(AvatarIKGoal.LeftFoot, leftLegTarget.position);
                        animator.SetIKRotation(AvatarIKGoal.LeftFoot, leftLegTarget.rotation);
                    }
                    if (rightLegTarget != null)
                    {
                        animator.SetIKPositionWeight(AvatarIKGoal.RightFoot, 1);
                        animator.SetIKPosition(AvatarIKGoal.RightFoot, rightLegTarget.position);
                        animator.SetIKRotation(AvatarIKGoal.RightFoot, rightLegTarget.rotation);
                    }
                    if (leftHandTarget != null)
                    {
                        animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, 1);
                        animator.SetIKPosition(AvatarIKGoal.LeftHand, leftHandTarget.position);
                        animator.SetIKRotation(AvatarIKGoal.LeftHand, leftHandTarget.rotation);
                    }
                    if (rightHandTarget != null)
                    {
                        animator.SetIKPositionWeight(AvatarIKGoal.RightHand, 1);
                        animator.SetIKPosition(AvatarIKGoal.RightHand, rightHandTarget.position);
                        animator.SetIKRotation(AvatarIKGoal.RightHand, rightHandTarget.rotation);
                    }
                }
                else
                {
                    animator.SetIKPositionWeight(AvatarIKGoal.LeftFoot, 0);
                    animator.SetIKRotationWeight(AvatarIKGoal.LeftFoot, 0);
                    animator.SetIKPositionWeight(AvatarIKGoal.RightFoot, 0);
                    animator.SetIKRotationWeight(AvatarIKGoal.RightFoot, 0);
                    animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, 0);
                    animator.SetIKRotationWeight(AvatarIKGoal.LeftHand, 0);
                    animator.SetIKPositionWeight(AvatarIKGoal.RightHand, 0);
                    animator.SetIKRotationWeight(AvatarIKGoal.RightHand, 0);
                }
            }
        }
    }
}
