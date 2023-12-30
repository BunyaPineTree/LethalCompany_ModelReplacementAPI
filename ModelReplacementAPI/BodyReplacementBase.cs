using _3rdPerson.Helper;
using GameNetcodeStuff;
using LCThirdPerson;
using ModelReplacement.AvatarBodyUpdater;
using MoreCompany.Cosmetics;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;


namespace ModelReplacement
{
    public enum ViewState
    {
        None,
        ThirdPerson,
        FirstPerson,
        Debug
    }

    public abstract class BodyReplacementBase : MonoBehaviour
    {

        //==================================================================== Rendering Logic ====================================================================
        //MainCamera          00100001001110110001011111111111 model, no arm = > 557520895
        //Mirror              00100001101110110001011101011111 model, no arm
        //ship camera         00000000000110000000001101001001 model, no arm
        //First Person        01100001001110110001011111110111 arm, no model = > 1631262711

        //Body  3                                         x
        //Arms  30             x                                                  
        //Visible 0                                          x
        //Invisible 31        x

        public static int CullingMaskThirdPerson = 557520895; //Base game MainCamera culling mask                                 
        public static int CullingMaskFirstPerson = 1631262711; //Modified base game to provide layers for arms and body 

        public static int modelLayer = 3; //Arbitrarily decided by MirrorDecor
        public static int armsLayer = 30; //Most cullingMasks shouldn't have a 30 slot, so I will use that one to place arms. 

        public static int visibleLayer = 0; // Likely all culling masks show layer 0
        public static int invisibleLayer = 31; //Most culling masks probably do not show layer 31
        //=========================================================================================================================================================

        //Base components
        public AvatarUpdater avatar { get; private set; }
        public PlayerControllerB controller { get; private set; }
        protected GameObject replacementModel;
        public string suitName { get; set; } = "";

        //Ragdoll components
        public AvatarUpdater ragdollAvatar { get; private set; }
        protected GameObject deadBody = null;
        protected GameObject replacementDeadBody = null;

        //Misc components
        private MeshRenderer nameTagObj = null;
        private MeshRenderer nameTagObj2 = null;
        private int danceNumber = 0;
        private int previousDanceNumber = 0;
        protected bool DontConvertUnsupportedShaders = false;

        //Mod Support components
        private AvatarUpdater cosmeticAvatar = null;

        #region Virtual and Abstract Methods

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
        /// <summary>
        /// Override this to return a derivative AvatarUpdater. Only do this if you really know what you are doing. 
        /// </summary>
        /// <returns></returns>
        protected virtual AvatarUpdater GetAvatarUpdater()
        {
            return new AvatarUpdater();
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

        #region Base Logic




        protected virtual void Awake()
        {

            controller = base.GetComponent<PlayerControllerB>();
            ModelReplacementAPI.Instance.Logger.LogInfo($"Awake {controller.playerUsername}");

            // Load model
            replacementModel = LoadAssetsAndReturnModel();

            if (replacementModel == null)
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

            foreach (var item in replacementModel.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                item.updateWhenOffscreen = true;
            }
            SetAllLayers(true);

            try
            {
                AddModelScripts();  // Set scripts missing from assetBundle
            }
            catch (Exception e)
            {
                ModelReplacementAPI.Instance.Logger.LogError($"Could not set all model scripts.\n Error: {e.Message}");
            }


            // Instantiate model
            replacementModel = UnityEngine.Object.Instantiate<GameObject>(replacementModel);
            replacementModel.name += $"({controller.playerUsername})";
            replacementModel.transform.localPosition = new Vector3(0, 0, 0);
            replacementModel.SetActive(true);


            // Sets y extents to the same size for player body and extents.
            var playerBodyExtents = controller.thisPlayerModel.bounds.extents;
            float scale = playerBodyExtents.y / GetBounds().extents.y;
            replacementModel.transform.localScale *= scale;


            // Assign the avatar
            avatar = GetAvatarUpdater();
            cosmeticAvatar = avatar;
            ragdollAvatar = new AvatarUpdater();
            avatar.AssignModelReplacement(controller.gameObject, replacementModel);


            // Misc fixes
            var gameObjects = controller.gameObject.GetComponentsInChildren<MeshRenderer>();
            nameTagObj = gameObjects.Where(x => x.gameObject.name == "LevelSticker").First();
            nameTagObj2 = gameObjects.Where(x => x.gameObject.name == "BetaBadge").First();
            ModelReplacementAPI.Instance.Logger.LogInfo($"AwakeEnd {controller.playerUsername}");
            rendererPatches();


        }

        protected virtual void Start()
        {

        }
        protected virtual void Update()
        {
            // Renderer logic
            SetAvatarRenderers(true);
            SetPlayerRenderers(false);
            SetAllLayers(true);

            // Handle Ragdoll creation and destruction
            GameObject deadBody = null;
            try
            {
                deadBody = controller.deadBody.gameObject;
            }
            catch { }
            if ((deadBody) && (replacementDeadBody == null)) //Player died this frame
            {
                Console.WriteLine("Set cosmeticAvatar to ragdoll");
                cosmeticAvatar = ragdollAvatar;
                CreateAndParentRagdoll(controller.deadBody);
                OnDeath();
            }
            if ((replacementDeadBody) && (deadBody == null)) //Player returned to life this frame
            {
                Console.WriteLine("Set cosmeticAvatar to living");
                cosmeticAvatar = avatar;
                Destroy(replacementDeadBody);
                replacementDeadBody = null;
            }

            // Update replacement models
            avatar.Update();
            ragdollAvatar.Update();
            if (ModelReplacementAPI.moreCompanyPresent) { SafeRenderCosmetics(true); }


            //Emotes
            previousDanceNumber = danceNumber;
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
                Console.WriteLine($"Dance change from {previousDanceNumber} to {danceNumber}");
                if (previousDanceNumber == 0) { StartCoroutine(WaitForDanceNumberChange()); } //Start new animation, takes time to switch to new animation state
                else if (danceNumber == 0) { OnEmoteEnd(); } // No dance, where there was previously dance.
                else { if (!emoteOngoing) { OnEmoteStart(danceNumber); } } //An animation did not start nor end, go immediately into the different animation
            }
        }



