using ModelReplacement.Monobehaviors;
using UnityEngine;
using UnityEngine.Rendering;
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
        //Literally all of this stuff down here is for managing layers and culling masks to implement the No PostProcessing layer.
        //If Content Warning doesn't need this, then just about all of it can be removed. 



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
        //First Person       1100001001110110001011111110111 arm, no model = > 1631262711

        //Base CustomPass    1111111111111111111111111111111 => 2147483647 
        //Adj CustomPass     1110111111111011111111111111111 => 2013134847
        //                      |         |
        //                      |  Adjusted with NoPost
        //ThirdPerson        0101001101110110001011111111111 model, no arm => 700127231
        //First Person       1101001001110010001011111111111 arm, no model => 1765349375
        //Mirror             0100001101110110001011101011111 model, no arm
        //ship camera        0000000000110000000001101001001 model, no arm 
        //Model  23                 x
        //Arms  30           x                                                  
        //Visible 0                                        x
        //FIX
        //NoPostModel 17                  x
        //NoPostArms 
        //NoPostVisible 27      x

        //Invisible 31      x

        //Using adjusted values here, other cameras will have their masks converted via patch.
        private static int CullingMaskThirdPerson = 700127231; //Base game MainCamera culling mask                                 
        public static int CullingMaskFirstPerson = 1765349375; //Modified base game to provide layers for arms and body 
        private static int CullingNoPostExcluded = 2013134847; //CustomPassVolume adjusted mask to remove postProcessing on designated layers.  
                                                               // public static int AllMask = (1 << visibleLayer) + (1 << NoPostVisibleLayer) + 1;


        public static int modelLayer = 23; //Arbitrarily decided
        public static int armsLayer = 30; //Most cullingMasks shouldn't have a 30 slot, so I will use that one to place arms. 
        public static int visibleLayer = 0; // Likely all culling masks show layer 0

        //These layers behave identically to their corresponding layers, with the additional trait of being excluded from the CustomPassVolume postProcessing
        public static int NoPostModelLayer = 17;
        //private static int NoPostArmsLayer;
        public static int NoPostVisibleLayer = 27;

        private static int invisibleLayer = 31; //No culling mask shows layer 31

        private bool UseNoPostProcessing => bodyReplacementExists ? bodyReplacement.UseNoPostProcessing : false;
        private bool DebugRenderPlayer => bodyReplacementExists ? bodyReplacement.DebugRenderPlayer : false;
        private bool DebugRenderModel => bodyReplacementExists ? bodyReplacement.DebugRenderModel : false;
        public int ModelLayer => UseNoPostProcessing ? NoPostModelLayer : modelLayer;
        public int ArmsLayer => armsLayer;
        public int VisibleLayer => UseNoPostProcessing ? NoPostVisibleLayer : visibleLayer;
        public int InvisibleLayer => invisibleLayer;

        protected override void Awake()
        {
            base.Awake();
        }
        public void Start()
        {
            RendererPatches();
        }

        public override void ReportBodyReplacementAddition(BodyReplacementBase replacement)
        {
            base.ReportBodyReplacementAddition(replacement);
        }
        public override void UpdatePlayer()
        {
            ViewState state = GetViewState();
            SetPlayerRenderers(true);
            // Set culling masks as necessary. This is for No Post Processing layers, and may not be needed for Content Warning
            //controller.gameplayCamera.cullingMask = CullingMaskFirstPerson;
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
            }
        }
        public override void UpdateModelReplacement()
        {
            ViewState state = GetViewState();
            SetPlayerRenderers(false);
            SetPlayerLayers(modelLayer);
            // Set culling masks as necessary. This is for No Post Processing layers, and may not be needed for Content Warning
            //controller.gameplayCamera.cullingMask = CullingMaskFirstPerson;
            if (state == ViewState.None)
            {
                SetArmLayers(InvisibleLayer);
                SetAvatarLayers(InvisibleLayer, ShadowCastingMode.Off);
            }
            else if (state == ViewState.FirstPerson)
            {
                SetArmLayers(ArmsLayer);
                //SetAvatarLayers(ModelLayer, ShadowCastingMode.On);
                SetAvatarLayers(VisibleLayer, ShadowCastingMode.ShadowsOnly);
            }
            else if (state == ViewState.ThirdPerson)
            {
                SetArmLayers(InvisibleLayer);
                SetAvatarLayers(VisibleLayer, ShadowCastingMode.On);
            }


        }




        public ViewState GetViewState()
        {
            // Return ViewState.None if dead
            // Return ViewState.ThirdPerson if a different player (or perhaps ViewState.FirstPerson if you are spectating through that player)
            // Return ViewState.ThirdPerson if a third person mod is in use, or any other feature is in use that puts you in third person
            // Return ViewState.FirstPerson otherwise

            /* // Lethal Company implementation
            if (!controller.isPlayerControlled) //Dead, render nothing
            {
                return ViewState.None;
            }
            if (GameNetworkManager.Instance.localPlayerController != controller) //Other player, render third person
            {
                return ViewState.ThirdPerson;
            }
            */

            return ViewState.FirstPerson; //Because none of the above triggered, we are in first person

        }
        public void SetPlayerRenderers(bool enabled)
        {
            // Enable or Disable base character rendering, shadows, etc...

            /* // Lethal Company Implementation
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


            controller.thisPlayerModel.shadowCastingMode = enabled ? ShadowCastingMode.On : ShadowCastingMode.Off;
            controller.thisPlayerModelLOD1.shadowCastingMode = enabled ? ShadowCastingMode.On : ShadowCastingMode.Off;
            controller.thisPlayerModelLOD2.shadowCastingMode = enabled ? ShadowCastingMode.On : ShadowCastingMode.Off;

            nameTagObj.enabled = enabled;
            nameTagObj2.enabled = enabled;
            */
        }
        public void SetPlayerLayers(int layer)
        {
            // Manage base character layers, may not be necessary for Content Warning, especially if there is no No PostProcessing layer

            /* // Lethal Company Implementation
            controller.thisPlayerModel.gameObject.layer = layer;
            controller.thisPlayerModelLOD1.gameObject.layer = layer;
            controller.thisPlayerModelLOD2.gameObject.layer = layer;
            nameTagObj.gameObject.layer = layer;
            nameTagObj2.gameObject.layer = layer;
            */
        }
        public void SetAvatarLayers(int layer, ShadowCastingMode mode)
        {
            // Manage replacement model layers and shadowcasting mode

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
            // Manage replacement arm layers (and set base arms invisible too, I guess)

            if (replacementViewModel)
            {
                //controller.thisPlayerModelArms.gameObject.layer = InvisibleLayer;
                Renderer[] renderers = replacementViewModel.GetComponentsInChildren<Renderer>();
                foreach (Renderer renderer in renderers)
                {
                    renderer.gameObject.layer = layer;
                    renderer.shadowCastingMode = ShadowCastingMode.Off;
                }
            }
            else
            {
                //controller.thisPlayerModelArms.gameObject.layer = layer;
            }
        }
        public void RendererPatches()
        {
            // Run patches for compatability, and to implement No PostProcessing, if necessary
            //PatchViewState();


        }

    }
}
