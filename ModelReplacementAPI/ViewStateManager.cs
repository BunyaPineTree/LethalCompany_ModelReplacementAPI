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
//
//This component exists to manageand patch the layers and cullingMasks for players and cameras.
//This is in an attempt to support all third person mods simultaneously without numerous individual patches, which historically are not a robust solution.
//
//
namespace ModelReplacement
{
    public enum ViewState
    {
        None,
        ThirdPerson,
        FirstPerson, 
        Debug
    }
    public class ViewStateManager : MonoBehaviour
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


        //First Person Layer => arms visible, mode


        //==================================================================== Rendering Logic ====================================================================
        //ThirdPerson        0100001001110110001011111111111 model, no arm = > 557520895
        //Mirror             0100001101110110001011101011111 model, no arm
        //ship camera        0000000000110000000001101001001 model, no arm
        //First Person       1100001001110110001011111110111 arm, no model = > 1631262711

        //Base CustomPass    1111111111111111111111111111111 => 2147483647   //FIND FIRST PERSON AND THIRD PERSON CULLING MASKS THAT SUPPORT A NOPOST LAYER
        //Adj CustomPass     1110111111011111111111111111111 => 1744830463
        //                      |      |
        //                      |  Adjusted with NoPost
        //ThirdPerson        0101001101110110001011111111111 model, no arm => 700127231
        //First Person       1101001001010110001011111111111 arm, no model => 1764431871
        //Mirror             0100001101110110001011101011111 model, no arm
        //ship camera        0000000000110000000001101001001 model, no arm 
        //Model  23                 x
        //Arms  30           x                                                  
        //Visible 0                                        x
        //FIX
        //NoPostModel 20               x 
        //NoPostArms 
        //NoPostVisible 27      x

        //Invisible 31      x

        //Using adjusted values here, other cameras will have their masks converted via patch.
        private static int CullingMaskThirdPerson = 700127231; //Base game MainCamera culling mask                                 
        public static int CullingMaskFirstPerson = 1764431871; //Modified base game to provide layers for arms and body 
        private static int CullingNoPostExcluded = 2012217343; //CustomPassVolume adjusted mask to remove postProcessing on designated layers.  
                                                               // public static int AllMask = (1 << visibleLayer) + (1 << NoPostVisibleLayer) + 1;


        public static int modelLayer = 23; //Arbitrarily decided
        public static int armsLayer = 30; //Most cullingMasks shouldn't have a 30 slot, so I will use that one to place arms. 
        public static int visibleLayer = 0; // Likely all culling masks show layer 0

        //These layers behave identically to their corresponding layers, with the additional trait of being excluded from the CustomPassVolume postProcessing
        public static int NoPostModelLayer = 20;
        //private static int NoPostArmsLayer;
        public static int NoPostVisibleLayer = 27;

        private static int invisibleLayer = 31; //No culling mask shows layer 31

        private bool bodyReplacementExists = false; 
        private BodyReplacementBase bodyReplacement;
        private PlayerControllerB controller;
        private GameObject replacementModel;
        private GameObject replacementViewModel;

        private MeshRenderer nameTagObj = null;
        private MeshRenderer nameTagObj2 = null;
        private bool UseNoPostProcessing => bodyReplacementExists? bodyReplacement.UseNoPostProcessing : false;
        private bool DebugRenderPlayer => bodyReplacementExists ?  bodyReplacement.DebugRenderPlayer: false;
        private bool DebugRenderModel => bodyReplacementExists ?  bodyReplacement.DebugRenderModel: false;

        public bool localPlayer => GameNetworkManager.Instance.localPlayerController == controller;
        public void Awake()
        {
            controller = base.GetComponent<PlayerControllerB>();
            MeshRenderer[] gameObjects = controller.gameObject.GetComponentsInChildren<MeshRenderer>();
            nameTagObj = gameObjects.Where(x => x.gameObject.name == "LevelSticker").First();
            nameTagObj2 = gameObjects.Where(x => x.gameObject.name == "BetaBadge").First();
            
        }
        public void Start()
        {
            RendererPatches();
        }

        public void Update()
        {
            if (bodyReplacementExists && bodyReplacement == null) { ReportBodyReplacementRemoval(); }
            if (bodyReplacementExists)
            {
                UpdateModelReplacement();
            }
            else
            {
                UpdatePlayer();
            }


        }

