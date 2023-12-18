using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using ModelReplacement;
using GameNetcodeStuff;
using System.Numerics;
using ModelReplacement.AvatarBodyUpdater;

//using ModelReplacement.Scripts;

namespace HatsuneMikuModelReplacement
{
    public class BodyReplacementMiku : BodyReplacementBase
    {
        //Required universally
        protected override GameObject LoadAssetsAndReturnModel()
        {
            //Replace with the Asset Name from your unity project 
            string model_name = "HatsuneMikuNTPrefab";
            return Assets.MainAssetBundle.LoadAsset<GameObject>(model_name);
        }

        //Miku mod specific scripts. Delete this if you have no custom scripts to add. 
        protected override void AddModelScripts()
        {
            //Set dynamic bone options. DynamicBones is a paid for asset that I am unable to include in this repository. If this section causes errors and you don't have DynamicBones in your project, just remove it. 
            replacementModel.GetComponentsInChildren<DynamicBone>().ToList().ForEach(bone =>
            {
                bone.m_UpdateRate = Plugin.UpdateRate.Value;
                bone.m_DistantDisable = Plugin.disablePhysicsAtRange.Value;
                bone.m_DistanceToObject = Plugin.distanceDisablePhysics.Value;
            });
        }


        protected override void OnEmoteStart(int emoteId)
        {
            if(emoteId == 1) {
                replacementModel.GetComponentInChildren<SkinnedMeshRenderer>().SetBlendShapeWeight(29, 0);
                replacementModel.GetComponentInChildren<SkinnedMeshRenderer>().SetBlendShapeWeight(44, 0);
                replacementModel.GetComponentInChildren<SkinnedMeshRenderer>().SetBlendShapeWeight(49, 0);
                replacementModel.GetComponentInChildren<SkinnedMeshRenderer>().SetBlendShapeWeight(58, 0);
                replacementModel.GetComponentInChildren<SkinnedMeshRenderer>().SetBlendShapeWeight(60, 0);

                replacementModel.GetComponentInChildren<SkinnedMeshRenderer>().SetBlendShapeWeight(83, 100);
                replacementModel.GetComponentInChildren<SkinnedMeshRenderer>().SetBlendShapeWeight(84, 100);
                replacementModel.GetComponentInChildren<SkinnedMeshRenderer>().SetBlendShapeWeight(25, 60);
                replacementModel.GetComponentInChildren<SkinnedMeshRenderer>().SetBlendShapeWeight(45, 100);
            }
            if(emoteId == 2)
            {
                replacementModel.GetComponentInChildren<SkinnedMeshRenderer>().SetBlendShapeWeight(83, 0);
                replacementModel.GetComponentInChildren<SkinnedMeshRenderer>().SetBlendShapeWeight(84, 0);
                replacementModel.GetComponentInChildren<SkinnedMeshRenderer>().SetBlendShapeWeight(25, 0);
                replacementModel.GetComponentInChildren<SkinnedMeshRenderer>().SetBlendShapeWeight(45, 0);

                replacementModel.GetComponentInChildren<SkinnedMeshRenderer>().SetBlendShapeWeight(29, 100);
                replacementModel.GetComponentInChildren<SkinnedMeshRenderer>().SetBlendShapeWeight(44, 100);
                replacementModel.GetComponentInChildren<SkinnedMeshRenderer>().SetBlendShapeWeight(49, 100);
                replacementModel.GetComponentInChildren<SkinnedMeshRenderer>().SetBlendShapeWeight(58, 100);
                replacementModel.GetComponentInChildren<SkinnedMeshRenderer>().SetBlendShapeWeight(60, 70);
            }
            
        }

        protected override void OnEmoteEnd()
        {
            replacementModel.GetComponentInChildren<SkinnedMeshRenderer>().SetBlendShapeWeight(83, 0);
            replacementModel.GetComponentInChildren<SkinnedMeshRenderer>().SetBlendShapeWeight(84, 0);
            replacementModel.GetComponentInChildren<SkinnedMeshRenderer>().SetBlendShapeWeight(25, 0);
            replacementModel.GetComponentInChildren<SkinnedMeshRenderer>().SetBlendShapeWeight(45, 0);

            replacementModel.GetComponentInChildren<SkinnedMeshRenderer>().SetBlendShapeWeight(29, 0);
            replacementModel.GetComponentInChildren<SkinnedMeshRenderer>().SetBlendShapeWeight(44, 0);
            replacementModel.GetComponentInChildren<SkinnedMeshRenderer>().SetBlendShapeWeight(49, 0);
            replacementModel.GetComponentInChildren<SkinnedMeshRenderer>().SetBlendShapeWeight(58, 0);
            replacementModel.GetComponentInChildren<SkinnedMeshRenderer>().SetBlendShapeWeight(60, 0);
        }
    }
}
