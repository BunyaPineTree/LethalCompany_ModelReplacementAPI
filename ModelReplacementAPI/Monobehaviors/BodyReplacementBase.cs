using GameNetcodeStuff;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.Rendering;
using AsmResolver.IO;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using System.Reflection.Emit;
using ModelReplacement.Scripts;
using ModelReplacement.Scripts.Player;
using ModelReplacement.Monobehaviors;
using ModelReplacement.AvatarBodyUpdater;

//        ModelReplacement, for backwards compatability.
namespace ModelReplacement
{
    public abstract class BodyReplacementBase : MonoBehaviour
    {
        public static List<BodyReplacementBase> allBodies = new();

        //Base components
        public AvatarUpdater avatar { get; private set; }
        public ViewModelUpdater viewModelAvatar { get; private set; }
        public ViewStateManager viewState = null;
        public MoreCompanyCosmeticManager cosmeticManager = null;
        public PlayerControllerB controller { get; private set; }
        public GameObject replacementModel;
        public GameObject replacementViewModel;
        public BoxCollider nameTagCollider = null;
        public GrabbableObject heldItem = null;

        //Ragdoll components
        public AvatarUpdater ragdollAvatar { get; private set; }
        public GameObject deadBody = null;
        public GameObject replacementDeadBody = null;

        //Shadow components
        public AvatarUpdater shadowAvatar { get; private set; }
        public GameObject replacementModelShadow = null;

        //Misc components
        private MaterialHelper matHelper = null;
        private int danceNumber = 0;
        private int previousDanceNumber = 0;
        public string suitName { get; set; } = "";

        //Mod Support components
        public AvatarUpdater cosmeticAvatar = null;
        public bool IsActive = true;

        //Settings
        public bool UseNoPostProcessing = false;
        public bool DontConvertUnsupportedShaders = false;
        public bool GenerateViewModel = false;
        public bool RemoveHelmet = false;

        //Debug
        public bool DebugRenderPlayer = false;
        public bool DebugRenderModel = false;

        #region Virtual and Abstract Methods

        /// <summary>
        /// Loads necessary assets from assetBundle, perform any necessary modifications on the replacement model and return it.
        /// </summary>
        /// <returns>Replacement model GameObject</returns>
        protected abstract GameObject LoadAssetsAndReturnModel();

        /// <summary>
        /// Loads necessary assets from assetBundle, perform any necessary modifications on the replacement viewModel and return it. Override if you intend on implementing your own viewmodel. 
        /// </summary>
        /// <returns>Replacement viewModel GameObject</returns>
        protected virtual GameObject LoadAssetsAndReturnViewModel()
        {
            return null;
        }
        /// <summary>
        /// Loads necessary assets from assetBundle, perform any necessary modifications on the replacement ragdoll and return it. Override if you intend on implementing your own viewmodel. 
        /// </summary>
        /// <returns>Replacement ragdoll GameObject</returns>
        protected virtual GameObject LoadAssetsAndReturnRagdoll(CauseOfDeath causeOfDeath)
        {
            return null;
        }

        /// <summary>
        /// Override this to return a derivative AvatarUpdater. Only do this if you really know what you are doing. 
        /// </summary>
        /// <returns></returns>
        protected virtual AvatarUpdater GetAvatarUpdater()
        {
            return new AvatarUpdater();
        }

        /// <summary>
        /// Override this to return a derivative ViewModelUpdater. Only do this if you really know what you are doing. 
        /// </summary>
        /// <returns></returns>
        protected virtual ViewModelUpdater GetViewModelUpdater()
        {
            return new ViewModelUpdater();
        }

        /// <summary>
        /// AssetBundles do not supply scripts that are not supported by the base game. Override to set custom scripts. 
        /// </summary>
        protected virtual void AddModelScripts()
        {

        }
        protected internal virtual void OnHitEnemy(bool dead)
        {
            Console.WriteLine($"PLAYER HIT ENEMY {controller.playerUsername}");
        }

        protected internal virtual void OnHitAlly(PlayerControllerB ally, bool dead)
        {
            Console.WriteLine($"PLAYER HIT ALLY {controller.playerUsername}");
        }

        protected internal virtual void OnDamageTaken(bool dead)
        {
            Console.WriteLine($"PLAYER TAKE DAMAGE  {controller.playerUsername}");
        }
        protected internal virtual void OnDamageTakenByAlly(PlayerControllerB ally, bool dead)
        {
            Console.WriteLine($"PLAYER TAKE DAMAGE BY ALLY {controller.playerUsername}");
        }

