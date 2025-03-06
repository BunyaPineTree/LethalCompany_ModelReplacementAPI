using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering;
using UnityEngine;
using _3rdPerson.Helper;
using LCThirdPerson;
using System.Collections;
using GameNetcodeStuff;
using TooManyEmotes.Patches;
using ModelReplacement.Monobehaviors;
using Steamworks;
//
//This component exists to manageand patch the layers and cullingMasks for players and cameras.
//This is in an attempt to support all third person mods simultaneously without numerous individual patches, which historically are not a robust solution.
//
//
//        ModelReplacement, for backwards compat
namespace ModelReplacement
{
    public enum ViewState
    {
        None,
        ThirdPerson,
        FirstPerson,
        Debug
    }
    public class ViewStateManager : ManagerBase
    {
        //Required logic
        //Layers:
        //Invisible: Doesn't render -> 31
        //Model: Only the local player is on the model layer. 
        //Arms: Only the local player arms are on the arms layer
        //Visible: All cameras see this layer

        //NoPost variations
        //NoPostModel: Only the local player is on this layer, if on a nopost model. 
        //NoPostArms: Only the local player's arms are on the arms layer
        //NoPostVisible: All cameras see this layer

        //Culling Masks:
        //FirstPerson: local player's firstPerson mask, can see Arms, NoPostArms, Visible, NoPostVisible
        //ThirdPerson: all other masks, can see Model, NoPostModel, Visible, NoPostVisible

        //Required Camera Patches
        //Every camera that sees arms must be able to see NoPostArms
        //Every camera that sees visible must be able to see NoPostVisible
        //Every camera that sees model must be able to see NoPostModel


        //==================================================================== Rendering Logic ====================================================================
        //ThirdPerson        0100001001110110001011111111111 model, no arm = > 557520895
        //Mirror             0100001101110110001011101011111 model, no arm
        //ship camera        0000000000110000000001101001001 model, no arm
        //First Person old   1100001001110110001011111110111 arm, no model = > 1631262711
        //first person new   0100011001110110001011111111111

        //Base CustomPass    1111111111111111111111111111111 => 2147483647 
        //Adj CustomPass     1110111111111011111111111111111 => 2013134847
        //                      |         |
        //                      |  Adjusted with NoPost


        // Adjusted firstperson/thirdperson culling masks can be determined by taking the main camera cullingg mask and making adjustments based on the below requirements
        //Visible layers     0001000000000000000000000000001
        //Arm  layers        1000000000000000000000000000000
        //Model layers       0000000100000100000000000000000
        //ThirdPerson        0101011101110110001011111111111 model, no arm, visible => 733681663
        //First Person       1101011001110010001011111111111 arm, no model, visible => 1798903807
        //Mirror             0100001101110110001011101011111 model, no arm
        //ship camera        0000000000110000000001101001001 model, no arm 
        //Model  23                 x
        //NoPostModel 17                  x
        //Arms  30           x
        //NoPostArms (not used) 
        //Visible 0                                        x
        //NoPostVisible 27      x

        //Invisible 31      x

        //Culling masks calculated on awake
        private static int CullingMaskThirdPerson = 0;                               
        public static int CullingMaskFirstPerson = 0;
        private static int CullingNoPostExcluded = 0;

        // Standard in game layers
        public static int modelLayer = 23; 
        public static int armsLayer = 4; 
        public static int visibleLayer = 0;
        private static int invisibleLayer = 31; //No culling mask shows layer 31


        //These layers behave identically to their corresponding layers, with the additional trait of being excluded from the CustomPassVolume postProcessing
        public static int NoPostModelLayer = 17;
        public static int NoPostVisibleLayer = 27;
        //private static int NoPostArmsLayer;

        public static int ragdollLayer = 20; //This is set in game

        private MeshRenderer nameTagObj = null;
        private MeshRenderer nameTagObj2 = null;

        Renderer Hud = null;



