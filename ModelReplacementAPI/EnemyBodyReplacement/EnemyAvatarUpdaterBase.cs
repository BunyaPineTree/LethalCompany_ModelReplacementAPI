using GameNetcodeStuff;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ModelReplacement.Enemies
{

    public abstract class EnemyAvatarUpdaterBase
    {
        //Third Person Avatar
        protected SkinnedMeshRenderer[] enemyModelRenderers = null;
        protected GameObject enemyGameObject = null;
        protected GameObject replacementGameObject = null;



        public virtual void AssignModelReplacement(GameObject enemy, GameObject replacement)
        {
            var enemyAI = enemy.GetComponent<EnemyAI>();
            enemyModelRenderers = enemyAI.skinnedMeshRenderers;
            this.enemyGameObject = enemy;
            this.replacementGameObject = replacement;
        }
        protected abstract void UpdateModel();

        

        public void Update()
        {
            if (enemyGameObject == null || replacementGameObject == null) { return; }
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

            if (boneName.Contains("PlayerRagdoll"))
            {
                return replacementAnimator.GetBoneTransform(HumanBodyBones.Hips);
            }

            return modelToAvatarBone.TryGetValue(boneName, out HumanBodyBones avatarBone)
                ? replacementAnimator.GetBoneTransform(avatarBone)
                : null; // throw new Exception($"Failed to find bone {boneName}");
        }

        public Transform GetPlayerTransformFromBoneName(string boneName)
        {
            IEnumerable<Transform> playerBones = enemyModelRenderers[0].bones.Where(x => x.name == boneName);

            if (playerBones.Any())
            {
                return playerBones.First();
            }

            if (boneName == "spine")
            {
                IEnumerable<Transform> ragdollBones = enemyModelRenderers[0].bones.Where(x => x.name.Contains("PlayerRagdoll"));
                return ragdollBones.Any() ? ragdollBones.First() : null;
            }

            return null;
        }
        

        
        
    }


}
