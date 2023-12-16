using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using ModelReplacement;
using ModelReplacement.Scripts;
using GameNetcodeStuff;

namespace HatsuneMikuModelReplacement
{
    public class BodyReplacementMiku : BodyReplacementBase
    {
        //Required universally
        protected override GameObject LoadAssetsAndReturnModel()
        {
            //Replace with the Asset Name from your unity project 
            string model_name = "HatsuneMikuNT";
            return Assets.MainAssetBundle.LoadAsset<GameObject>(model_name);
        }

        //Miku mod specific scripts. 
        protected override void AddModelScripts()
        {
            //Set dynamic bone options
            replacementModel.GetComponentsInChildren<DynamicBone>().ToList().ForEach(bone =>
            {
                bone.m_UpdateRate = Plugin.UpdateRate.Value;
                bone.m_DistantDisable = Plugin.disablePhysicsAtRange.Value;
                bone.m_DistanceToObject = Plugin.distanceDisablePhysics.Value;
            });
        }

 
    }
}
