using _3rdPerson.Helper;
using GameNetcodeStuff;
using LCThirdPerson;
using ModelReplacement;
using MoreCompany.Cosmetics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem.HID;

namespace ModelReplacement
{
    public abstract class BodyReplacementBase : MonoBehaviour
    {
        public bool localPlayer => (ulong)StartOfRound.Instance.thisClientPlayerId == controller.playerClientId;
        public bool renderLocalDebug = false;
        public bool renderBase = false;
        public bool renderModel = false;

        public bool DangerousViewState()
        {
            return ThirdPersonCamera.ViewState;
        }
        public bool DangerousLCViewState()
        {
            return ThirdPersonPlugin.Instance.Enabled;
        }
        public bool RenderBodyReplacement()
        {
            if(!localPlayer) { return true; }
            if (ModelReplacementAPI.thirdPersonPresent)
            {
                return DangerousViewState();
            }
            if(ModelReplacementAPI.LCthirdPersonPresent)
            {
                return DangerousLCViewState();
            }
            return false;
        }


        // public bool alive = true;
        public string boneMapJsonStr;
        public string jsonPath;

        public BoneMap Map;
        public PlayerControllerB controller;
        public GameObject replacementModel;

        //Ragdoll components
        public GameObject deadBody = null;
        public GameObject replacementDeadBody = null;
        public BoneMap ragDollMap;

        //Misc components
        private MeshRenderer nameTagObj = null;
        private MeshRenderer nameTagObj2 = null;
        public bool flagReparentObject = false;
        public bool moreCompanyCosmeticsReparented = false;

        //Settings
        internal bool ragdollEnabled = true;
        internal bool bloodDecalsEnabled = true;
        


        //Abstract methods 
        /// <summary>
        /// the name of this model replacement's bone mapping .json. Can be anywhere in the bepinex/plugins folder
        /// </summary>
        public abstract string boneMapFileName { get; }
        /// <summary>
        /// Loads necessary assets from assetBundle, perform any necessary modifications on the replacement character model and return it.
        /// </summary>
        /// <returns>Model replacement GameObject</returns>
        public abstract GameObject LoadAssetsAndReturnModel();

        /// <summary>
        /// AssetBundles do not supply scripts that are not supported by the base game. Programmatically set custom scripts here. 
        /// </summary>
        public abstract void AddModelScripts();


        //Virtual methods
        /// <summary>
        /// An array containing at least the bone transforms mapped in boneMap.json. By base it returns all bones in a model. Override if your model has multiple armatures with duplicate bone names.  
        /// </summary>
        /// <returns></returns>
        public virtual Transform[] GetMappedBones()
        {
            List<Transform> result = new List<Transform>();
            foreach (SkinnedMeshRenderer renderer in replacementModel.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                result.AddRange(renderer.bones);
            }
            return result.ToArray();
        }

