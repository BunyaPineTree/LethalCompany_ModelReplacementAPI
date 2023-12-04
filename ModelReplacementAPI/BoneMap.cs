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
        [JsonIgnore]
        private List<MappedBone> mappedBones = new List<MappedBone>();

        [JsonProperty]
        private List<float> _positionOffSet = new List<float>();
        [JsonIgnore]
        private Vector3 positionOffset = Vector3.zero;

        [JsonProperty]
        private List<float> _itemHolderPositionOffset = new List<float>();
        [JsonIgnore]
        private Vector3 itemHolderPositionOffset = Vector3.zero;

        [JsonProperty]
        private string itemHolderBone = "";
        [JsonIgnore]
        private Transform itemHolderTransform = null;

        [JsonProperty]
        private string rootBone = "";
        [JsonIgnore]
        private Transform rootBoneTransform = null;
        [JsonIgnore]
        public Transform[] playerBoneList = null;
        [JsonIgnore]
        public Transform[] modelBoneList = null;
        [JsonIgnore]
        public BodyReplacementBase replacementBase;
        public static BoneMap DeserializeFromJson(string jsonStr)
        {
            return Newtonsoft.Json.JsonConvert.DeserializeObject<BoneMap>(jsonStr);
        }

        public string SerializeToJsonString()
        {
            _positionOffSet = new List<float> { positionOffset.x, positionOffset.y, positionOffset.z };
            _itemHolderPositionOffset = new List<float> { itemHolderPositionOffset.x, itemHolderPositionOffset.y, itemHolderPositionOffset.z };

            boneMap.Clear();
            foreach (var item in mappedBones)
            {
                List<string> listStr = new List<string>();
                listStr.Add(item.playerBoneString);
                listStr.Add(item.modelBoneString);
                if(item.rotationOffset != Quaternion.identity)
                {
                    listStr.Add(item.rotationOffset.x.ToString());
                    listStr.Add(item.rotationOffset.y.ToString());
                    listStr.Add(item.rotationOffset.z.ToString());
                    listStr.Add(item.rotationOffset.w.ToString());
                    if(item.additionalVars.Count > 0)
                    {
                        foreach (var item1 in item.additionalVars)
                        {
                            listStr.Add(item1);
                        }
                    }
                }

                boneMap.Add(listStr);
            }


            return Newtonsoft.Json.JsonConvert.SerializeObject(this);
        }
        
        public void SetBodyReplacement(BodyReplacementBase body)
        {
            replacementBase = body;
        }

        public void MapBones(Transform[] playerBones, Transform[] modelBones)
        {
            playerBoneList = playerBones;
            modelBoneList = modelBones;

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

        public MappedBone GetMappedBoneWithPlayerName(string playerTransformName)
        {
            var a = mappedBones.Where(x => x.playerBoneString == playerTransformName);

            if (a.Any())
            {
                return a.First();
            }
            ModelReplacementAPI.Instance.Logger.LogWarning($"No mapped bone with player bone name {playerTransformName}");
            return null;
        }

        public List<MappedBone> GetMappedBones() => mappedBones;

        public void UpdateMappedBone(string playerBoneString, string modelBoneString, Quaternion rotationOffset)
        {
            var oldMapped = mappedBones.Where(x => x.playerBoneString == playerBoneString);
            if (oldMapped.Any())
            {
                var a = GetMappedBoneWithPlayerName(playerBoneString);
                a.modelBoneString = modelBoneString;
                a.rotationOffset = rotationOffset;

                var b = modelBoneList.Where(x => x.name == modelBoneString);
                if(b.Any()) { a.modelTransform = b.First(); }

            }
            else
            {
                var a = playerBoneList.Where(x => x.name == playerBoneString);
                var b = modelBoneList.Where(x => x.name == modelBoneString);

                Transform plTransform = null;
                Transform mdTransform = null;

                if(a.Any()) { plTransform = a.First(); }
                if (b.Any()) { mdTransform = b.First(); }

                MappedBone newMB = new MappedBone(playerBoneString, modelBoneString, rotationOffset, plTransform, mdTransform);
                mappedBones.Add(newMB);

            }

        }

        public void UpdateRootBoneAndOffset(Transform newRootBone, Vector3 offset)
        {
            if(newRootBone != rootBoneTransform)
            {
                rootBoneTransform.parent = null;

                rootBoneTransform = newRootBone;
                rootBone = newRootBone.name;
            }
            
            positionOffset = offset;
            replacementBase.ReparentModel();
            
        }

        public void UpdateItemHolderBoneAndOffset(Transform newRootBone, Vector3 offset)
        {
            itemHolderTransform = newRootBone;
            itemHolderBone = newRootBone.name;
            itemHolderPositionOffset = offset;
            replacementBase.flagReparentObject = true;
        }

        public class MappedBone
        {
            public string playerBoneString;
            public string modelBoneString;
            public Quaternion rotationOffset = Quaternion.identity;

            public Transform playerTransform;
            public Transform modelTransform;

            public List<string> additionalVars = new List<string>();

            public MappedBone(string playerBoneString, string modelBoneString, Quaternion rotationOffset, Transform playerTransform, Transform modelTransform)
            {
                this.playerBoneString = playerBoneString;
                this.modelBoneString = modelBoneString;
                this.rotationOffset = rotationOffset;
                this.playerTransform = playerTransform;
                this.modelTransform = modelTransform;
            }

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
