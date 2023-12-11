using _3rdPerson.Helper;
using GameNetcodeStuff;
using LCThirdPerson;
using ModelReplacement;
using MoreCompany.Cosmetics;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.HID;
using ModelReplacement.AvatarBodyUpdater;
using UnityEngine.Pool;

namespace ModelReplacement
{
    public abstract class BodyReplacementBase : MonoBehaviour
    {
        private bool localPlayer => (ulong)StartOfRound.Instance.thisClientPlayerId == controller.playerClientId;

        //Debug variables, if renderLocalDebug is true, renderBase and renderModel will decide what to render
        public bool renderLocalDebug = false;
        public bool renderBase = false;
        public bool renderModel = false;

        //Base components
        public AvatarUpdater avatar { get; private set; }
        public PlayerControllerB controller { get; private set; }
        protected GameObject replacementModel;

        //Ragdoll components
        public AvatarUpdater ragdollAvatar { get; private set; }
        protected GameObject deadBody = null;
        protected GameObject replacementDeadBody = null;

        //Misc components
        private MeshRenderer nameTagObj = null;
        private MeshRenderer nameTagObj2 = null;
        private bool moreCompanyCosmeticsReparented = false;


        /// <summary>
        /// Loads necessary assets from assetBundle, perform any necessary modifications on the replacement character model and return it.
        /// </summary>
        /// <returns>Model replacement GameObject</returns>
        protected abstract GameObject LoadAssetsAndReturnModel();


        /// <summary>
        /// AssetBundles do not supply scripts that are not supported by the base game. Override to set custom scripts. 
        /// </summary>
        protected virtual void AddModelScripts()
        {

        }


        void Awake()
        {
            
            controller = base.GetComponent<PlayerControllerB>();
            ModelReplacementAPI.Instance.Logger.LogInfo($"Awake {controller.playerUsername}");

            // Load model
            replacementModel = LoadAssetsAndReturnModel();

            if(replacementModel == null)
            {
                ModelReplacementAPI.Instance.Logger.LogFatal("LoadAssetsAndReturnModel() returned null. Verify that your assetbundle works and your asset name is correct. ");
            }


            // Fix Materials
            Renderer[] renderers = replacementModel.GetComponentsInChildren<Renderer>();
            Material gameMat = controller.thisPlayerModel.GetComponent<SkinnedMeshRenderer>().sharedMaterial;
            gameMat = new Material(gameMat); // Copy so that shared material isn't accidently changed by overriders of GetReplacementMaterial()
            Dictionary<Material, Material> matMap = new();
            List<Material> materials = ListPool<Material>.Get();
            foreach (Renderer renderer in renderers)
            {
                renderer.GetSharedMaterials(materials);
                for (int i = 0; i < materials.Count; i++)
                {
                    Material mat = materials[i];
                    if (!matMap.TryGetValue(mat, out var replacementMat))
                    {
                        matMap[mat] = replacementMat = GetReplacementMaterial(gameMat, mat);
                    }
                    materials[i] = replacementMat;
                }
                renderer.SetMaterials(materials);
            }
            ListPool<Material>.Release(materials);

            // Set scripts missing from assetBundle
            try
            {
                AddModelScripts();
            }
            catch (Exception e)
            {
                ModelReplacementAPI.Instance.Logger.LogError($"Could not set all model scripts.\n Error: {e.Message}");
            }
            

            // Instantiate model
            replacementModel = UnityEngine.Object.Instantiate<GameObject>(replacementModel);
            replacementModel.name += $"({controller.playerUsername})";
            SetRenderers(false); //Initializing with renderers disabled prevents model flickering for local player
            replacementModel.transform.localPosition = new Vector3(0, 0, 0);
            replacementModel.SetActive(true);

            // Sets y extents to the same size for player body and extents.
            var playerBodyExtents = controller.thisPlayerModel.bounds.extents;
            float scale = playerBodyExtents.y / GetBounds().extents.y;
            replacementModel.transform.localScale *= scale;


            // Assign the avatar
            avatar = new AvatarUpdater();
            ragdollAvatar = new AvatarUpdater();
            avatar.AssignModelReplacement(controller.gameObject, replacementModel);


            // Misc fixes
            var gameObjects = controller.gameObject.GetComponentsInChildren<MeshRenderer>();
            nameTagObj = gameObjects.Where(x => x.gameObject.name == "LevelSticker").First();
            nameTagObj2 = gameObjects.Where(x => x.gameObject.name == "BetaBadge").First();
            ModelReplacementAPI.Instance.Logger.LogInfo($"AwakeEnd {controller.playerUsername}");

            AfterAwake();
        }