        public void ReportBodyReplacementAddition(BodyReplacementBase replacement)
        {
            bodyReplacement = replacement;
            replacementModel = replacement.replacementModel;
            replacementViewModel = replacement.replacementViewModel;
            bodyReplacementExists = true;
            PatchViewState();
        }
        public void ReportBodyReplacementRemoval()
        {
            bodyReplacement = null;
            replacementModel = null;
            replacementViewModel = null;
            bodyReplacementExists = false;
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
            if ((cam1.cullingMask & (1 << modelLayer)) == 0) //If the bitwise and is 0, then cullingMask culls the modelLayer, and needs to have modelLayer added.
            {
                cam1.cullingMask += (1 << modelLayer);
            }

            Camera cam2 = GameObject.Find("Environment/HangarShip/Cameras/ShipCamera").GetComponent<Camera>();
            if ((cam2.cullingMask & (1 << modelLayer)) == 0) //If the bitwise and is 0, then cullingMask culls the modelLayer, and needs to have modelLayer added.
            {
                cam2.cullingMask += (1 << modelLayer);
            }

            Console.WriteLine(GameObject.Find("Environment/HangarShip/Cameras/ShipCamera").GetComponent<Camera>().cullingMask);
        }
        public int ModelLayer => UseNoPostProcessing ? NoPostModelLayer : modelLayer;
        public int ArmsLayer => armsLayer;
        public int VisibleLayer => UseNoPostProcessing ? NoPostVisibleLayer : visibleLayer;
        public int InvisibleLayer => invisibleLayer;
        public void UpdatePlayer()
        {
            ViewState state = GetViewState();
            SetPlayerRenderers(true);
            controller.gameplayCamera.cullingMask = CullingMaskFirstPerson;
            if (state == ViewState.None)
            {
                SetArmLayers(InvisibleLayer);
                SetPlayerLayers(invisibleLayer);
            }
            else if (state == ViewState.FirstPerson)
            {
                SetArmLayers(ArmsLayer);
                SetPlayerLayers(modelLayer);
            }
            else if (state == ViewState.ThirdPerson)
            {
                SetArmLayers(InvisibleLayer);
                SetPlayerLayers(visibleLayer);
                if (ModelReplacementAPI.LCthirdPersonPresent)
                {
                    controller.gameplayCamera.cullingMask = CullingMaskThirdPerson;
                }
            }
        }
        public void UpdateModelReplacement()
        {
            ViewState state = GetViewState();
            SetPlayerRenderers(false);
            SetPlayerLayers(modelLayer);
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
            }
            else if (state == ViewState.ThirdPerson)
            {
                SetArmLayers(InvisibleLayer);
                SetAvatarLayers(VisibleLayer, ShadowCastingMode.On);
                if (ModelReplacementAPI.LCthirdPersonPresent)
                {
                    controller.gameplayCamera.cullingMask = CullingMaskThirdPerson;
                }
            }
            else if (state == ViewState.Debug)
            {
                if (DebugRenderModel)
                {
                    SetAvatarLayers(VisibleLayer, ShadowCastingMode.On);
                }
                else
                {
                    SetAvatarLayers(InvisibleLayer, ShadowCastingMode.Off);
                }
                if (DebugRenderPlayer)
                {
                    SetPlayerRenderers(true);
                    SetPlayerLayers(visibleLayer);
                }
                else
                {
                    SetPlayerRenderers(false);
                    SetPlayerLayers(invisibleLayer);
                }
                if (ModelReplacementAPI.LCthirdPersonPresent)
                {
                    controller.gameplayCamera.cullingMask = CullingMaskThirdPerson;
                }

            }

        }




        public ViewState GetViewState()
        {
            if (DebugRenderModel || DebugRenderPlayer)
            {
                return ViewState.Debug;
            }
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
        public void SetPlayerRenderers(bool enabled)
        {
            if (localPlayer)
            {
                controller.thisPlayerModel.enabled = enabled;
                controller.thisPlayerModelLOD1.enabled = false;
                controller.thisPlayerModelLOD2.enabled = false;
            }
            else
            {
                controller.thisPlayerModel.enabled = enabled;
                controller.thisPlayerModelLOD1.enabled = enabled;
                controller.thisPlayerModelLOD2.enabled = enabled;
            }
            

            controller.thisPlayerModel.shadowCastingMode = enabled ? ShadowCastingMode.On:  ShadowCastingMode.Off;
            controller.thisPlayerModelLOD1.shadowCastingMode = enabled ? ShadowCastingMode.On : ShadowCastingMode.Off;
            controller.thisPlayerModelLOD2.shadowCastingMode = enabled ? ShadowCastingMode.On : ShadowCastingMode.Off;

            nameTagObj.enabled = enabled;
            nameTagObj2.enabled = enabled;
        }
        public void SetPlayerLayers(int layer)
        {
            controller.thisPlayerModel.gameObject.layer = layer;
            controller.thisPlayerModelLOD1.gameObject.layer = layer;
            controller.thisPlayerModelLOD2.gameObject.layer = layer;
            nameTagObj.gameObject.layer = layer;
            nameTagObj2.gameObject.layer = layer;
        }
        public void SetAvatarLayers(int layer, ShadowCastingMode mode)
        {
            if(replacementModel == null) { return; }
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
                }
            }
            else
            {
                controller.thisPlayerModelArms.gameObject.layer = layer;
            }
        }
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
        private bool DangerousTMEViewState() => ThirdPersonEmoteController.emoteCamera.enabled;
    }
}
