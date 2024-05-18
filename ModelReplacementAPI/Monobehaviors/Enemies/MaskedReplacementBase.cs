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
using Steamworks;
using UnityEngine.InputSystem.XR;
using ModelReplacement.Scripts;
using ModelReplacement.Scripts.Player;
using ModelReplacement.Scripts.Enemies;
using _3rdPerson.Helper;

namespace ModelReplacement.Monobehaviors.Enemies
{
    public class MaskedReplacementBase : MonoBehaviour
    {

        //Base components
        public MaskedAvatarUpdater avatar { get; private set; }
        public MaskedPlayerEnemy enemyAI = null;
        public GameObject replacementModel;

        //Networking and Initialization
        public bool IsActive = false;



        #region Unity Logic
        protected virtual void Awake()
        {
            // basic
            enemyAI = GetComponent<MaskedPlayerEnemy>();
            ModelReplacementAPI.Instance.Logger.LogInfo($"Awake Masked mimicking {enemyAI.mimickingPlayer} {this}");

            if (!ModelReplacementAPI.MRAPI_NetworkingPresent)
            {
                var mimicking = enemyAI.mimickingPlayer;

                if( mimicking == null)
                {
                    Console.WriteLine("Masked not mimicking anyone, choose at random: ");

                    System.Random random = new System.Random();
                    mimicking = StartOfRound.Instance.allPlayerScripts[random.Next(StartOfRound.Instance.ClientPlayerList.Count)];
                }
                Console.WriteLine($"{mimicking.name}");
                if (mimicking != null)
                {
                    SetReplacement(mimicking); 
                }
            }
        }

        protected void SetReplacement(PlayerControllerB mimicking)
        {
            bool setReplacement = ModelReplacementAPI.GetPlayerModelReplacement(mimicking, out var modelReplacement);
            if (!setReplacement) { return; }
            IsActive = true;

            // Load Models 
            replacementModel = Instantiate(modelReplacement.replacementModel);
            replacementModel.name += $"(Masked)";
            replacementModel.transform.localPosition = new Vector3(0, 0, 0);
            replacementModel.SetActive(true);

            foreach (Renderer renderer in replacementModel.GetComponentsInChildren<Renderer>())
            {
                renderer.shadowCastingMode = ShadowCastingMode.On;
                renderer.gameObject.layer = modelReplacement.viewState.VisibleLayer;
                renderer.enabled = enabled;
            }

            // Assign avatars
            avatar = new MaskedAvatarUpdater();

            avatar.AssignModelReplacement(enemyAI.gameObject, replacementModel);

            // Disable Masked renderers
            enemyAI.rendererLOD0.enabled = false;
            enemyAI.rendererLOD1.enabled = false;
            enemyAI.rendererLOD2.enabled = false;
            enemyAI.rendererLOD0.shadowCastingMode = ShadowCastingMode.Off;
            enemyAI.rendererLOD1.shadowCastingMode = ShadowCastingMode.Off;
            enemyAI.rendererLOD2.shadowCastingMode = ShadowCastingMode.Off;

            // Remove Nametag
            MeshRenderer[] gameObjects = enemyAI.gameObject.GetComponentsInChildren<MeshRenderer>();
            gameObjects.Where(x => x.gameObject.name == "LevelSticker").First().enabled = false;
            gameObjects.Where(x => x.gameObject.name == "BetaBadge").First().enabled = false;



            /*
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
            */

        }

        protected virtual void Start() { }

        public virtual void LateUpdate()
        {
            if (!IsActive) return;

            // Update replacement models
            avatar.Update();
            //UpdateItemTransform();

            /*
            //Bounding box calculation for nameTag
            Bounds modelBounds = GetBounds(replacementModel);
            nameTagCollider.center = modelBounds.center;
            nameTagCollider.size = modelBounds.size;
            */

        }



        protected virtual void OnDestroy()
        {
            if (replacementModel != null) {
                ModelReplacementAPI.Instance.Logger.LogInfo($"Destroy masked body component {replacementModel.name}");
                Destroy(replacementModel);
            }
        }

        #endregion

        #region Helpers, Materials, Ragdolls, Rendering, etc...
        /*
        public bool CanPositionItemOnCustomViewModel => (replacementViewModel != null) && (viewModelAvatar.ItemHolderViewModel);
        public void UpdateItemTransform()
        {
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
            
            if ((viewState.GetViewState() == ViewState.FirstPerson) && CanPositionItemOnCustomViewModel)
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
            

        }
        */




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




        /*

        public class RaycastTarget:MonoBehaviour
        {
            public PlayerControllerB controller = null;
            public BodyReplacementBase bodyReplacement = null;
            public GameObject modelObj = null;

            private void Update()
            {
                if((bodyReplacement == null) && (modelObj != null))
                {
                    
                }
            }
        }
        */
    }
}