        protected virtual void OnDestroy()
        {
            ModelReplacementAPI.Instance.Logger.LogInfo($"Destroy body component for {controller.playerUsername}");
            controller.thisPlayerModel.enabled = true;
            controller.thisPlayerModelLOD1.enabled = true;
            controller.thisPlayerModelLOD2.enabled = true;

            nameTagObj.enabled = true;
            nameTagObj2.enabled = true;

            if (ModelReplacementAPI.moreCompanyPresent) { SafeRenderCosmetics(false); }
            Destroy(replacementModel);
            Destroy(replacementDeadBody);
        }

        #endregion

        #region Helpers, Materials, Ragdolls, Rendering, etc...
        private void SetAllLayers(bool useAdjustedMask)
        {
            ViewState state = GetViewState();
            Renderer[] renderers = replacementModel.GetComponentsInChildren<Renderer>();
            controller.gameplayCamera.cullingMask = CullingMaskFirstPerson;
            if (state == ViewState.None)
            {
                controller.thisPlayerModelArms.gameObject.layer = invisibleLayer;
                foreach (Renderer renderer in renderers)
                {
                    renderer.shadowCastingMode = ShadowCastingMode.Off;
                    renderer.gameObject.layer = invisibleLayer;
                }
            }
            else if (state == ViewState.FirstPerson)
            {
                controller.thisPlayerModelArms.gameObject.layer = armsLayer;
                foreach (Renderer renderer in renderers)
                {
                    renderer.shadowCastingMode = ShadowCastingMode.On;
                    renderer.gameObject.layer = modelLayer;
                }
            }
            else if (state == ViewState.ThirdPerson)
            {
                foreach (Renderer renderer in renderers)
                {
                    renderer.shadowCastingMode = ShadowCastingMode.On;
                    renderer.gameObject.layer = visibleLayer;
                }
                if(ModelReplacementAPI.LCthirdPersonPresent)
                {
                    controller.gameplayCamera.cullingMask = CullingMaskThirdPerson;
                }
            }
        }

        /// <summary> Shaders with any of these prefixes won't be automatically converted. </summary>
        private static readonly string[] shaderPrefixWhitelist =
        {
            "HDRP/",
            "GUI/",
            "Sprites/",
            "UI/",
            "Unlit/",
            "Toon",
            "lilToon",
            "Shader Graphs/",
            "Hidden/"
        };