		void Update()
        {
            // Local/Nonlocal renderer logic
            if (!renderLocalDebug)
            {

                if (RenderBodyReplacement()) {
                    SetRenderers(true);
                    controller.thisPlayerModel.enabled = false; // Don't render original body if non-local player
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

            // Handle Ragdoll creation and destruction
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
            if ((replacementDeadBody) && (deadBody == null))
            {
                Destroy(replacementDeadBody);
                replacementDeadBody = null;
            }

            // Update replacement models
            avatar.UpdateModel();
            ragdollAvatar.UpdateModel();
            AttemptReparentMoreCompanyCosmetics();


            AfterUpdate();
        }

        void OnDestroy()
        {
            ModelReplacementAPI.Instance.Logger.LogInfo($"Destroy body component for {controller.playerUsername}");
            controller.thisPlayerModel.enabled = true;
            controller.thisPlayerModelLOD1.enabled = true;
            controller.thisPlayerModelLOD2.enabled = true;
           
            nameTagObj.enabled = true;
            nameTagObj2.enabled = true;
            AttemptUnparentMoreCompanyCosmetics();

            Destroy(replacementModel);
            Destroy(replacementDeadBody);
        }


        /// <summary> Shaders with any of these prefixes won't be automatically converted. </summary>
        private static readonly string[] shaderPrefixWhitelist =
        {
            "HDRP/",
            "GUI/",
            "Sprites/",
            "UI/",
            "Unlit/",
        };

        /// <summary>
        /// Get a replacement material based on the original game material, and the material found on the replacing model.
        /// </summary>
        /// <param name="gameMaterial">The equivalent material on the model being replaced.</param>
        /// <param name="modelMaterial">The material on the replacing model.</param>
        /// <returns>The replacement material created from the <see cref="gameMaterial"/> and the <see cref="modelMaterial"/></returns>
        protected virtual Material GetReplacementMaterial(Material gameMaterial, Material modelMaterial)
        {
            if (shaderPrefixWhitelist.Any(prefix => modelMaterial.shader.name.StartsWith(prefix)))
            {
                return modelMaterial;
            }
            else
            {
                // XXX Ideally this material would be manually destroyed when the replacement model is destroyed.
                Material replacementMat = new Material(gameMaterial);
                replacementMat.color = modelMaterial.color;
                replacementMat.mainTexture = modelMaterial.mainTexture;
                replacementMat.mainTextureOffset = modelMaterial.mainTextureOffset;
                replacementMat.mainTextureScale = modelMaterial.mainTextureScale;
                replacementMat.SetShaderKeywords(modelMaterial.GetShaderKeywords());
                replacementMat.SetEnabledKeywords(modelMaterial.GetEnabledKeywords());

                return replacementMat;
            }
        }

        private void CreateAndParentRagdoll(DeadBodyInfo bodyinfo)
        {
            deadBody = bodyinfo.gameObject;

            //Instantiate replacement Ragdoll and assign the avatar
            SkinnedMeshRenderer deadBodyRenderer = deadBody.GetComponentInChildren<SkinnedMeshRenderer>();
            replacementDeadBody = UnityEngine.Object.Instantiate<GameObject>(replacementModel);
            replacementDeadBody.name += $"(Ragdoll)";
            ragdollAvatar.AssignModelReplacement(deadBody, replacementDeadBody);


            //Enable all renderers in replacement ragdoll and disable renderer for original
            foreach (Renderer renderer in replacementDeadBody.GetComponentsInChildren<Renderer>())
            {
                renderer.enabled = true;
            }
            deadBodyRenderer.enabled = false;

  
            //blood decals not working
            foreach (var item in bodyinfo.bodyBloodDecals)
            {
                Transform bloodParentTransform = item.transform.parent;

                Transform mappedTranform = ragdollAvatar.GetAvatarTransformFromBoneName(bloodParentTransform.name);
                if (mappedTranform)
                {
                    UnityEngine.Object.Instantiate<GameObject>(item, mappedTranform);
                }
            }

        }

        private void SetRenderers(bool enabled)
        {
            foreach (Renderer renderer in replacementModel.GetComponentsInChildren<Renderer>())
            {
                renderer.enabled = enabled;
            }
        }

        /// <summary>
        /// Returns whether the local client can render the body replacement. This is a factor of whether the body replacement belongs to their player, and whether they are using a third person mod. 
        /// </summary>
        /// <returns></returns>
        public bool RenderBodyReplacement()
        {
            if (!localPlayer) { return true; }
            if (ModelReplacementAPI.thirdPersonPresent)
            {
                return DangerousViewState();
            }
            if (ModelReplacementAPI.LCthirdPersonPresent)
            {
                return DangerousLCViewState();
            }
            return false;
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

        #region Third Person Mods Logic
        private bool DangerousViewState()
        {
            return ThirdPersonCamera.ViewState;
        }
        private bool DangerousLCViewState()
        {
            return ThirdPersonPlugin.Instance.Enabled;
        }
        #endregion
        #region MoreCompany Cosmetics Logic
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
        }
        private void DangerousParent()
        {
            var applications = controller.gameObject.GetComponentsInChildren<CosmeticApplication>();
            if ((applications.Any()))
            {
                foreach (var application in applications)
                {
                    Transform mappedHead = avatar.GetAvatarTransformFromBoneName("spine.004");
                    Transform mappedChest = avatar.GetAvatarTransformFromBoneName("spine.003");
                    Transform mappedLowerArmRight = avatar.GetAvatarTransformFromBoneName("arm.R_lower");
                    Transform mappedHip = avatar.GetAvatarTransformFromBoneName("spine");
                    Transform mappedShinLeft = avatar.GetAvatarTransformFromBoneName("shin.L");
                    Transform mappedShinRight = avatar.GetAvatarTransformFromBoneName("shin.R");


                    application.head = mappedHead;
                    application.chest = mappedChest;
                    application.lowerArmRight = mappedLowerArmRight;
                    application.hip = mappedHip;
                    application.shinLeft = mappedShinLeft;
                    application.shinRight = mappedShinRight;

                    application.RefreshAllCosmeticPositions();
                    moreCompanyCosmeticsReparented = true;
                }
                Console.WriteLine(" reparent done");
            }
        }
        #endregion

        // Optional user methods
        protected virtual void AfterStart()
        {

        }
        void Start() //Only exists for posterity
        {
            AfterStart();
        }
        protected virtual void AfterAwake()
        {

        }
        protected virtual void AfterUpdate()
        {

        }
       

    }
}
