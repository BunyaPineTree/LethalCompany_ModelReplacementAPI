using _3rdPerson.Helper;
using GameNetcodeStuff;
using ModelReplacement.AvatarBodyUpdater;
using ModelReplacement.Modules;
using Steamworks;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem.XR;
using UnityEngine.Pool;
using UnityEngine.Rendering;

namespace ModelReplacement.Enemies
{
    public abstract class EnemyReplacementBase<T> : MonoBehaviour where T : EnemyAI
    {
        
        //Base components
        public EnemyAvatarUpdaterBase avatar { get; private set; }
        public T enemyAI { get; private set; }
        public GameObject replacementModel;
        public BoxCollider nameTagCollider = null;
        public GrabbableObject heldItem = null;
        private MaterialHelperEnemy matHelper = null;


        //Settings
        public bool UseNoPostProcessing = false;
        public bool DontConvertUnsupportedShaders = false;

        #region Virtual and Abstract Methods

        /// <summary>
        /// Loads necessary assets from assetBundle, perform any necessary modifications on the replacement model and return it.
        /// </summary>
        /// <returns>Replacement model GameObject</returns>
        protected abstract GameObject LoadAssetsAndReturnModel();

        /// <summary>
        /// Override this to return a derivative AvatarUpdater. Only do this if you really know what you are doing. 
        /// </summary>
        /// <returns></returns>
        protected virtual EnemyAvatarUpdaterBase GetAvatarUpdater()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// AssetBundles do not supply scripts that are not supported by the base game. Override to set custom scripts. 
        /// </summary>
        protected virtual void AddModelScripts()
        {

        }
        #endregion

        #region LoadModels
        protected GameObject LoadModelReplacement()
        {
            // Load models
            GameObject tempReplacementModel = LoadAssetsAndReturnModel();
            if (tempReplacementModel == null)
            {
                ModelReplacementAPI.Instance.Logger.LogFatal("LoadAssetsAndReturnModel() returned null. Verify that your assetbundle works and your asset name is correct. ");
            }

            //Offset Builder Data
            var replacementAnimator = tempReplacementModel.GetComponentInChildren<Animator>();
            OffsetBuilder offsetBuilder = replacementAnimator.gameObject.GetComponent<OffsetBuilder>();
            UseNoPostProcessing = offsetBuilder.UseNoPostProcessing;
            Vector3 rootScale = offsetBuilder.rootScale;


            // Fix Materials and renderers
            Material gameMat = enemyAI.skinnedMeshRenderers[0].sharedMaterial;
            gameMat = new Material(gameMat); // Copy so that shared material isn't accidently changed by overriders of GetReplacementMaterial()

            Renderer[] renderers = tempReplacementModel.GetComponentsInChildren<Renderer>();
            SkinnedMeshRenderer[] skinnedRenderers = tempReplacementModel.GetComponentsInChildren<SkinnedMeshRenderer>();
            Dictionary<Material, Material> matMap = new();
            List<Material> materials = ListPool<Material>.Get();
            foreach (Renderer renderer in renderers)
            {
                renderer.GetSharedMaterials(materials);
                for (int i = 0; i < materials.Count; i++)
                {
                    Material mat = materials[i];
                    if (!matMap.TryGetValue(mat, out Material replacementMat))
                    {
                        matMap[mat] = replacementMat = matHelper.GetReplacementMaterial(gameMat, mat);
                    }
                    materials[i] = replacementMat;
                }
                renderer.SetMaterials(materials);
            }
            ListPool<Material>.Release(materials);
            foreach (SkinnedMeshRenderer item in skinnedRenderers)
            {
                item.updateWhenOffscreen = true;
            }
            foreach (Camera cam in tempReplacementModel.GetComponentsInChildren<Camera>())
            {
                cam.enabled = false;
            }

            // Sets y extents to the same size for player body and extents.
            Vector3 playerBodyExtents = enemyAI.skinnedMeshRenderers[0].bounds.extents;
            //float playerHeight = 1.465f; //Hardcode player height to account for emote mods. 
            float scale = playerBodyExtents.y / GetBounds(tempReplacementModel).extents.y;
            tempReplacementModel.transform.localScale *= scale;

            Vector3 baseScale = tempReplacementModel.transform.localScale;
            tempReplacementModel.transform.localScale = Vector3.Scale(baseScale, rootScale);

            // Instantiate model
            tempReplacementModel = UnityEngine.Object.Instantiate<GameObject>(tempReplacementModel);
            tempReplacementModel.name += $"({enemyAI.enemyType.name})";
            tempReplacementModel.transform.localPosition = new Vector3(0, 0, 0);
            tempReplacementModel.SetActive(true);
            return tempReplacementModel;
        }
        #endregion


