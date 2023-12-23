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
using Unity.Netcode;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;


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

        protected internal virtual void OnHitAlly(PlayerControllerB ally,  bool dead)
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

            foreach (var item in replacementModel.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                item.updateWhenOffscreen = true;
            }       
            
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
            SetRenderers(false); //Initializing with renderers disabled prevents model flickering for local player
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

            //Mirror patch
            if (ModelReplacementAPI.mirrorDecorPresent)
            {
                MirrorPatch();
            }
        }

        protected virtual void Start()
        {

        }

        protected virtual void Update()
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
                else { if(!emoteOngoing) { OnEmoteStart(danceNumber); }} //An animation did not start nor end, go immediately into the different animation
            }
            //Console.WriteLine($"{danceNumber} {danceHash}");

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

                /*
            mesh.materials[0].shader = goodShader;

            mesh.materials[0].EnableKeyword("_EMISSION");
            mesh.materials[0].EnableKeyword("_SPECGLOSSMAP");
            mesh.materials[0].EnableKeyword("_NORMALMAP");

            mesh.materials[0].SetTexture("_BaseColorMap", TexBase01);
            mesh.materials[0].SetTexture("_SpecularColorMap", TexSpec);
            mesh.materials[0].SetFloat("_Smoothness", .30f);
            mesh.materials[0].SetTexture("_EmissiveColorMap", TexEmit);
            mesh.materials[0].SetTexture("_BumpMap", TexNorm);
            mesh.materials[0].SetColor("_EmissiveColor", Color.white);
            */

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

            bounds.SetMinMax(new Vector3(minX, minY, minZ), new Vector3(maxX, maxY, maxZ));
            return bounds;
        }

        #endregion

        #region Coroutines
        private bool emoteOngoing = false;
        private IEnumerator WaitForDanceNumberChange()
        {
            if(emoteOngoing) { yield break; }
            emoteOngoing = true;
            int frame = 0;
            while (frame < 20)
            {
                if (danceNumber == 0) { emoteOngoing = false; yield break; }
                yield return new WaitForEndOfFrame();
                frame++;
            }
            if(danceNumber != 0) { emoteOngoing = false; OnEmoteStart(danceNumber); }
        }


        #endregion




        #region MirrorDecor Logic
        private void MirrorPatch()
        {
            foreach (var item in replacementModel.GetComponentsInChildren<Renderer>())
            {
                item.shadowCastingMode = ShadowCastingMode.On;
                item.gameObject.layer = 3;
            }
            if (localPlayer) { return; }
            if (ModelReplacementAPI.thirdPersonPresent)
            {
                DangeroudFixCamera();
            }
            if (ModelReplacementAPI.LCthirdPersonPresent)
            {
            }
        }
        #endregion

        #region Third Person Mods Logic
        private void DangeroudFixCamera()
        {
            ThirdPersonCamera.GetCamera.cullingMask = 557520887;
        }
        private void DangeroudFixCameraLC()
        {
            //LCThirdPerson .camera.Instance.game
        }

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
            if(anim == null) { return currentID; }
            if(anim.emoteId == 1) { return -1; }
            if(anim.emoteId == 2) { return -2; }
            if(anim.emoteId == 3) { return -3; }
            return anim.emoteId;

        }

        #endregion



    }
}