        private void CreateAndParentRagdoll(DeadBodyInfo bodyinfo)
        {
            Console.WriteLine($"Death on {controller.playerUsername}");

            deadBody = bodyinfo.gameObject;
            SkinnedMeshRenderer deadBodyRenderer = deadBody.GetComponentInChildren<SkinnedMeshRenderer>();
            replacementDeadBody = UnityEngine.Object.Instantiate<GameObject>(replacementModel);
            
            //Enable all renderers in replacement ragdoll and disable renderer for original
            foreach (Renderer renderer in replacementDeadBody.GetComponentsInChildren<Renderer>())
            {
                renderer.enabled = true;
            }
            deadBodyRenderer.enabled = false;


            //Get all bones in the replacement model and select the ones whose names are in GetMappedBones
            List<Transform> replacementDeadBodyBones = new List<Transform>();
            foreach (SkinnedMeshRenderer renderer in replacementDeadBody.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                replacementDeadBodyBones.AddRange(renderer.bones);
            }
            Transform[] originalMappedBones = this.GetMappedBones();
            Transform[] replacementMappedBones = new Transform[originalMappedBones.Length];
            for (int i = 0; i < originalMappedBones.Length; i++)
			{
                replacementMappedBones[i] = replacementDeadBodyBones.Where((Transform bone) => bone.name == originalMappedBones[i].name).First();

            }

            //Make the replacement ragdoll bonemap and parent it
            ragDollMap = BoneMap.DeserializeFromJson(boneMapJsonStr);
            ragDollMap.MapBones(deadBodyRenderer.bones, replacementMappedBones);
            ragDollMap.RootBone().parent = deadBodyRenderer.rootBone;
            ragDollMap.RootBone().localPosition = Vector3.zero + Map.PositionOffset();

            //blood decals not working
            foreach (var item in bodyinfo.bodyBloodDecals)
            {
                Transform bloodParentTransform = item.transform.parent;

                Transform mappedTranform = ragDollMap.GetMappedTransform(bloodParentTransform.name);
                if (mappedTranform)
                {
                    UnityEngine.Object.Instantiate<GameObject>(item, mappedTranform);
                }


            }

        }

        public virtual void AfterAwake()
        {

        }
        public void RepairModel()
        {
            if (controller == null)
            {
                ModelReplacementAPI.Instance.Logger.LogFatal($" {GetType()} PlayerControllerB is null");
                Destroy(this);
            }
            if (controller.thisPlayerModel == null)
            {
                ModelReplacementAPI.Instance.Logger.LogFatal($"{controller.name} {GetType()} base player model is null");
                Destroy(this);
            }
            if (replacementModel == null)
            {
                ModelReplacementAPI.Instance.Logger.LogFatal($"{controller.name} {GetType()} replacementModel is null");
                Destroy(this);
            }
   
            Map = BoneMap.DeserializeFromJson(boneMapJsonStr);
            Map.MapBones(controller.thisPlayerModel.bones, GetMappedBones());
            Map.SetBodyReplacement(this);
            ReparentModel();
            moreCompanyCosmeticsReparented = false;


        }


        internal void Awake()
        {
            
            controller = base.GetComponent<PlayerControllerB>();
            Console.WriteLine($"Awake {controller.playerUsername}");

            //Load model
            replacementModel = LoadAssetsAndReturnModel();

            if(replacementModel == null)
            {
                Console.WriteLine("LoadAssetsAndReturnModel returned null");
            }


            //Fix Materials
            Renderer[] renderers = replacementModel.GetComponentsInChildren<Renderer>();
            Material gameMat = controller.thisPlayerModel.GetComponent<SkinnedMeshRenderer>().material;
            foreach (Renderer renderer in renderers)
            {
                List<Material> mats = new List<Material>();
                foreach (Material material in renderer.materials)
                {
                    Material mat = new Material(gameMat);
                    mat.mainTexture = material.mainTexture;
                    mats.Add(mat);
                }
                renderer.SetMaterials(mats);

            }

            //Set scripts missing from assetBundle
            try
            {
                AddModelScripts();
            }
            catch (Exception e)
            {
                ModelReplacementAPI.Instance.Logger.LogError($"Could not set all model scripts.\n Error: {e.Message}");
            }
            

            //Instantiate model
            replacementModel = UnityEngine.Object.Instantiate<GameObject>(replacementModel);
            SetRenderers(false); //Initializing with renderers disabled prevents model flickering for local player
            replacementModel.transform.localPosition = new Vector3(0, 0, 0);
            replacementModel.SetActive(true);

            //sets y extents to the same size for player body and extents.
            var playerBodyExtents = controller.thisPlayerModel.bounds.extents;
            float scale = playerBodyExtents.y / GetBounds().extents.y;
            replacementModel.transform.localScale *= scale;

            //Get all .jsons in plugins and select the matching boneMap.json, deserialize bone map
            string pluginsPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            while (true)
            {
                string folder = new DirectoryInfo(pluginsPath).Name;
                if(folder == "plugins") { break; }
                pluginsPath = Path.Combine(pluginsPath, "..");
            }
            string[] allfiles = Directory.GetFiles(pluginsPath, "*.json", SearchOption.AllDirectories);
            jsonPath = allfiles.Where(f => Path.GetFileName(f) == boneMapFileName).First();
            boneMapJsonStr = File.ReadAllText(jsonPath);
            Map = BoneMap.DeserializeFromJson(boneMapJsonStr);

            //Map bones and parent mdodel
            Map.MapBones(controller.thisPlayerModel.bones, GetMappedBones());
            Map.SetBodyReplacement(this);
            ReparentModel();

            //Misc fixes
            var gameObjects = controller.gameObject.GetComponentsInChildren<MeshRenderer>();
            nameTagObj = gameObjects.Where(x => x.gameObject.name == "LevelSticker").First();
            nameTagObj2 = gameObjects.Where(x => x.gameObject.name == "BetaBadge").First();
            Console.WriteLine($"AwakeEnd {controller.playerUsername}");

            AfterAwake();
        }

