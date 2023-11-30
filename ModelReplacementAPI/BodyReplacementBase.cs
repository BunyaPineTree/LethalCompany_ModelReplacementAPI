using GameNetcodeStuff;
using ModelReplacement;
using MoreCompany.Cosmetics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace ModelReplacement
{
    public abstract class BodyReplacementBase : MonoBehaviour
    {
        // public bool alive = true;
        public string boneMapJsonStr;

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

        //Settings
        internal bool ragdollEnabled = false;
        internal bool bloodDecalsEnabled = false;


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
        /// An array containing at least the bone transforms mapped in boneMap.json. Override if your model has more than one SkinnedMeshRenderer, bones in more than one SkinnedMeshRenderer, or any other complex bone layout. 
        /// </summary>
        /// <param name="modelReplacement">Use this parameter when returning your model's bone transforms so that it can work on the ragdoll as well</param>
        /// <returns></returns>
        public virtual Transform[] GetMappedBones(GameObject modelReplacement)
        {
            return GetArmatureSkinnedMeshRenderer(modelReplacement).bones;
        }

        /// <summary>
        /// The SkinnedMeshRenderer that is attached to your model's armature. Override if your model has more than one SkinnedMeshRenderer. (such as the base game player model)
        /// </summary>
        /// <param name="modelReplacement">Use this parameter when returning your model's SkinnedMeshRenderer so that it can work on the ragdoll as well</param>
        /// <returns></returns>
        public virtual SkinnedMeshRenderer GetArmatureSkinnedMeshRenderer(GameObject modelReplacement )
        {
            return modelReplacement.GetComponentInChildren<SkinnedMeshRenderer>();
        }


        /// <summary>
        /// AssetBundles do not supply scripts that are not supported by the base game. Programmatically set custom scripts here. 
        /// </summary>
        public abstract void AddModelScripts();

        private void CreateAndParentRagdoll(DeadBodyInfo bodyinfo)
        {
           // alive = false;
            deadBody = bodyinfo.gameObject;

            replacementDeadBody = UnityEngine.Object.Instantiate<GameObject>(replacementModel);
            SkinnedMeshRenderer replacementRenderer = GetArmatureSkinnedMeshRenderer(replacementDeadBody);
            SkinnedMeshRenderer deadBodyRenderer = deadBody.GetComponentInChildren<SkinnedMeshRenderer>();

            replacementRenderer.enabled = true;
            deadBodyRenderer.enabled = false;



            Transform[] deadMappedbones = GetMappedBones(replacementDeadBody);
            ragDollMap = BoneMap.DeserializeFromJson(boneMapJsonStr);
            ragDollMap.MapBones(deadBodyRenderer.bones, deadMappedbones);

            replacementRenderer.rootBone.parent = deadBodyRenderer.rootBone;
            replacementRenderer.rootBone.localPosition = Vector3.zero + ragDollMap.PositionOffset();


            //blood decals not working
            if(bloodDecalsEnabled)
            {
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

        }

        void Awake()
        {
            
            controller = base.GetComponent<PlayerControllerB>();
            Console.WriteLine($"Awake {controller.playerUsername}");

            //Load model
            replacementModel = LoadAssetsAndReturnModel();

            if(replacementModel is null)
            {
                Console.WriteLine("LoadAssetsAndReturnModel returned null");
            }


            //Fix Materials
            SkinnedMeshRenderer[] renderers = replacementModel.GetComponentsInChildren<SkinnedMeshRenderer>();
            Material gameMat = controller.thisPlayerModel.GetComponent<SkinnedMeshRenderer>().material;
            foreach (SkinnedMeshRenderer renderer in renderers)
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
            AddModelScripts();

            //Instantiate model and parent to player body
            replacementModel = UnityEngine.Object.Instantiate<GameObject>(replacementModel);
            SkinnedMeshRenderer replacementModelSkinnedMeshRenderer = GetArmatureSkinnedMeshRenderer(replacementModel);
            replacementModelSkinnedMeshRenderer.enabled = false; //prevents model flickering for local player
            replacementModel.transform.localPosition = new Vector3(0, 0, 0);
            replacementModel.SetActive(true);

            //sets y extents to the same size for player body and extents.
            var replacementExtents = replacementModelSkinnedMeshRenderer.bounds.extents;
            var playerBodyExtents = controller.thisPlayerModel.bounds.extents;
            float scale = playerBodyExtents.y / replacementExtents.y;
            replacementModel.transform.localScale *= scale;


            //Load and set boneMap 
            string pluginsPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            while (true)
            {
                string folder = new DirectoryInfo(pluginsPath).Name;
                if(folder == "plugins") { break; }
                Path.Combine(pluginsPath, "..");
            }

            string[] allfiles = Directory.GetFiles(pluginsPath, "*.json", SearchOption.AllDirectories);
            string jsonPath = allfiles.Where(f => Path.GetFileName(f) == boneMapFileName).First();
 
            boneMapJsonStr = File.ReadAllText(jsonPath);
            Map = BoneMap.DeserializeFromJson(boneMapJsonStr);
            Map.MapBones(controller.thisPlayerModel.bones, GetMappedBones(replacementModel));

            replacementModelSkinnedMeshRenderer.rootBone.parent = controller.thisPlayerModel.rootBone;
            replacementModelSkinnedMeshRenderer.rootBone.localPosition = Vector3.zero + Map.PositionOffset();


            var gameObjects = controller.gameObject.GetComponentsInChildren<MeshRenderer>();
            nameTagObj = gameObjects.Where(x => x.gameObject.name == "LevelSticker").First();
            nameTagObj2 = gameObjects.Where(x => x.gameObject.name == "BetaBadge").First();
            Console.WriteLine($"AwakeEnd {controller.playerUsername}");
        }

        void Start()
        {
            Console.WriteLine($"Start {controller.playerUsername}");
            foreach (var item in controller.gameObject.GetComponentsInChildren<CosmeticApplication>())
            {
                Transform mappedHead = Map.GetMappedTransform("spine.004");
                Transform mappedLowerArmRight = Map.GetMappedTransform("arm.R_lower");
                Transform mappedHip = Map.GetMappedTransform("spine");


                item.head = mappedHead;
                item.lowerArmRight = mappedLowerArmRight;
                item.hip = mappedHip;


            }
        }

        void Update()
        {
           // Console.WriteLine($"Awake {controller.playerUsername}");
            SkinnedMeshRenderer replacementModelSkinnedMeshRenderer = GetArmatureSkinnedMeshRenderer(replacementModel);
            replacementModelSkinnedMeshRenderer.enabled = true;
            bool localPlayer = (ulong)StartOfRound.Instance.thisClientPlayerId == controller.playerClientId;// Don't render miku if local player and alive
            if (localPlayer)
            {
                replacementModelSkinnedMeshRenderer.enabled = false;

            }
            //if (true)
            if (!localPlayer)
            {
                controller.thisPlayerModel.enabled = false;
                controller.thisPlayerModelLOD1.enabled = false;
                controller.thisPlayerModelLOD2.enabled = false;
                nameTagObj.enabled = false;
                nameTagObj2.enabled = false;

            }

            Map.UpdateModelbones();


            //Ragdoll
            if (ragdollEnabled)
            {
                GameObject deadBody = null;
                try
                {
                    deadBody = controller.deadBody.gameObject;
                }
                catch { }

                if ((deadBody != null) && (replacementDeadBody is null))
                {
                    CreateAndParentRagdoll(controller.deadBody);
                }
                if (replacementDeadBody)
                {
                    if (deadBody is null)
                    {
                        Destroy(replacementDeadBody);
                        replacementDeadBody = null;
                        ragDollMap = null;
                        return;
                    }
                    else
                    {
                        ragDollMap.UpdateModelbones();
                    }
                }
            }
            
            

            //Held Item
            Transform handTransform = Map.ItemHolder();
            //if(handTransform)
            if (!localPlayer && handTransform)
            {
                if (controller.currentlyGrabbingObject && (controller.currentlyGrabbingObject.parentObject != handTransform))
                {
                    GameObject HeldItemOffset = new GameObject();
                    Transform heldItemTransform = UnityEngine.Object.Instantiate<GameObject>(HeldItemOffset, handTransform).transform;
                    controller.currentlyGrabbingObject.parentObject = heldItemTransform;
                    heldItemTransform.localPosition += Map.ItemHolderPositionOffset();

                }
                if (controller.currentlyHeldObjectServer && (controller.currentlyHeldObjectServer.parentObject != handTransform))
                {
                    GameObject HeldItemOffset = new GameObject();
                    Transform heldItemTransform = UnityEngine.Object.Instantiate<GameObject>(HeldItemOffset, handTransform).transform;
                    controller.currentlyHeldObjectServer.parentObject = heldItemTransform;
                    heldItemTransform.localPosition += Map.ItemHolderPositionOffset();
                }
                if (controller.currentlyHeldObject && (controller.currentlyHeldObject.parentObject != handTransform))
                {
                    GameObject HeldItemOffset = new GameObject();
                    Transform heldItemTransform = UnityEngine.Object.Instantiate<GameObject>(HeldItemOffset, handTransform).transform;
                    controller.currentlyHeldObject.parentObject = heldItemTransform;
                    heldItemTransform.localPosition += Map.ItemHolderPositionOffset();
                }
            }
        }

        void OnDestroy()
        {
           // Console.WriteLine($"Destroy body component for {controller.playerUsername}");
            controller.thisPlayerModel.enabled = true;
            controller.thisPlayerModelLOD1.enabled = true;
            controller.thisPlayerModelLOD2.enabled = true;
            nameTagObj.enabled = true;
            nameTagObj2.enabled = true;
            Destroy(replacementModel);
            Destroy(replacementDeadBody);
        }


  
        



    }
}
