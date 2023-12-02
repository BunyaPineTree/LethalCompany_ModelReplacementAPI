using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace ModelReplacement
{
    [JsonObject]
    public class BoneMap
    {
        [JsonProperty]
        private List<List<string>> boneMap = new List<List<string>>();
        private List<MappedBone> mappedBones = new List<MappedBone>();

        [JsonProperty]
        private List<float> _positionOffSet = new List<float>();
        private Vector3 positionOffset = Vector3.zero;

        [JsonProperty]
        private List<float> _itemHolderPositionOffset = new List<float>();
        private Vector3 itemHolderPositionOffset = Vector3.zero;

        [JsonProperty]
        private string itemHolderBone = "";
        private Transform itemHolderTransform = null;

        [JsonProperty]
        private string rootBone = "";
        private Transform rootBoneTransform = null;



        public static BoneMap DeserializeFromJson(string jsonStr)
        {
            return Newtonsoft.Json.JsonConvert.DeserializeObject<BoneMap>(jsonStr);
        }

        public void MapBones(Transform[] playerBones, Transform[] modelBones)
        {
            //Set ragdoll bone
            if(!boneMap.Where(x=> x[0] == "PlayerRagdoll(Clone)").Any())
            {
                if (boneMap.Where(x => x[0] == "spine").Any())
                {
                    List<string> ragdollSpineBonevars = new List<string>();
                    List<string> spineVars = boneMap.Where(x => x[0] == "spine").First();
                    for (int i = 0; i < spineVars.Count(); i++)
                    {
                        if(i == 0)
                        {
                            ragdollSpineBonevars.Add("PlayerRagdoll(Clone)");
                        }
                        else
                        {
                            ragdollSpineBonevars.Add(spineVars[i]);
                        }
                    }


                    boneMap.Add(ragdollSpineBonevars);
                }
               
            }
   
            mappedBones.Clear();
            itemHolderTransform = null;
            if(_positionOffSet.Count == 3)
            {
                positionOffset = new Vector3(_positionOffSet[0], _positionOffSet[1], _positionOffSet[2]);
            }
            if (_itemHolderPositionOffset.Count == 3)
            {
                itemHolderPositionOffset = new Vector3(_itemHolderPositionOffset[0], _itemHolderPositionOffset[1], _itemHolderPositionOffset[2]);
            }

            foreach (var vars in boneMap)
            {
                string playerBone = vars[0];
                string modelBone = vars[1];

                if (modelBone == "") { continue; }
                if (!modelBones.Any(x => x.name == modelBone))
                {
                    ModelReplacementAPI.Instance.Logger.LogWarning($"No bone in model with name ({modelBone})");
                    continue;
                }
                if (!playerBones.Any(x => x.name == playerBone))
                {
                    ModelReplacementAPI.Instance.Logger.LogWarning($"No bone in player with name ({playerBone})");
                    continue;
                }

                Transform playerTransform = playerBones.Where(x => x.name == playerBone).First();
                Transform modelTransform = modelBones.Where(x => x.name == modelBone).First();

                mappedBones.Add(new MappedBone(vars, playerTransform, modelTransform));

            }

            itemHolderTransform = modelBones.Where(x => x.name == itemHolderBone).First();
            rootBoneTransform = modelBones.Where(x => x.name == rootBone).First();
        }

        public void UpdateModelbones()
        {
            mappedBones.ForEach(x =>
            {
                bool destroyMappedBone = x.Update();

            });

            List<MappedBone> destroyBones = new List<MappedBone>();
            mappedBones.ForEach(x =>
            {
                bool destroyMappedBone = x.Update();
                if(destroyMappedBone ) { destroyBones.Add(x); }
            });
            destroyBones.ForEach(x => { mappedBones.Remove(x); });
        }

        public bool CompletelyDestroyed()
        {
            if ((mappedBones.Count() == 0) || rootBoneTransform == null)
            {
                Console.WriteLine("bone Map destroyed");
                return true;
            }
            return false;
        }

        public Vector3 PositionOffset() => positionOffset;
        public Vector3 ItemHolderPositionOffset() => itemHolderPositionOffset;
        public Transform ItemHolder() => itemHolderTransform;
        public Transform RootBone() => rootBoneTransform;

        public Transform GetMappedTransform(string playerTransformName)
        {
            var a = mappedBones.Where(x => x.playerBoneString == playerTransformName);

            if (a.Any())
            {
                return a.First().modelTransform;
            }
            ModelReplacementAPI.Instance.Logger.LogWarning($"No mapped bone with player bone name {playerTransformName}");
            return null;



        }

        public class MappedBone
        {
            public string playerBoneString;
            public string modelBoneString;
            public Quaternion rotationOffset = Quaternion.identity;

            public Transform playerTransform;
            public Transform modelTransform;

            public List<string> additionalVars = new List<string>();
            public MappedBone(List<string> vars, Transform player, Transform model)
            {
                playerTransform = player;
                modelTransform = model;

                int varsCount = vars.Count;
                if (varsCount >= 2)
                {
                    playerBoneString = vars[0];
                    modelBoneString = vars[1];

                }
                if (varsCount >= 6)
                {
                    float x, y, z, w;
                    try
                    {
                       
                        x = float.Parse(vars[2]);
                        y = float.Parse(vars[3]);
                        z = float.Parse(vars[4]);
                        w = float.Parse(vars[5]);
                        rotationOffset = new Quaternion(x, y, z, w);
                        //Console.WriteLine($"Setting quaternion for {modelBoneString} xyzw({x},{y}, {z}, {w})");
                    }
                    catch (Exception e)
                    {
                        ModelReplacementAPI.Instance.Logger.LogError($"could not parse rotation offset for player bone {playerBoneString} xyzw({vars[2]},{vars[3]},{vars[4]},{vars[5]})");
                    }

                }
                if(varsCount > 6)
                {
                    for (int i = 6; i < varsCount; i++)
                    {
                        additionalVars.Add(vars[i]);
                    }
                }



            }

            public bool Update()
            {
                if((modelTransform == null) || (playerTransform == null))
                {
                    ModelReplacementAPI.Instance.Logger.LogError($"Could not Update bone, model or player transform is null. Destroying MappedBone ({modelBoneString})");
                    
                    return true;
                }
                try
                {
                    modelTransform.rotation = new Quaternion(playerTransform.rotation.x, playerTransform.rotation.y, playerTransform.rotation.z, playerTransform.rotation.w);
                    modelTransform.rotation *= rotationOffset;
                }
                catch
                {
                    ModelReplacementAPI.Instance.Logger.LogError($"Could not Update bones for {playerTransform.name} to {modelTransform.name} ");
                    return true;
                }
                return false;
                

            }



        }
    }
}
