using ModelReplacement.AvatarBodyUpdater;
using ModelReplacement.Scripts;
using ModelReplacement.Scripts.Player;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Pool;

//        ModelReplacement, for backwards compatability.
namespace ModelReplacement
{
    public abstract class BodyReplacementBase : MonoBehaviour
    {

        //Base components
        public AvatarUpdater avatar { get; private set; }
        public ViewModelUpdater viewModelAvatar { get; private set; }
        public ViewStateManager viewState = null;
        public GameObject replacementModel;
        public GameObject replacementViewModel;


        //Misc components
        private MaterialHelper matHelper = null;
        private BoxCollider nameTagCollider = null;

        //Mod Support components
        public bool IsActive = true;

        //Settings
        public bool UseNoPostProcessing = false;
        public bool DontConvertUnsupportedShaders = false;
        public bool GenerateViewModel = false;


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
            Animator replacementAnimator = tempReplacementModel.GetComponentInChildren<Animator>();
            OffsetBuilder offsetBuilder = replacementAnimator.gameObject.GetComponent<OffsetBuilder>();
            UseNoPostProcessing = offsetBuilder.UseNoPostProcessing;
            GenerateViewModel = offsetBuilder.GenerateViewModel;
            Vector3 rootScale = offsetBuilder.rootScale;


            // Fix Materials and renderers
            // Find some way to get the ingame material to translate unsupported materials. 
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
            // Get a new hardcoded player height
            float playerHeight = 1.465f; //Hardcode player height to account for emote mods. 
            float scale = playerHeight / GetBounds(tempReplacementModel).extents.y;
            tempReplacementModel.transform.localScale *= scale;

            Vector3 baseScale = tempReplacementModel.transform.localScale;
            tempReplacementModel.transform.localScale = Vector3.Scale(baseScale, rootScale);

            // Instantiate model
            tempReplacementModel = Instantiate(tempReplacementModel);
            //tempReplacementModel.name += $"({controller.playerUsername})";
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
            // Find some way to get the ingame material to translate unsupported materials. 
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
            //TempReplacementViewModel.name += $"({controller.playerUsername})(ViewModel)";
            TempReplacementViewModel.transform.localPosition = new Vector3(0, 0, 0);
            TempReplacementViewModel.SetActive(false); //Prevent flicker
            return TempReplacementViewModel;
        }

        #endregion


        #region Unity Logic
        protected virtual void Awake()
        {
            // basic
            //controller = GetComponent<PlayerControllerB>();
            viewState = GetComponent<ViewStateManager>();
            matHelper = new MaterialHelper();

            // Load Models 
            replacementModel = LoadModelReplacement();
            replacementViewModel = LoadViewModelreplacement();

            // Assign avatars
            avatar = GetAvatarUpdater();
            viewModelAvatar = GetViewModelUpdater();

            // Get the character object through an analog for the PlayerControllerB, or otherwise
            avatar.AssignModelReplacement(controller.gameObject, replacementModel);
            viewModelAvatar.AssignViewModelReplacement(controller.gameObject, replacementViewModel);

            //Misc
            SetAvatarRenderers(true);
            viewState.ReportBodyReplacementAddition(this);


            // This section below is for the implementation of nameTags, which may or may not be necessary in Content Warning

            //Colliders for nametag
            GameObject colliderObj = new GameObject("MRAPINameCollider");
            colliderObj.layer = 23;
            colliderObj.transform.SetParent(replacementModel.transform);

            nameTagCollider = colliderObj.AddComponent<BoxCollider>();
            nameTagCollider.isTrigger = true;

            // RaycastTarget may or may not be necessary, but it is used in Lethal Company to display nametags
            RaycastTarget target = colliderObj.AddComponent<RaycastTarget>();
            //target.controller = controller;
            target.bodyReplacement = this;
            target.modelObj = replacementModel;


        }

        protected virtual void Start() { }

        public virtual void LateUpdate()
        {

            // Update replacement models
            avatar.Update();
            viewModelAvatar.Update();
            UpdateItemTransform();

            //Bounding box calculation for nameTag
            Bounds modelBounds = GetBounds(replacementModel);
            nameTagCollider.center = modelBounds.center;
            nameTagCollider.size = modelBounds.size;

        }



        protected virtual void OnDestroy()
        {
            Destroy(replacementModel);
            Destroy(replacementViewModel);
        }

        #endregion

        #region items
        public bool CanPositionItemOnCustomViewModel => (replacementViewModel != null) && (viewModelAvatar.ItemHolderViewModel != null);
        public void UpdateItemTransform()
        {
            // Item logic, probably specific to the game

            /*
            if (!heldItem) return;
            if (heldItem.parentObject == null || heldItem.playerHeldBy == null) return;
            if (heldItem.playerHeldBy != controller)
            {
                heldItem = null;
                return;
            }

            if (viewState.GetViewState() == ViewState.ThirdPerson)
            {
                Transform parentObject = avatar.ItemHolder;
                parentObject.localPosition = avatar.ItemHolderPositionOffset;

                heldItem.transform.rotation = heldItem.parentObject.rotation;
                heldItem.transform.Rotate(heldItem.itemProperties.rotationOffset);
                heldItem.transform.position = parentObject.position;
                Vector3 vector = heldItem.itemProperties.positionOffset;
                vector = heldItem.parentObject.rotation * vector;
                heldItem.transform.position += vector;
            }

            if (viewState.GetViewState() == ViewState.FirstPerson && CanPositionItemOnCustomViewModel)
            {
                Transform parentObject = viewModelAvatar.ItemHolderViewModel;
                parentObject.localPosition = avatar.ItemHolderPositionOffset;

                heldItem.transform.rotation = heldItem.parentObject.rotation;
                heldItem.transform.Rotate(heldItem.itemProperties.rotationOffset);
                heldItem.transform.position = parentObject.position;
                Vector3 vector = heldItem.itemProperties.positionOffset;
                vector = heldItem.parentObject.rotation * vector;
                heldItem.transform.position += vector;

            }

            */
        }
        #endregion


        #region Helpers, Materials, Ragdolls, Rendering, etc...


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
