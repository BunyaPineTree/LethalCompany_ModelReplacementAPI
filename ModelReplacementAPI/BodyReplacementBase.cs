using _3rdPerson.Helper;
using GameNetcodeStuff;
using LCThirdPerson;
using ModelReplacement.Modules;
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
    public abstract class BodyReplacementBase : MonoBehaviour
    {
        
        //Base components
        public AvatarUpdater avatar { get; private set; }
        public ViewStateManager viewState = null;
        public MoreCompanyCosmeticManager cosmeticManager = null;
        public MaterialHelper materialHelper = null;
        public PlayerControllerB controller { get; private set; }
        public GameObject replacementModel;

        //Ragdoll components
        public AvatarUpdater ragdollAvatar { get; private set; }
        protected GameObject deadBody = null;
        protected GameObject replacementDeadBody = null;

        //Misc components
        private MeshRenderer nameTagObj = null;
        private MeshRenderer nameTagObj2 = null;
        private int danceNumber = 0;
        private int previousDanceNumber = 0;
        public string suitName { get; set; } = "";

        //Mod Support components
        public AvatarUpdater cosmeticAvatar = null;

        //Settings
        public bool UseNoPostProcessing = false;
        public bool DontConvertUnsupportedShaders = false;


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

            // Fix Materials and renderers
            materialHelper = new MaterialHelper(this);
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
                    if (!matMap.TryGetValue(mat, out Material replacementMat))
                    {
                        matMap[mat] = replacementMat = materialHelper.GetReplacementMaterial(gameMat, mat);
                    }
                    materials[i] = replacementMat;
                }
                renderer.SetMaterials(materials);
            }
            ListPool<Material>.Release(materials);
            foreach (SkinnedMeshRenderer item in replacementModel.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                item.updateWhenOffscreen = true;
            }
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
            replacementModel.transform.localPosition = new Vector3(0, 0, 0);
            replacementModel.SetActive(true);


            // Sets y extents to the same size for player body and extents.
            Vector3 playerBodyExtents = controller.thisPlayerModel.bounds.extents;
            float scale = playerBodyExtents.y / GetBounds().extents.y;
            replacementModel.transform.localScale *= scale;


            // Assign the avatar
            avatar = GetAvatarUpdater();
            cosmeticAvatar = avatar;
            ragdollAvatar = new AvatarUpdater();
            avatar.AssignModelReplacement(controller.gameObject, replacementModel);
            viewState = new ViewStateManager(this);
            cosmeticManager = new MoreCompanyCosmeticManager(this);

            // Misc fixes
            MeshRenderer[] gameObjects = controller.gameObject.GetComponentsInChildren<MeshRenderer>();
            nameTagObj = gameObjects.Where(x => x.gameObject.name == "LevelSticker").First();
            nameTagObj2 = gameObjects.Where(x => x.gameObject.name == "BetaBadge").First();
            
            viewState.RendererPatches();
            SetAvatarRenderers(true);
            viewState.SetAllLayers();

            ModelReplacementAPI.Instance.Logger.LogInfo($"AwakeEnd {controller.playerUsername}");
        }

        protected virtual void Start() { viewState.RendererPatches(); }
        protected virtual void LateUpdate()
        {
            // Renderer logic
            SetPlayerRenderers(false);
            viewState.SetAllLayers();

            // Handle Ragdoll creation and destruction
            GameObject deadBody = null;
            try
            {
                deadBody = controller.deadBody.gameObject;
            }
            catch { }
            if (deadBody && (replacementDeadBody == null)) //Player died this frame
            {
                Console.WriteLine("Set cosmeticAvatar to ragdoll");
                cosmeticAvatar = ragdollAvatar;
                CreateAndParentRagdoll(controller.deadBody);
                OnDeath();
            }
            if (replacementDeadBody && (deadBody == null)) //Player returned to life this frame
            {
                Console.WriteLine("Set cosmeticAvatar to living");
                cosmeticAvatar = avatar;
                Destroy(replacementDeadBody);
                replacementDeadBody = null;
            }

            // Update replacement models
            avatar.Update();
            ragdollAvatar.Update();
            cosmeticManager.Update(true);


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
                if (previousDanceNumber == 0) { StartCoroutine(WaitForDanceNumberChange()); } //Start new animation, takes time to switch to new animation state
                else if (danceNumber == 0) { OnEmoteEnd(); } // No dance, where there was previously dance.
                else { if (!emoteOngoing) { OnEmoteStart(danceNumber); } } //An animation did not start nor end, go immediately into the different animation
            }
        }



        protected virtual void OnDestroy()
        {
            ModelReplacementAPI.Instance.Logger.LogInfo($"Destroy body component for {controller.playerUsername}");
            SetPlayerRenderers(true);
            cosmeticManager.Update(false);
            Destroy(replacementModel);
            Destroy(replacementDeadBody);
        }

        #endregion

        #region Helpers, Materials, Ragdolls, Rendering, etc...
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
                renderer.gameObject.layer = viewState.VisibleLayer;
            }
            deadBodyRenderer.enabled = false;



            //blood decals not working
            foreach (GameObject item in bodyinfo.bodyBloodDecals)
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
            IEnumerable<Bounds> allBounds = replacementModel.GetComponentsInChildren<SkinnedMeshRenderer>().Select(r => r.bounds);

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



    }
}