        protected internal virtual void OnDeath()
        {
            Console.WriteLine($"PLAYER DEATH {controller.playerUsername} ");
        }

        protected internal virtual void OnEmoteStart(int emoteId)
        {
            Console.WriteLine($"PLAYER EMOTE START {controller.playerUsername} ID {emoteId}");
        }
        protected internal virtual void OnEmoteEnd()
        {


            Console.WriteLine($"PLAYER EMOTE END {controller.playerUsername}");
        }


        #endregion

        #region LoadModels
        private GameObject LoadModelReplacement()
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
            GenerateViewModel = offsetBuilder.GenerateViewModel;
            RemoveHelmet = offsetBuilder.RemoveHelmet;
            Vector3 rootScale = offsetBuilder.rootScale;


            // Fix Materials and renderers
            Material gameMat = controller.thisPlayerModel.GetComponent<SkinnedMeshRenderer>().sharedMaterial;
            gameMat = new Material(gameMat); // Copy so that shared material isn't accidently changed by overriders of GetReplacementMaterial()

            Renderer[] renderers = tempReplacementModel.GetComponentsInChildren<Renderer>();
            SkinnedMeshRenderer[] skinnedRenderers = tempReplacementModel.GetComponentsInChildren<SkinnedMeshRenderer>();
            Dictionary<Material, Material> matMap = new();
            List<Material> materials = ListPool<Material>.Get();
            foreach (Renderer renderer in renderers)
            {
                renderer.renderingLayerMask = (1 << 0) + (1 << 9);
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
            //Vector3 playerBodyExtents = controller.thisPlayerModel.bounds.extents;
            float playerHeight = 1.465f; //Hardcode player height to account for emote mods. 
            float scale = playerHeight / GetBounds(tempReplacementModel).extents.y;
            tempReplacementModel.transform.localScale *= scale;

            Vector3 baseScale = tempReplacementModel.transform.localScale;
            tempReplacementModel.transform.localScale = Vector3.Scale(baseScale, rootScale);

            // Instantiate model
            tempReplacementModel = Instantiate(tempReplacementModel);
            tempReplacementModel.name += $"({controller.playerUsername})";
            tempReplacementModel.transform.localPosition = new Vector3(0, 0, 0);
            tempReplacementModel.SetActive(true);
            return tempReplacementModel;
        }
        private GameObject LoadViewModelreplacement()
        {
            //Generate a view model with the replacement model if requested
            GameObject TempReplacementViewModel = null;
            if (GenerateViewModel || ModelReplacementAPI.EnforceViewModelGeneration.Value)
            {
                //Instantiate new replacement model
                TempReplacementViewModel = Instantiate(replacementModel);
                TempReplacementViewModel.name += $"(ViewModel)";
                TempReplacementViewModel.transform.localPosition = new Vector3(0, 0, 0);
                TempReplacementViewModel.SetActive(false); //Prevent flicker -> doesn't actually work
                
                return MeshHelper.ConvertModelToViewModel(TempReplacementViewModel);

            }

            //Load custom viewModel, return null if it was null;
            TempReplacementViewModel = LoadAssetsAndReturnViewModel();
            if (TempReplacementViewModel == null) return null; //No viewmodel provided, return null

            // Fix materials and renderers on ViewModel
            Material gameMat = controller.thisPlayerModel.GetComponent<SkinnedMeshRenderer>().sharedMaterial;
            gameMat = new Material(gameMat); // Copy so that shared material isn't accidently changed by overriders of GetReplacementMaterial()
            Renderer[] renderersViewModel = TempReplacementViewModel.GetComponentsInChildren<Renderer>();
            SkinnedMeshRenderer[] skinnedRenderersViewModel = TempReplacementViewModel.GetComponentsInChildren<SkinnedMeshRenderer>();
            Dictionary<Material, Material> matMapViewModel = new();
            List<Material> materialsViewModel = ListPool<Material>.Get();
            foreach (Renderer renderer in renderersViewModel)
            {
                renderer.GetSharedMaterials(materialsViewModel);
                for (int i = 0; i < materialsViewModel.Count; i++)
                {
                    Material mat = materialsViewModel[i];
                    if (!matMapViewModel.TryGetValue(mat, out Material replacementMat))
                    {
                        matMapViewModel[mat] = replacementMat = matHelper.GetReplacementMaterial(gameMat, mat);
                    }
                    materialsViewModel[i] = replacementMat;
                }
                renderer.SetMaterials(materialsViewModel);
            }
            ListPool<Material>.Release(materialsViewModel);
            foreach (SkinnedMeshRenderer item in skinnedRenderersViewModel)
            {
                item.updateWhenOffscreen = true;
            }
            foreach (Camera cam in TempReplacementViewModel.GetComponentsInChildren<Camera>())
            {
                cam.enabled = false;
            }

            //Instantiate viewModel
            TempReplacementViewModel = Instantiate(TempReplacementViewModel);
            TempReplacementViewModel.name += $"({controller.playerUsername})(ViewModel)";
            TempReplacementViewModel.transform.localPosition = new Vector3(0, 0, 0);
            TempReplacementViewModel.SetActive(false); //Prevent flicker
            return TempReplacementViewModel;
        }
        private GameObject LoadRagdollReplacement(CauseOfDeath causeOfDeath)
        {
            // Load models
            GameObject tempReplacementModel = LoadAssetsAndReturnRagdoll(causeOfDeath);

            if (tempReplacementModel == null)
            {
                tempReplacementModel = Instantiate(replacementModel);
                tempReplacementModel.name += $"(Ragdoll)";
                return tempReplacementModel;
            }

            //Offset Builder Data
            var replacementAnimator = tempReplacementModel.GetComponentInChildren<Animator>();
            OffsetBuilder offsetBuilder = replacementAnimator.gameObject.GetComponent<OffsetBuilder>();
            Vector3 rootScale = offsetBuilder.rootScale;


            // Fix Materials and renderers
            Material gameMat = controller.thisPlayerModel.GetComponent<SkinnedMeshRenderer>().sharedMaterial;
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

                renderer.shadowCastingMode = ShadowCastingMode.On;
                renderer.gameObject.layer = viewState.VisibleLayer;
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
            //Vector3 playerBodyExtents = controller.thisPlayerModel.bounds.extents;
            float playerHeight = 1.465f; //Hardcode player height to account for emote mods. 
            float scale = playerHeight / GetBounds(tempReplacementModel).extents.y;
            tempReplacementModel.transform.localScale *= scale;

            Vector3 baseScale = tempReplacementModel.transform.localScale;
            tempReplacementModel.transform.localScale = Vector3.Scale(baseScale, rootScale);

            // Instantiate model
            tempReplacementModel = Instantiate(tempReplacementModel);
            tempReplacementModel.name += $"({controller.playerUsername})(Ragdoll)";
            tempReplacementModel.transform.localPosition = new Vector3(0, 0, 0);
            tempReplacementModel.SetActive(true);
            return tempReplacementModel;
        }
        #endregion