        MeshRenderer helmet = null;
        bool HUDDefault = true;
        private bool UseNoPostProcessing => bodyReplacementExists ? bodyReplacement.UseNoPostProcessing : false;
        public int ModelLayer => UseNoPostProcessing ? NoPostModelLayer : modelLayer;
        public int ArmsLayer => armsLayer;
        public int VisibleLayer => UseNoPostProcessing ? NoPostVisibleLayer : visibleLayer;
        public int InvisibleLayer => invisibleLayer;

        protected override void Awake()
        {
            base.Awake();
            MeshRenderer[] gameObjects = controller.gameObject.GetComponentsInChildren<MeshRenderer>();
            nameTagObj = gameObjects.Where(x => x.gameObject.name == "LevelSticker").First();
            nameTagObj2 = gameObjects.Where(x => x.gameObject.name == "BetaBadge").First();

            helmet = GameObject.Find("Systems/Rendering/PlayerHUDHelmetModel").GetComponentInChildren<MeshRenderer>();

            Hud = controller.localVisor.transform.GetChild(0).GetComponent<Renderer>();
            HUDDefault = Hud.enabled;

            // Masks for each rendering mode
            int MaskModel = (1 << modelLayer) + (1 << NoPostModelLayer);
            int MaskArms = (1 << armsLayer);
            int MaskVisible = (1 << visibleLayer) + (1 << NoPostVisibleLayer);
            int MaskNoPost = (1 << NoPostModelLayer) + (1 << NoPostVisibleLayer);

            // Perform patches on the base game camera to form the first person/ third person cull masks
            CullingMaskFirstPerson = controller.gameplayCamera.cullingMask;
            CullingMaskFirstPerson = CullingMaskFirstPerson | MaskArms; // Turn on arms
            CullingMaskFirstPerson = CullingMaskFirstPerson & ~MaskModel; // Turn off model
            CullingMaskFirstPerson = CullingMaskFirstPerson | MaskVisible; // Turn on visible
            //
            CullingMaskThirdPerson = controller.gameplayCamera.cullingMask;
            CullingMaskThirdPerson = CullingMaskThirdPerson & ~MaskArms; // Turn off arms
            CullingMaskThirdPerson = CullingMaskThirdPerson | MaskModel; // Turn on model
            CullingMaskThirdPerson = CullingMaskThirdPerson | MaskVisible; // Turn on visible

            // Perform a patch on the post processing culling mask to exclude necessary layers
            CullingNoPostExcluded = 2147483647; // 30 1's in a row
            CullingNoPostExcluded = CullingNoPostExcluded & ~MaskNoPost; // Remove the nopost layers from the post processing culling mask

            // Disable collision between additional layers and ragdoll. 

            int[] layersToDeleteCollision = new int[] { ragdollLayer, 21, 3, 14, 22, 6 };

            foreach (int layer in layersToDeleteCollision)
            {
                //Physics.IgnoreLayerCollision(layer, modelLayer);
                //Physics.IgnoreLayerCollision(layer, NoPostModelLayer);
                //Physics.IgnoreLayerCollision(layer, visibleLayer);
                //Physics.IgnoreLayerCollision(layer, NoPostVisibleLayer);
                //Physics.IgnoreLayerCollision(layer, armsLayer);
                //Physics.IgnoreLayerCollision(layer, invisibleLayer);
            }

           

        }
        public void Start()
        {
            RendererPatches();
        }