        public void ReparentModel()
        {
            Map.RootBone().parent = controller.thisPlayerModel.rootBone;
            Map.RootBone().localPosition = Vector3.zero + Map.PositionOffset();
        }

        public virtual void AfterStart()
        {

        }
        void Start()
        {
            AfterStart();
        }

        public virtual void AfterUpdate()
        {

        }
        
        private void AttemptUnparentMoreCompanyCosmetics()
        {
            if (!ModelReplacementAPI.moreCompanyPresent) { return; }
            if (!moreCompanyCosmeticsReparented) { return; } //no cosmetics parented
            DangerousUnparent();
        }
        private void AttemptReparentMoreCompanyCosmetics()
        {
            if (!ModelReplacementAPI.moreCompanyPresent) { return; }
            if (moreCompanyCosmeticsReparented) { return; } //cosmetics already parented
            DangerousParent();

        }
        private void DangerousUnparent()
        {
            var applications = controller.gameObject.GetComponentsInChildren<CosmeticApplication>();
            if ((applications.Any()))
            {
                foreach (var application in applications)
                {
                    foreach (var cosmeticInstance in application.spawnedCosmetics)
                    {
                        cosmeticInstance.transform.parent = null;
                    }
                    moreCompanyCosmeticsReparented = false;
                }
            }
            Console.WriteLine(" unparent done");

        }
        private void DangerousParent()
        {
            var applications = controller.gameObject.GetComponentsInChildren<CosmeticApplication>();
            if ((applications.Any()))
            {
                foreach (var application in applications)
                {
                    Console.WriteLine($"{application.GetType().Name} parent");
                    Transform mappedHead = Map.GetMappedTransform("spine.004");
                    Transform mappedChest = Map.GetMappedTransform("spine.003");
                    Transform mappedLowerArmRight = Map.GetMappedTransform("arm.R_lower");
                    Transform mappedHip = Map.GetMappedTransform("spine");
                    Transform mappedShinLeft = Map.GetMappedTransform("shin.L");
                    Transform mappedShinRight = Map.GetMappedTransform("shin.R");


                    application.head = mappedHead;
                    application.chest = mappedChest;
                    application.lowerArmRight = mappedLowerArmRight;
                    application.hip = mappedHip;
                    application.shinLeft = mappedShinLeft;
                    application.shinRight = mappedShinRight;

                    foreach (var cosmeticInstance in application.spawnedCosmetics)
                    {
                        Transform transform = null;
                        switch (cosmeticInstance.cosmeticType)
                        {
                            case CosmeticType.HAT:
                                transform = application.head;
                                break;
                            case CosmeticType.CHEST:
                                transform = application.chest;
                                break;
                            case CosmeticType.R_LOWER_ARM:
                                transform = application.lowerArmRight;
                                break;
                            case CosmeticType.HIP:
                                transform = application.hip;
                                break;
                            case CosmeticType.L_SHIN:
                                transform = application.shinLeft;
                                break;
                            case CosmeticType.R_SHIN:
                                transform = application.shinRight;
                                break;
                        }
                        cosmeticInstance.transform.position = transform.position;
                        cosmeticInstance.transform.rotation = transform.rotation;
                        cosmeticInstance.transform.parent = transform;
                    }
                    moreCompanyCosmeticsReparented = true;
                }
                Console.WriteLine(" reparent done");
            }
        }

