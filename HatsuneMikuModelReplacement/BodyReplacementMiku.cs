using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Newtonsoft.Json;
using System.Linq;
using System.Xml;
using System.IO;
using GameNetcodeStuff;
using UnityEngine.PlayerLoop;
using System.Reflection;
using ModelReplacement;

namespace HatsuneMikuModelReplacement
{
    public class BodyReplacementMiku : BodyReplacementBase
    {
        public override string boneMapFileName => "boneMapMiku.json";

        public override GameObject LoadAssetsAndReturnModel()
        {
            string model_name = "HatsuneMikuNT 1";
            return Assets.MainAssetBundle.LoadAsset<GameObject>(model_name);
        }

        public override Transform[] GetMappedBones()
        {
            return replacementModel.GetComponentInChildren<SkinnedMeshRenderer>().bones;
        }


        public override SkinnedMeshRenderer GetArmatureSkinnedMeshRenderer()
        {
            return replacementModel.GetComponentInChildren<SkinnedMeshRenderer>();
        }

        public override SkinnedMeshRenderer GetDeadBodySkinnedMeshRenderer()
        {
            return replacementDeadBody.GetComponentInChildren<SkinnedMeshRenderer>();
        }


        public override void AddModelScripts()
        {
            //Set dynamic bones
            var skirtBones = replacementModel.GetComponentsInChildren<Transform>().Where(x => x.name.Contains("Skirt")).Where(x => x.name.Contains("001"));
            Console.WriteLine($"Dynamic bones {skirtBones.Count()}");
            List<DynamicBone> skirtDynBones = new List<DynamicBone>();
            skirtBones.ToList().ForEach(bone =>
            {
                DynamicBone dynBone = bone.gameObject.AddComponent<DynamicBone>();
                dynBone.m_Root = bone;
                dynBone.m_UpdateRate = 60;
                dynBone.m_Damping = 0.14f;
                dynBone.m_Elasticity = 0.05f;
                dynBone.m_Stiffness = 0.7f;
                dynBone.m_Inert = 0.28f;
                dynBone.m_Radius = 0.01f;
                dynBone.m_Gravity = new Vector3(0, -0.01f, 0);
                skirtDynBones.Add(dynBone);
            });

            var hipBone = replacementModel.GetComponentsInChildren<Transform>().Where(x => x.name.Contains("Hips")).First();
            var legBoneL = replacementModel.GetComponentsInChildren<Transform>().Where(x => x.name.Contains("Upper Leg.L")).First();
            var legBoneR = replacementModel.GetComponentsInChildren<Transform>().Where(x => x.name.Contains("Upper Leg.R")).First();
            var colliderL = legBoneL.gameObject.AddComponent<DynamicBoneCollider>();
            colliderL.m_Center = new Vector3(-0.01f, 0.15f, -0.01f);
            colliderL.m_Radius = 0.085f;
            colliderL.m_Height = 0.5f;
            colliderL.m_Direction = DynamicBoneCollider.Direction.Y;
            colliderL.m_Bound = DynamicBoneCollider.Bound.Outside;

            var colliderR = legBoneR.gameObject.AddComponent<DynamicBoneCollider>();
            colliderR.m_Center = new Vector3(0.01f, 0.15f, -0.01f);
            colliderR.m_Radius = 0.085f;
            colliderR.m_Height = 0.5f;
            colliderR.m_Direction = DynamicBoneCollider.Direction.Y;
            colliderR.m_Bound = DynamicBoneCollider.Bound.Outside;

            var colliderM = hipBone.gameObject.AddComponent<DynamicBoneCollider>();
            colliderM.m_Center = new Vector3(0f, -0.08f, -0.03f);
            colliderM.m_Radius = 0.13f;
            colliderM.m_Height = 0.3f;
            colliderM.m_Direction = DynamicBoneCollider.Direction.X;
            colliderM.m_Bound = DynamicBoneCollider.Bound.Outside;

            List<DynamicBoneCollider> dynamicBoneColliders = new List<DynamicBoneCollider>() { colliderL, colliderR, colliderM };
            skirtDynBones.ForEach(x => { x.m_Colliders = dynamicBoneColliders; });

            var HairBones = replacementModel.GetComponentsInChildren<Transform>().Where(x => x.name.Contains("FrontHair.") || x.name.Contains("TwinTail.A.001"));
            HairBones.ToList().ForEach(bone =>
            {
                DynamicBone dynBone = bone.gameObject.AddComponent<DynamicBone>();
                dynBone.m_Root = bone;
                dynBone.m_UpdateRate = 60;
                dynBone.m_Damping = 0.254f;
                dynBone.m_Elasticity = 0.08f;
                dynBone.m_Stiffness = 0.5f;
                dynBone.m_Inert = 0.274f;
                dynBone.m_Radius = 0.05f;
                dynBone.m_Gravity = new Vector3(0, -0.01f, 0);
            });

            var HairBones2 = replacementModel.GetComponentsInChildren<Transform>().Where(x => x.name.Contains("TwinTail.A.001"));
            HairBones2.ToList().ForEach(x => x.localScale = new Vector3(1, 0.7f, 1));
        }

 
    }
}
