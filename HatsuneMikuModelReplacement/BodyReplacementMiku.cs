using GameNetcodeStuff;
using ModelReplacement;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using UnityEngine;

namespace HatsuneMikuModelReplacement
{
    public class BodyReplacementMiku : BodyReplacementBase
    {
        List<string> baseMatNames = new List<string>()
        {
            "Body",
            "Eye",
            "Costume",
            "Hair",
            "HairF",
            "Transparent"
        };


        //Required universally
        protected override GameObject LoadAssetsAndReturnModel()
        {
            //Replace with the Asset Name from your unity project 
            string model_name = "HatsuneMikuNT";
            var model = Assets.MainAssetBundle.LoadAsset<GameObject>(model_name);

            int suitID = base.GetComponent<PlayerControllerB>().currentSuitID;
            string suitName = StartOfRound.Instance.unlockablesList.unlockables[suitID].unlockableName.ToLower().Replace(" ", "");

            var commaSepList2 = Plugin.suitNamesToEnableEvilMiku.Value.Split(',');
            commaSepList2 = commaSepList2.Select(x => x.ToLower().Replace(" ", "")).ToArray();

            if (commaSepList2.Contains(suitName))
            {
                var renderer = model.GetComponentInChildren<SkinnedMeshRenderer>();

                string suffix = "D";
                List<Material> newMaterials = new List<Material>();
                foreach(var matName in baseMatNames)
                {
                    newMaterials.Add(Assets.MainAssetBundle.LoadAsset<Material>(matName + suffix));
                }
                renderer.SetMaterials(newMaterials);
            }
            else
            {
                var renderer = model.GetComponentInChildren<SkinnedMeshRenderer>();

                string suffix = "";
                List<Material> newMaterials = new List<Material>();
                foreach (var matName in baseMatNames)
                {
                    newMaterials.Add(Assets.MainAssetBundle.LoadAsset<Material>(matName + suffix));
                }
                renderer.SetMaterials(newMaterials);
            }

            return model;
        }


        protected override void OnDeath()
        {
            replacementDeadBody.GetComponentInChildren<SkinnedMeshRenderer>().SetBlendShapeWeight(83, 0);
            replacementDeadBody.GetComponentInChildren<SkinnedMeshRenderer>().SetBlendShapeWeight(84, 0);
            replacementDeadBody.GetComponentInChildren<SkinnedMeshRenderer>().SetBlendShapeWeight(25, 0);
            replacementDeadBody.GetComponentInChildren<SkinnedMeshRenderer>().SetBlendShapeWeight(45, 0);

            replacementDeadBody.GetComponentInChildren<SkinnedMeshRenderer>().SetBlendShapeWeight(29, 0);
            replacementDeadBody.GetComponentInChildren<SkinnedMeshRenderer>().SetBlendShapeWeight(44, 0);
            replacementDeadBody.GetComponentInChildren<SkinnedMeshRenderer>().SetBlendShapeWeight(49, 0);
            replacementDeadBody.GetComponentInChildren<SkinnedMeshRenderer>().SetBlendShapeWeight(58, 0);
            replacementDeadBody.GetComponentInChildren<SkinnedMeshRenderer>().SetBlendShapeWeight(60, 0);

            replacementDeadBody.GetComponentInChildren<SkinnedMeshRenderer>().SetBlendShapeWeight(29, 100);
            replacementDeadBody.GetComponentInChildren<SkinnedMeshRenderer>().SetBlendShapeWeight(52, 100);
            replacementDeadBody.GetComponentInChildren<SkinnedMeshRenderer>().SetBlendShapeWeight(61, 100);
            replacementDeadBody.GetComponentInChildren<SkinnedMeshRenderer>().SetBlendShapeWeight(67, 12);
            replacementDeadBody.GetComponentInChildren<SkinnedMeshRenderer>().SetBlendShapeWeight(72, 100);
        }

        protected override void OnEmoteStart(int emoteId)
        {
            replacementModel.GetComponentInChildren<SkinnedMeshRenderer>().SetBlendShapeWeight(29, 0);
            replacementModel.GetComponentInChildren<SkinnedMeshRenderer>().SetBlendShapeWeight(44, 0);
            replacementModel.GetComponentInChildren<SkinnedMeshRenderer>().SetBlendShapeWeight(49, 0);
            replacementModel.GetComponentInChildren<SkinnedMeshRenderer>().SetBlendShapeWeight(58, 0);
            replacementModel.GetComponentInChildren<SkinnedMeshRenderer>().SetBlendShapeWeight(60, 0);

            replacementModel.GetComponentInChildren<SkinnedMeshRenderer>().SetBlendShapeWeight(83, 0);
            replacementModel.GetComponentInChildren<SkinnedMeshRenderer>().SetBlendShapeWeight(84, 0);
            replacementModel.GetComponentInChildren<SkinnedMeshRenderer>().SetBlendShapeWeight(25, 0);
            replacementModel.GetComponentInChildren<SkinnedMeshRenderer>().SetBlendShapeWeight(45, 0);


            if (emoteId == 1) {
               

                replacementModel.GetComponentInChildren<SkinnedMeshRenderer>().SetBlendShapeWeight(83, 100);
                replacementModel.GetComponentInChildren<SkinnedMeshRenderer>().SetBlendShapeWeight(84, 100);
                replacementModel.GetComponentInChildren<SkinnedMeshRenderer>().SetBlendShapeWeight(25, 60);
                replacementModel.GetComponentInChildren<SkinnedMeshRenderer>().SetBlendShapeWeight(45, 100);
            }
            if(emoteId == 2)
            {
               

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