        public override void ReportBodyReplacementAddition(BodyReplacementBase replacement)
        {
            base.ReportBodyReplacementAddition(replacement);
            PatchViewState();
        }
        public override void UpdatePlayer()
        {
            ViewState state = GetViewState();
            SetPlayerRenderers(true, true);
            controller.gameplayCamera.cullingMask = CullingMaskFirstPerson;
            if (state == ViewState.None)
            {
                SetArmLayers(InvisibleLayer);
                SetPlayerLayers(invisibleLayer);
                
                if (localPlayer)
                {
                    Hud.enabled = false;
                }
            }
            else if (state == ViewState.FirstPerson)
            {
                SetArmLayers(ArmsLayer);
                SetPlayerLayers(modelLayer);

                if (localPlayer)
                {
                    Hud.enabled = HUDDefault;
                }
            }
            else if (state == ViewState.ThirdPerson)
            {
                SetArmLayers(InvisibleLayer);
                SetPlayerLayers(visibleLayer);

                if (ModelReplacementAPI.LCthirdPersonPresent)
                {
                    controller.gameplayCamera.cullingMask = CullingMaskThirdPerson;
                }

                if (localPlayer)
                {
                    Hud.enabled = false;
                }
            }
        }
        public override void UpdateModelReplacement()
        {
            ViewState state = GetViewState();
            SetPlayerRenderers(false, false);
            SetPlayerLayers(modelLayer);
            SetShadowModel(false);
            controller.gameplayCamera.cullingMask = CullingMaskFirstPerson;
            if (state == ViewState.None)
            {
                SetArmLayers(InvisibleLayer);
                SetAvatarLayers(InvisibleLayer, ShadowCastingMode.Off);
            }
            else if (state == ViewState.FirstPerson)
            {
                SetArmLayers(ArmsLayer);
                SetAvatarLayers(ModelLayer, ShadowCastingMode.On);
                SetShadowModel(true);
                
                if (localPlayer)
                {
                    Hud.enabled =  (bodyReplacement.RemoveHelmet ? false : HUDDefault);
                }
            }
            else if (state == ViewState.ThirdPerson)
            {
                SetArmLayers(InvisibleLayer);
                SetAvatarLayers(VisibleLayer, ShadowCastingMode.On);
                
                if (ModelReplacementAPI.LCthirdPersonPresent)
                {
                    controller.gameplayCamera.cullingMask = CullingMaskThirdPerson;
                }

                if (localPlayer)
                {
                    Hud.enabled = (false);
                }
            }
        }




        public ViewState GetViewState()
        {
            if (!controller.isPlayerControlled) //Dead, render nothing
            {
                return ViewState.None;
            }
            if (GameNetworkManager.Instance.localPlayerController != controller) //Other player, render third person
            {
                return ViewState.ThirdPerson;
            }
            if (ModelReplacementAPI.thirdPersonPresent && Safe3rdPersonActive()) //If any of these are true, we are in third person mode and must render third person
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
            if (ModelReplacementAPI.tooManyEmotesPresent && SafeTMEActive())
            {
                return ViewState.ThirdPerson;
            }
            return ViewState.FirstPerson; //Because none of the above triggered, we are in first person

        }

        #region Set,Layer helpers
        public void SetPlayerRenderers(bool enabled, bool helmetShadow)
        {
            if (localPlayer)
            {
                helmet.shadowCastingMode = helmetShadow ? ShadowCastingMode.On : ShadowCastingMode.Off;
            }

            controller.thisPlayerModel.enabled = enabled;
            controller.thisPlayerModel.shadowCastingMode = ShadowCastingMode.On;
            controller.thisPlayerModelLOD1.enabled = enabled;
            controller.thisPlayerModelLOD1.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
            controller.thisPlayerModelLOD2.enabled = false;
                
            nameTagObj.enabled = enabled;
            nameTagObj2.enabled = enabled;
        }