        #region Unity Logic
        protected virtual void Awake()
        {
            // basic
            enemyAI = base.GetComponent<T>();
            ModelReplacementAPI.Instance.Logger.LogInfo($"Awake {enemyAI.enemyType.name} {this}");

            matHelper = new MaterialHelperEnemy(this);

            // Load Models 
            replacementModel = LoadModelReplacement();
            try
            {
                AddModelScripts();
            }
            catch (Exception e)
            {
                ModelReplacementAPI.Instance.Logger.LogError($"Could not set all model scripts.\n Error: {e.Message}");
            }
           
            // Assign avatars
            avatar = GetAvatarUpdater();
            avatar.AssignModelReplacement(enemyAI.gameObject, replacementModel);
            SetAvatarRenderers(true);
  
            ModelReplacementAPI.Instance.Logger.LogInfo($"AwakeEnd {enemyAI.enemyType.name}");
        }

        protected virtual void Start() {}

        public virtual void Update()
        {
            SetPlayerRenderers(false);
            SetPlayerLayers(InvisibleLayer);
            SetAvatarLayers(VisibleLayer, ShadowCastingMode.On);

        }

        public virtual void LateUpdate()
        {
            avatar.Update();
            //UpdateItemTransform();
        }
        public int VisibleLayer => UseNoPostProcessing ? ViewStateManager.noPostVisibleLayer : ViewStateManager.visibleLayer;
        public int InvisibleLayer => ViewStateManager.invisibleLayer;

        public void SetPlayerRenderers(bool enabled)
        {
            foreach(var renderer in enemyAI.skinnedMeshRenderers)
            {
                renderer.enabled = enabled;
                renderer.shadowCastingMode = enabled ? ShadowCastingMode.On : ShadowCastingMode.Off;
            }
        }
        public void SetPlayerLayers(int layer)
        {
            foreach (var renderer in enemyAI.skinnedMeshRenderers)
            {
                renderer.gameObject.layer = layer;
            }
        }
        public void SetAvatarLayers(int layer, ShadowCastingMode mode)
        {
            if (replacementModel == null) { return; }
            Renderer[] renderers = replacementModel.GetComponentsInChildren<Renderer>();
            foreach (Renderer renderer in renderers)
            {
                renderer.shadowCastingMode = mode;
                renderer.gameObject.layer = layer;
            }
        }



        protected virtual void OnDestroy()
        {
            ModelReplacementAPI.Instance.Logger.LogInfo($"Destroy body component for {enemyAI.enemyType.name}");
            Destroy(replacementModel);
        }

        #endregion

        #region Helpers, Materials, Ragdolls, Rendering, etc...
        public void UpdateItemTransform()
        {
            return;
            /*
            if (!heldItem) return;
            if (heldItem.parentObject == null || heldItem.playerHeldBy == null) return;
            if (heldItem.playerHeldBy != enemyAI)
            {
                heldItem = null;
                return;
            }

            Transform parentObject = avatar.ItemHolder;
            parentObject.localPosition = avatar.ItemHolderPositionOffset;

            heldItem.transform.rotation = heldItem.parentObject.rotation;
            heldItem.transform.Rotate(heldItem.itemProperties.rotationOffset);
            heldItem.transform.position = parentObject.position;
            Vector3 vector = heldItem.itemProperties.positionOffset;
            vector = heldItem.parentObject.rotation * vector;
            heldItem.transform.position += vector;
            */
        }
       


        public void SetAvatarRenderers(bool enabled)
        {
            foreach (Renderer renderer in replacementModel.GetComponentsInChildren<Renderer>())
            {
                renderer.enabled = enabled;
            }
        }

        private Bounds GetBounds(GameObject model)
        {
            Bounds bounds = new Bounds();
            IEnumerable<Bounds> allBounds = model.GetComponentsInChildren<SkinnedMeshRenderer>().Select(r => r.bounds);

            float maxX = allBounds.OrderByDescending(x => x.max.x).First().max.x;
            float maxY = allBounds.OrderByDescending(x => x.max.y).First().max.y;
            float maxZ = allBounds.OrderByDescending(x => x.max.z).First().max.z;

            float minX = allBounds.OrderBy(x => x.min.x).First().min.x;
            float minY = allBounds.OrderBy(x => x.min.y).First().min.y;
            float minZ = allBounds.OrderBy(x => x.min.z).First().min.z;


            bounds.SetMinMax(new Vector3(minX, minY, minZ), new Vector3(maxX, maxY, maxZ));
            return bounds;
        }

        #endregion

    }
}
