using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering;
using UnityEngine;
using _3rdPerson.Helper;
using LCThirdPerson;
using System.Collections;
using UnityEngine.InputSystem.XR;
using GameNetcodeStuff;

namespace ModelReplacement.Modules
{
    public enum ViewState
    {
        None,
        ThirdPerson,
        FirstPerson,
        Debug
    }
    public class ViewStateManager
    {
        //==================================================================== NoPost Analysis ====================================================================
        //ALL CULLINGMASKS IN USE IN GAME
        //0000000000000000000000000100000
        //0000000100110000000001101001001
        //0100001001110010001011111111111
        //0000011001110010001011111111111
        //0000000000000000100000100000000
        //0000000000110000000001101001001
        //1101001001110110001011111110111 
        //0000000000000110001011111111111
        //ALL GAMEOBJECT LAYERS IN USE IN GAME
        //1101101111001001111101111101001

        //CANDIDATE NOPOST CULLING MAP AND LAYER
        //1100111111111111111111111111111 => 28 model => 27 visible

        //No camera in the game by base renders layers 28, 27, 
        //I will place arms in the 30 slot, as I already have confirmation that there is no conflict, and place NoPost in 28
        //No matter where I place NoPost, all cameras need to be adjusted. It is easier to patch them than do this manually. 
        //Patch all cameras that display layer 3(The model layer) to also display layer 28, and all cameras that display layer 0(The visible layer) to also also display layer 27
        //This improves maintainability, as we only need to patch other mod cameras with the Mirror convention of models placed on 3


        //==================================================================== Rendering Logic ====================================================================
        //ThirdPerson        0100001001110110001011111111111 model, no arm = > 557520895
        //Mirror             0100001101110110001011101011111 model, no arm
        //ship camera        0000000000110000000001101001001 model, no arm
        //First Person       1100001001110110001011111110111 arm, no model = > 1631262711

        //Base CustomPass    1111111111111111111111111111111 => 2147483647   //FIND FIRST PERSON AND THIRD PERSON CULLING MASKS THAT SUPPORT A NOPOST LAYER
        //Adj CustomPass     1100111111111111111111111111111 => 1744830463
        //                     ||
        //                     ||  Adjusted with NoPost
        //ThirdPerson        0111001001110110001011111111111 model, no arm => 960174079
        //Mirror             0111001101110110001011101011111 model, no arm
        //ship camera        0011000000110000000001101001001 model, no arm 
        //First Person       1101001001110110001011111110111 arm, no model => 1765480439

        //Model  3                                       x
        //Arms  30           x                                                  
        //Visible 0                                        x

        //NoPostModel 28       x
        //NoPostArms 
        //NoPostVisible 27      x

        //Invisible 31      x

        //Using adjusted values here, other cameras will have their masks converted via patch.
        private static int CullingMaskThirdPerson = 960174079; //Base game MainCamera culling mask                                 
        public static int CullingMaskFirstPerson = 1765480439; //Modified base game to provide layers for arms and body 
        private static int CullingNoPostExcluded = 1744830463; //CustomPassVolume adjusted mask to remove postProcessing on designated layers. 

        public static int modelLayer = 3; //Arbitrarily decided by MirrorDecor
        public static int armsLayer = 30; //Most cullingMasks shouldn't have a 30 slot, so I will use that one to place arms. 
        private static int visibleLayer = 0; // Likely all culling masks show layer 0

        //These layers behave identically to their corresponding layers, with the additional trait of being excluded from the CustomPassVolume postProcessing
        private static int NoPostModelLayer = 28;
        //private static int NoPostArmsLayer;
        private static int NoPostVisibleLayer = 27;

        private static int invisibleLayer = 31; //No culling mask shows layer 31

        private BodyReplacementBase bodyReplacement;
        private PlayerControllerB controller;
        private GameObject replacementModel;
        private bool UseNoPostProcessing => bodyReplacement.UseNoPostProcessing;

        public ViewStateManager(BodyReplacementBase bodyreplacement)
        {
            this.bodyReplacement = bodyreplacement;
            controller = bodyreplacement.controller;
            replacementModel = bodyreplacement.replacementModel;
        }