        /// <summary>
        /// Get a replacement material based on the original game material, and the material found on the replacing model.
        /// </summary>
        /// <param name="gameMaterial">The equivalent material on the model being replaced.</param>
        /// <param name="modelMaterial">The material on the replacing model.</param>
        /// <returns>The replacement material created from the <see cref="gameMaterial"/> and the <see cref="modelMaterial"/></returns>
        protected virtual Material GetReplacementMaterial(Material gameMaterial, Material modelMaterial)
        {

            if (DontConvertUnsupportedShaders || shaderPrefixWhitelist.Any(prefix => modelMaterial.shader.name.StartsWith(prefix)))
            {
                return modelMaterial;
            }
            else
            {
                ModelReplacementAPI.Instance.Logger.LogInfo($"Creating replacement material for material {modelMaterial.name} / shader {modelMaterial.shader.name}");
                // XXX Ideally this material would be manually destroyed when the replacement model is destroyed.

                Material replacementMat = new Material(gameMaterial);
                replacementMat.color = modelMaterial.color;
                replacementMat.mainTexture = modelMaterial.mainTexture;
                replacementMat.mainTextureOffset = modelMaterial.mainTextureOffset;
                replacementMat.mainTextureScale = modelMaterial.mainTextureScale;

                /*
                if (modelMaterial.HasTexture("_BaseColorMap"))
                {
                    replacementMat.SetTexture("_BaseColorMap", modelMaterial.GetTexture("_BaseColorMap"));
                }
                if (modelMaterial.HasTexture("_SpecularColorMap"))
                {
                    replacementMat.SetTexture("_SpecularColorMap", modelMaterial.GetTexture("_SpecularColorMap"));
                    replacementMat.EnableKeyword("_SPECGLOSSMAP");
                }
                if (modelMaterial.HasFloat("_Smoothness"))
                {
                    replacementMat.SetFloat("_Smoothness", modelMaterial.GetFloat("_Smoothness"));
                }
                if (modelMaterial.HasTexture("_EmissiveColorMap"))
                {
                    replacementMat.SetTexture("_EmissiveColorMap", modelMaterial.GetTexture("_EmissiveColorMap"));
                }
                if (modelMaterial.HasTexture("_BumpMap"))
                {
                    replacementMat.SetTexture("_BumpMap", modelMaterial.GetTexture("_BumpMap"));
                    replacementMat.EnableKeyword("_NORMALMAP");
                }
                if (modelMaterial.HasColor("_EmissiveColor"))
                {
                    replacementMat.SetColor("_EmissiveColor", modelMaterial.GetColor("_EmissiveColor"));
                    replacementMat.EnableKeyword("_EMISSION");
                }
                */
                replacementMat.EnableKeyword("_EMISSION");
                replacementMat.EnableKeyword("_NORMALMAP");
                replacementMat.EnableKeyword("_SPECGLOSSMAP");
                replacementMat.SetFloat("_NormalScale", 0);

                HDMaterial.ValidateMaterial(replacementMat);

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
                renderer.shadowCastingMode = ShadowCastingMode.On;
                renderer.gameObject.layer = visibleLayer;
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

        private void SetAvatarRenderers(bool enabled)
        {
            foreach (Renderer renderer in replacementModel.GetComponentsInChildren<Renderer>())
            {
                renderer.enabled = enabled;
            }
        }

        private void SetPlayerRenderers(bool enabled)
        {
            controller.thisPlayerModel.enabled = enabled;
            controller.thisPlayerModelLOD1.enabled = enabled;
            controller.thisPlayerModelLOD2.enabled = enabled;
            nameTagObj.enabled = enabled;
            nameTagObj2.enabled = enabled;
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




        #region Third Person Mods Logic
        public ViewState GetViewState()
        {
            if (controller.isPlayerDead) //Dead, render nothing
            {
                return ViewState.None;
            }
            if (GameNetworkManager.Instance.localPlayerController != controller) //Other player, render third person
            {
                return ViewState.ThirdPerson;
            }
            if(ModelReplacementAPI.thirdPersonPresent && Safe3rdPersonActive()) //If any of these are true, we are in third person mode and must render third person
            {
                return ViewState.ThirdPerson;
            }
            if (ModelReplacementAPI.LCthirdPersonPresent && SafeLCActive())
            {
                return ViewState.ThirdPerson;
            }
            if (ModelReplacementAPI.recordingCameraPresent && SafeRecCamActive())
            {
                return ViewState.ThirdPerson;
            }
            return ViewState.FirstPerson; //Because none of the above triggered, we are in first person
            
        }
        private void rendererPatches()
        {
            if (ModelReplacementAPI.recordingCameraPresent)
            {
                SafeFixRecordingCamera();
            }

            if (ModelReplacementAPI.thirdPersonPresent)
            {
                SafeFix3rdPerson();
            }
            if (ModelReplacementAPI.LCthirdPersonPresent)
            {
            }
        }

        public bool Safe3rdPersonActive()
        {
            return DangerousViewState3rdPerson();
        }
        private bool DangerousViewState3rdPerson() { return ThirdPersonCamera.ViewState; }
        public bool SafeLCActive()
        {
            return DangerousLCViewState();
        }
        private bool DangerousLCViewState() { return ThirdPersonPlugin.Instance.Enabled; }
        public bool SafeRecCamActive()
        {
            var a = GameObject.FindObjectsOfType<Camera>().Where(x => x.gameObject.name == "ThridPersonCam");
            if(!a.Any()) { return false; }
            return a.First().enabled;
        }
        private void SafeFixRecordingCamera()
        {
            StartCoroutine(DangerousFixRecordingCamera());
        }
        private IEnumerator DangerousFixRecordingCamera()
        {
            int frame = 0;
            while (frame < 20)
            {
                yield return new WaitForEndOfFrame();
                frame++;
            }
            var a = GameObject.FindObjectsOfType<Camera>().Where(x => x.gameObject.name == "ThridPersonCam");
            a.First().cullingMask = CullingMaskThirdPerson;
        }
        private void SafeFix3rdPerson()
        {
            DangerousFix3rdPerson();
        }
        private void DangerousFix3rdPerson() { ThirdPersonCamera.GetCamera.cullingMask = CullingMaskThirdPerson; }

        #endregion

        #region MoreCompany Cosmetics Logic

        private void SafeRenderCosmetics(bool useAvatarTransforms)
        {
            DangerousRenderCosmetics(useAvatarTransforms);
        }

        private void DangerousRenderCosmetics(bool useAvatarTransforms)
        {
            var applications = controller.gameObject.GetComponentsInChildren<CosmeticApplication>();
            if ((applications.Any()))
            {
                foreach (var application in applications)
                {
                    if (useAvatarTransforms)
                    {
                        foreach (CosmeticInstance cosmeticInstance in application.spawnedCosmetics)
                        {
                            Transform transform = null;
                            switch (cosmeticInstance.cosmeticType)
                            {
                                case CosmeticType.HAT:
                                    transform = cosmeticAvatar.GetAvatarTransformFromBoneName("spine.004");
                                    break;
                                case CosmeticType.CHEST:
                                    transform = cosmeticAvatar.GetAvatarTransformFromBoneName("spine.003");
                                    break;
                                case CosmeticType.R_LOWER_ARM:
                                    transform = cosmeticAvatar.GetAvatarTransformFromBoneName("arm.R_lower");
                                    break;
                                case CosmeticType.HIP:
                                    transform = cosmeticAvatar.GetAvatarTransformFromBoneName("spine");
                                    break;
                                case CosmeticType.L_SHIN:
                                    transform = cosmeticAvatar.GetAvatarTransformFromBoneName("shin.L");
                                    break;
                                case CosmeticType.R_SHIN:
                                    transform = cosmeticAvatar.GetAvatarTransformFromBoneName("shin.R");
                                    break;
                            }
                            cosmeticInstance.transform.parent = null;
                            cosmeticInstance.transform.SetPositionAndRotation(transform.position, transform.rotation);

                        }
                    }
                    else
                    {
                        application.RefreshAllCosmeticPositions();
                    }

                }
            }



        }

        #endregion

        #region TooManyEmotes

        private int SafeGetEmoteID(int currentID)
        {
            return DangerousGetEmoteID(currentID);
        }

        private int DangerousGetEmoteID(int currentID)
        {
            var anim = TooManyEmotes.Patches.PlayerPatcher.GetCurrentlyPlayingEmote(controller);
            if (anim == null) { return currentID; }
            if (anim.emoteId == 1) { return -1; }
            if (anim.emoteId == 2) { return -2; }
            if (anim.emoteId == 3) { return -3; }
            return anim.emoteId;

        }

        #endregion



    }
}