        void Update()
        {
            if (Map.CompletelyDestroyed())
            {
                Console.WriteLine("Map gone destroy");
                Destroy(this);
                return;
            }

            
            //Local/Nonlocal player logic
            if (!renderLocalDebug)
            {

                if (RenderBodyReplacement()) {
                    SetRenderers(true);
                    controller.thisPlayerModel.enabled = false; //Don't render original body if non-local player
                    controller.thisPlayerModelLOD1.enabled = false;
                    controller.thisPlayerModelLOD2.enabled = false;
                    nameTagObj.enabled = false;
                    nameTagObj2.enabled = false;
                    
                }
                else
                {
                    SetRenderers(false); // Don't render model replacement if local player
                }
            }
            else
            {
                foreach (Renderer renderer in controller.gameObject.GetComponentsInChildren<SkinnedMeshRenderer>())
                {
                    renderer.enabled = renderBase;
                }
                SetRenderers(renderModel);
            }


            //Update replacement model
            Map.UpdateModelbones();
            AttemptReparentMoreCompanyCosmetics();

            //Ragdoll
            if (ragdollEnabled)
            {
                GameObject deadBody = null;
                try
                {
                    deadBody = controller.deadBody.gameObject;
                }
                catch { }

                if ((deadBody) && (replacementDeadBody == null))
                {
                    CreateAndParentRagdoll(controller.deadBody);
                }
                if (replacementDeadBody)
                {
                    if (deadBody == null)
                    {
                        Destroy(replacementDeadBody);
                        replacementDeadBody = null;
                        ragDollMap = null;
                    }
                    else
                    {
                        ragDollMap.UpdateModelbones();
                    }
                }
            }
                
            //HeldItem handled through patch
            
            AfterUpdate();
        }

        void OnDestroy()
        {
            Console.WriteLine($"Destroy body component for {controller.playerUsername}");
            controller.thisPlayerModel.enabled = true;
            controller.thisPlayerModelLOD1.enabled = true;
            controller.thisPlayerModelLOD2.enabled = true;
           
            nameTagObj.enabled = true;
            nameTagObj2.enabled = true;
            AttemptUnparentMoreCompanyCosmetics();

            Destroy(replacementModel);
            Destroy(replacementDeadBody);
        }

        private void SetRenderers(bool enabled)
        {
            foreach (Renderer renderer in replacementModel.GetComponentsInChildren<Renderer>())
            {
                renderer.enabled = enabled;
            }
        }

        private Bounds GetBounds()
        {
            Bounds bounds = new Bounds();
            var allBounds = replacementModel.GetComponentsInChildren<SkinnedMeshRenderer>().Select(r => r.bounds);

            float maxX = allBounds.OrderByDescending(x => x.max.x).First().max.x;
            float maxY = allBounds.OrderByDescending(x => x.max.y).First().max.y;
            float maxZ = allBounds.OrderByDescending(x => x.max.z).First().max.z;

            float minX = allBounds.OrderBy(x => x.min.x).First().min.x;
            float minY = allBounds.OrderBy(x => x.min.y).First().min.y;
            float minZ = allBounds.OrderBy(x => x.min.z).First().min.z;

            bounds.SetMinMax(new Vector3(minX, minY, minZ), new Vector3(maxZ, maxY, maxZ));
            return bounds;
        }



    }
}