        #region Unity Logic
        protected virtual void Awake()
        {
            // basic
            controller = GetComponent<PlayerControllerB>();
            ModelReplacementAPI.Instance.Logger.LogInfo($"Awake {controller.playerUsername} {this}");
            viewState = GetComponent<ViewStateManager>();
            cosmeticManager = GetComponent<MoreCompanyCosmeticManager>();
            matHelper = new MaterialHelper();

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
            if (viewState.localPlayer)
            {
                replacementModelShadow = GameObject.Instantiate(replacementModel);
                replacementModelShadow.name += $"(Shadow)";
                foreach (Renderer renderer in replacementModelShadow.GetComponentsInChildren<Renderer>())
                {
                    renderer.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
                    renderer.gameObject.layer = viewState.VisibleLayer;
                }
            }
            replacementViewModel = LoadViewModelreplacement();

            // Assign avatars
            avatar = GetAvatarUpdater();
            shadowAvatar = GetAvatarUpdater();
            viewModelAvatar = GetViewModelUpdater();
            ragdollAvatar = new AvatarUpdater();
            cosmeticAvatar = avatar;
            avatar.AssignModelReplacement(controller.gameObject, replacementModel);
            shadowAvatar.AssignModelReplacement(controller.gameObject, replacementModelShadow);
            viewModelAvatar.AssignViewModelReplacement(controller.gameObject, replacementViewModel);

            //Misc
            SetAvatarRenderers(true);
            viewState.ReportBodyReplacementAddition(this);
            cosmeticManager.ReportBodyReplacementAddition(this);

            //Colliders for nametag
            GameObject colliderObj = new GameObject("MRAPINameCollider");
            colliderObj.layer = 23;
            colliderObj.transform.SetParent(replacementModel.transform);

            nameTagCollider = colliderObj.AddComponent<BoxCollider>();
            nameTagCollider.isTrigger = true;

            var target = colliderObj.AddComponent<RaycastTarget>();
            target.controller = controller;
            target.bodyReplacement = this;
            target.modelObj = replacementModel;

            allBodies.Add(this);
            ModelReplacementAPI.Instance.Logger.LogInfo($"AwakeEnd {controller.playerUsername}");
        }