        public static void PatchViewState()
        {
            var cpass = GameObject.Find("Systems/Rendering/CustomPass").GetComponent<CustomPassVolume>().customPasses.First();
            (cpass as DrawRenderersCustomPass).layerMask = CullingNoPostExcluded;

            var a = GameObject.FindObjectsOfType<Camera>();

            int maskModel = (1 << modelLayer);
            int maskVisible = (1 << visibleLayer);

            int maskNoPassModel = (1 << NoPostModelLayer);
            int maskNoPassVisible = (1 << NoPostVisibleLayer);

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
                }
            }
        }
        public int ModelLayer => UseNoPostProcessing ? NoPostModelLayer : modelLayer;
        public int ArmsLayer => armsLayer;
        public int VisibleLayer => UseNoPostProcessing ? NoPostVisibleLayer : visibleLayer;
        public int InvisibleLayer => invisibleLayer;
        public void SetAllLayers()
        {
            ViewState state = GetViewState();
            Renderer[] renderers = replacementModel.GetComponentsInChildren<Renderer>();
            controller.gameplayCamera.cullingMask = CullingMaskFirstPerson;
            controller.gameplayCamera.clearFlags = CameraClearFlags.Nothing;
            if (state == ViewState.None)
            {
                controller.thisPlayerModelArms.gameObject.layer = InvisibleLayer;
                foreach (Renderer renderer in renderers)
                {
                    renderer.shadowCastingMode = ShadowCastingMode.Off;
                    renderer.gameObject.layer = InvisibleLayer;
                }
            }
            else if (state == ViewState.FirstPerson)
            {
                controller.thisPlayerModelArms.gameObject.layer = ArmsLayer;
                foreach (Renderer renderer in renderers)
                {
                    renderer.shadowCastingMode = ShadowCastingMode.On;
                    renderer.gameObject.layer = ModelLayer;
                }
            }
            else if (state == ViewState.ThirdPerson)
            {
                foreach (Renderer renderer in renderers)
                {
                    renderer.shadowCastingMode = ShadowCastingMode.On;
                    renderer.gameObject.layer = VisibleLayer;
                }
                if (ModelReplacementAPI.LCthirdPersonPresent)
                {
                    controller.gameplayCamera.cullingMask = CullingMaskThirdPerson;
                }

                
            }
            
        }

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
            return ViewState.FirstPerson; //Because none of the above triggered, we are in first person

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
            IEnumerable<Camera> a = GameObject.FindObjectsOfType<Camera>().Where(x => x.gameObject.name == "ThridPersonCam");
            if (!a.Any()) { return false; }
            return a.First().enabled;
        }
        private void SafeFixRecordingCamera()
        {
            bodyReplacement.StartCoroutine(DangerousFixRecordingCamera());
        }
        private IEnumerator DangerousFixRecordingCamera()
        {
            int frame = 0;
            while (frame < 20)
            {
                yield return new WaitForEndOfFrame();
                frame++;
            }
            IEnumerable<Camera> a = GameObject.FindObjectsOfType<Camera>().Where(x => x.gameObject.name == "ThridPersonCam");
            a.First().cullingMask = CullingMaskThirdPerson;
        }
        private void SafeFix3rdPerson()
        {
            DangerousFix3rdPerson();
        }
        private void DangerousFix3rdPerson() { ThirdPersonCamera.GetCamera.cullingMask = CullingMaskThirdPerson; }











        #region Debug
        public static string GetGameObjectPath(GameObject obj)
        {
            string path = "/" + obj.name;
            while (obj.transform.parent != null)
            {
                obj = obj.transform.parent.gameObject;
                path = "/" + obj.name + path;
            }
            return path;
        }
        public static void QueryCamerasAndRenderers()
        {
            var a = GameObject.FindObjectsOfType<Camera>();
            HashSet<int> cullingMasks = new HashSet<int>();
            a.ToList().ForEach(c => { cullingMasks.Add(c.cullingMask); });

            var b = GameObject.FindObjectsOfType<GameObject>();
            HashSet<int> layers = new HashSet<int>();
            b.ToList().ForEach(l => { layers.Add(l.layer); if (l.layer == 27) { Console.WriteLine(GetGameObjectPath(l)); } });

            int layerRep = 0;

            for (int i = 0; i <= 30; i++)
            {
                int c = (int)Math.Pow(2, i);
                if (layers.Contains(i))
                {
                    layerRep += c;
                    Console.WriteLine($"Adding {c} for i={i}");
                }

            }

            string representation = "ALL CULLING MASKS\n";
            cullingMasks.ToList().ForEach(m => representation += Convert.ToString(m, 2) + "\n");

            representation += "ALL LAYERS\n" + Convert.ToString(layerRep, 2);

            Console.WriteLine(representation);

            layers.ToList().ForEach(l => { Console.WriteLine(l); });
        }
        #endregion

    }
}