        public void SetPlayerLayers(int layer)
        {
            controller.thisPlayerModel.gameObject.layer = layer;
            controller.thisPlayerModelLOD1.gameObject.layer = visibleLayer;
            controller.thisPlayerModelLOD2.gameObject.layer = visibleLayer;
            nameTagObj.gameObject.layer = layer;
            nameTagObj2.gameObject.layer = layer;
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
        public void SetArmLayers(int layer)
        {
            if (replacementViewModel)
            {
                controller.thisPlayerModelArms.gameObject.layer = InvisibleLayer;
                Renderer[] renderers = replacementViewModel.GetComponentsInChildren<Renderer>();
                foreach (Renderer renderer in renderers)
                {
                    renderer.gameObject.layer = layer;
                    renderer.shadowCastingMode = ShadowCastingMode.Off;
                }
            }
            else
            {
                controller.thisPlayerModelArms.gameObject.layer = layer;
            }
        }
        public void SetShadowModel(bool enabled)
        {
            if(bodyReplacement.replacementModelShadow == null) { return; }  
            bodyReplacement.replacementModelShadow.SetActive(enabled);
        }
        #endregion

        #region Compatability and Patches
        public void RendererPatches()
        {
            PatchViewState();



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

        public static void PatchViewState()
        {
            var cpass = GameObject.Find("Systems/Rendering/CustomPass").GetComponent<CustomPassVolume>().customPasses.First();
            (cpass as DrawRenderersCustomPass).layerMask = CullingNoPostExcluded;

            var a = FindObjectsOfType<Camera>();

            int maskModel = 1 << modelLayer;
            int maskVisible = 1 << visibleLayer;

            int maskNoPassModel = 1 << NoPostModelLayer;
            int maskNoPassVisible = 1 << NoPostVisibleLayer;
            int maskArms = 1 << 3;  //1 << armsLayer; // 3 is by base the first person effects layer

            foreach (Camera camera in a)
            {
                int cullingMask = camera.cullingMask;

                if ((cullingMask & maskModel) != 0) //If the bitwise and is 0, then cullingMask does not contain the modelLayer bit, and can be ignored
                {
                    if ((cullingMask & maskNoPassModel) == 0) //If the bitwise and is 0, then cullingMask culls the NoPassModel layer, and needs to have NoPass added.
                    {
                        camera.cullingMask += maskNoPassModel;
                    }
                }
                if ((cullingMask & maskVisible) != 0) //If the bitwise and is 0, then cullingMask does not contain the visibleLayer bit, and can be ignored
                {
                    if ((cullingMask & maskNoPassVisible) == 0) //If the bitwise and is 0, then cullingMask culls the NoPassVisible layer, and needs to have NoPass added.
                    {
                        camera.cullingMask += maskNoPassVisible;
                    }
                    /*
                    if((cullingMask & maskArms) == 0)//If the bitwise and is 0, then cullingMask culls the Arms layer, and should see the model layer
                    {
                        camera.cullingMask += maskModel;
                    }
                    */

                }
            }
            Camera cam1 = GameObject.Find("Environment/HangarShip/Cameras/FrontDoorSecurityCam/SecurityCamera").GetComponent<Camera>();
            if ((cam1.cullingMask & 1 << modelLayer) == 0) //If the bitwise and is 0, then cullingMask culls the modelLayer, and needs to have modelLayer added.
            {
                cam1.cullingMask += 1 << modelLayer;
            }

            Camera cam2 = GameObject.Find("Environment/HangarShip/Cameras/ShipCamera").GetComponent<Camera>();
            if ((cam2.cullingMask & 1 << modelLayer) == 0) //If the bitwise and is 0, then cullingMask culls the modelLayer, and needs to have modelLayer added.
            {
                cam2.cullingMask += 1 << modelLayer;
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
            IEnumerable<Camera> a = FindObjectsOfType<Camera>().Where(x => x.gameObject.name == "ThridPersonCam");
            if (!a.Any()) { return false; }
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
            IEnumerable<Camera> a = FindObjectsOfType<Camera>().Where(x => x.gameObject.name == "ThridPersonCam");
            a.First().cullingMask = CullingMaskThirdPerson;
        }
        private void SafeFix3rdPerson()
        {
            DangerousFix3rdPerson();
        }
        private void DangerousFix3rdPerson() { ThirdPersonCamera.GetCamera.cullingMask = CullingMaskThirdPerson; }

        private bool SafeTMEActive()
        {
            return DangerousTMEViewState();
        }
        private bool DangerousTMEViewState()
        {
            try
            {
                return false;//return ThirdPersonEmoteController.emoteCamera.enabled;
            }
            catch { return false; }
        }

        #endregion
    }
}