        protected virtual void Start() { }

        public virtual void LateUpdate()
        {
            // Handle Ragdoll creation and destruction
            GameObject deadBody = null;
            try
            {
                deadBody = controller.deadBody.gameObject;
            }
            catch { }
            if (deadBody && replacementDeadBody == null) //Player died this frame
            {
                cosmeticAvatar = ragdollAvatar;
                CreateAndParentRagdoll(controller.deadBody);
                OnDeath();
            }
            if (replacementDeadBody && deadBody == null) //Player returned to life this frame
            {
                cosmeticAvatar = avatar;
                Destroy(replacementDeadBody);
                replacementDeadBody = null;
            }
            if (deadBody && !deadBody.activeInHierarchy)
            {
                if (replacementDeadBody != null)
                {
                    replacementDeadBody.SetActive(false);
                }
            }

            // Update replacement models
            avatar.Update();
            shadowAvatar.Update();
            ragdollAvatar.Update();
            viewModelAvatar.Update();
            cosmeticManager.UpdateModelReplacement();
            //UpdateItemTransform();

            //Bounding box calculation for nameTag
            if (replacementModel != null)
            {
                Bounds modelBounds = GetBounds(replacementModel);
                nameTagCollider.center = modelBounds.center;
                nameTagCollider.size = modelBounds.size;
            }

            //Emotes
            previousDanceNumber = danceNumber;
            if (controller.playerBodyAnimator != null)
            {
                int danceHash = controller.playerBodyAnimator.GetCurrentAnimatorStateInfo(1).fullPathHash;
                if (controller.performingEmote)
                {
                    if (danceHash == -462656950) { danceNumber = 1; }
                    else if (danceHash == 2103786480) { danceNumber = 2; }
                    else { danceNumber = 3; }
                }
                else { danceNumber = 0; }
                if (ModelReplacementAPI.tooManyEmotesPresent)
                {
                    danceNumber = SafeGetEmoteID(danceNumber);
                }

                if (danceNumber != previousDanceNumber)
                {
                    if (previousDanceNumber == 0) { StartCoroutine(WaitForDanceNumberChange()); } //Start new animation, takes time to switch to new animation state
                    else if (danceNumber == 0) { OnEmoteEnd(); } // No dance, where there was previously dance.
                    else { if (!emoteOngoing) { OnEmoteStart(danceNumber); } } //An animation did not start nor end, go immediately into the different animation
                }     
            }
        }

        static BodyReplacementBase()
        {
            RenderPipelineManager.beginContextRendering += ReallyLateUpdate;
        }

        public static void ReallyLateUpdate(ScriptableRenderContext context, List<Camera> cameras)
        {
            for (int i = 0; i < allBodies.Count; i++)
            {
                BodyReplacementBase body = allBodies[i];
                if (!body || !body.isActiveAndEnabled) continue;
                body.UpdateItemTransform();
            }
        }


        protected virtual void OnDestroy()
        {
            ModelReplacementAPI.Instance.Logger.LogInfo($"Destroy body component for {controller.playerUsername}");
            allBodies.Remove(this);

            Destroy(replacementModel);
            Destroy(replacementModelShadow);
            Destroy(replacementViewModel);
            Destroy(replacementDeadBody);
        }

        #endregion

        #region items
        public void UpdateItemTransform()
        {
            if (!heldItem) return;
            if (heldItem.parentObject == null || heldItem.playerHeldBy != controller || controller.ItemSlots[controller.currentItemSlot] != heldItem)
            {
                heldItem = null;
                return;
            }

            bool inFirstPerson = viewState.GetViewState() == ViewState.FirstPerson;

            if(inFirstPerson)
            {
                if(heldItem.itemProperties.twoHandedAnimation)
                    heldItem.transform.Translate((viewModelAvatar.ItemOffsetLeft + viewModelAvatar.ItemOffsetRight) / 2, Space.World);
                else
                    heldItem.transform.Translate(viewModelAvatar.ItemOffsetRight, Space.World);
            }
            else
            {
                if (viewState.localPlayer)
                {
                    // Reset transform to serverItemHolder position
                    heldItem.transform.rotation = controller.serverItemHolder.rotation;
                    heldItem.transform.Rotate(heldItem.itemProperties.rotationOffset);
                    heldItem.transform.position = controller.serverItemHolder.position + controller.serverItemHolder.rotation * heldItem.itemProperties.positionOffset;
                }

                if(heldItem.itemProperties.twoHandedAnimation)
                    heldItem.transform.Translate((avatar.ItemOffsetLeft + avatar.ItemOffsetRight) / 2, Space.World);
                else
                    heldItem.transform.Translate(avatar.ItemOffsetRight, Space.World);
            }

            // Update jetpack backpack
            if(heldItem is JetpackItem jet)
            {
                Quaternion baseRot = avatar.lowerSpine.rotation * avatar.jetpackRotOffset;

                jet.backPart.rotation = baseRot;
                jet.backPart.Rotate(jet.backPartRotationOffset);
                jet.backPart.position = avatar.lowerSpine.position + baseRot * jet.backPartPositionOffset;
            }
        }
        #endregion


        #region Helpers, Materials, Ragdolls, Rendering, etc...

        static List<Transform> GetAllTransforms(Transform parent)
        {
            var transformList = new List<Transform>();
            BuildTransformList(transformList, parent);
            return transformList;
        }

        private static void BuildTransformList(ICollection<Transform> transforms, Transform parent)
        {
            if (parent == null) { return; }
            foreach (Transform t in parent)
            {
                transforms.Add(t);
                BuildTransformList(transforms, t);
            }
        }

        private void CreateAndParentRagdoll(DeadBodyInfo bodyinfo)
        {
            deadBody = bodyinfo.gameObject;
            deadBody.layer = ViewStateManager.ragdollLayer;

            //Instantiate replacement Ragdoll and assign the avatar
            SkinnedMeshRenderer deadBodyRenderer = deadBody.GetComponentInChildren<SkinnedMeshRenderer>();
            replacementDeadBody = LoadRagdollReplacement(bodyinfo.causeOfDeath);
            ragdollAvatar.AssignModelReplacement(deadBody, replacementDeadBody);

            foreach (var item in GetAllTransforms(replacementDeadBody.transform))
            {
                item.gameObject.layer = ViewStateManager.ragdollLayer;
            }

            //Enable all renderers in replacement ragdoll and disable renderer for original
            foreach (Renderer renderer in replacementDeadBody.GetComponentsInChildren<Renderer>())
            {
                renderer.enabled = true;
                renderer.shadowCastingMode = ShadowCastingMode.On;
                renderer.gameObject.layer = viewState.VisibleLayer;
                renderer.renderingLayerMask = (1 << 0) + (1 << 9);
            }
            deadBodyRenderer.enabled = false;



            Console.WriteLine("Ragdoll Creation.");

            //blood decals not working?
            foreach (GameObject item in bodyinfo.bodyBloodDecals)
            {
                Transform bloodParentTransform = item.transform.parent;

                Transform mappedTranform = ragdollAvatar.GetAvatarTransformFromBoneName(bloodParentTransform.name);
                if (mappedTranform)
                {
                    Instantiate(item, mappedTranform);
                }
            }



        }


        public void SetAvatarRenderers(bool enabled)
        {
            foreach (Renderer renderer in replacementModel.GetComponentsInChildren<Renderer>())
            {
                renderer.enabled = enabled;
            }
            if (replacementViewModel != null)
            {
                foreach (Renderer renderer in replacementViewModel.GetComponentsInChildren<Renderer>())
                {
                    renderer.enabled = enabled;
                }
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

        #region Coroutines
        private bool emoteOngoing = false;
        private IEnumerator WaitForDanceNumberChange()
        {
            if (emoteOngoing) { yield break; }
            emoteOngoing = true;
            int frame = 0;
            while (frame < 20)
            {
                if (danceNumber == 0) { emoteOngoing = false; yield break; }
                yield return new WaitForEndOfFrame();
                frame++;
            }
            if (danceNumber != 0) { emoteOngoing = false; OnEmoteStart(danceNumber); }
        }



        #endregion

        #region TooManyEmotes

        private int SafeGetEmoteID(int currentID)
        {
            return DangerousGetEmoteID(currentID);
        }

        private int DangerousGetEmoteID(int currentID)
        {
            TooManyEmotes.UnlockableEmote anim = TooManyEmotes.Patches.PlayerPatcher.GetCurrentlyPlayingEmote(controller);
            if (anim == null) { return currentID; }
            if (anim.emoteId == 1) { return -1; }
            if (anim.emoteId == 2) { return -2; }
            if (anim.emoteId == 3) { return -3; }
            return anim.emoteId;

        }

        #endregion


        public class RaycastTarget : MonoBehaviour
        {
            public PlayerControllerB controller = null;
            public BodyReplacementBase bodyReplacement = null;
            public GameObject modelObj = null;

            private void Update()
            {
                if (bodyReplacement == null && modelObj != null)
                {

                }
            }
        }
    }
}
