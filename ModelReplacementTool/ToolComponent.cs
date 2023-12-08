using GameNetcodeStuff;
using ModelReplacement;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Schema;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Experimental.GlobalIllumination;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
//using UnityEngine.UIElements;

namespace ModelReplacementTool
{
    public class ToolComponent : MonoBehaviour
    {
        public PlayerControllerB controller;
        public BodyReplacementBase bodyReplacement;

        Dictionary<string, BoneMap.MappedBone?> mappedBonesInModel = new Dictionary<string, BoneMap.MappedBone> ();
        Dictionary<string, BonePanel> PanelsPerMappedBone = new Dictionary<string, BonePanel>();
        public bool doSymmetric = true;

        public BoneMap map;
        Transform[] allbones;
        public List<string> GetUnmappedBoneNames()
        {
            var allbones = bodyReplacement.GetMappedBones().Select(x => x.name).ToList();
            List<string> returned = new List<string> ();
            var renderedMapBones = mappedBonesInModel.Values.ToList();

            foreach (var item in allbones)
            {
                returned.Add(item);
            }
            foreach (var item in renderedMapBones)
            {
                if(item is null) {  continue; }
                returned.Remove(item.modelBoneString);
            }


            return returned;

        }
        public void UpdateDropDowns()
        {
            foreach (var item in PanelsPerMappedBone.Values)
            {
                item.PopulateDropdown();
            }
        }
        public BonePanel MapSymmetric(BonePanel panel)
        {
            if (!doSymmetric) { return null; }
            if(panel.mapped == null) { return null; }
            if(panel.mapped.playerBoneString.Length <= 2) { return null; }

            string boneName = panel.mapped.playerBoneString.ToLower();

            string strToReplace = "";
            string repStr = "";
            for (int i = 0;i < boneName.Count()-1; i++)
            {
                char char1 = boneName[i];
                char char2 = boneName[i+1];
               // Console.WriteLine($"{char1}{char2}");
                if((char2 == 'r') || (char2 == 'l'))
                {
                    if ((char1 == '.') || (char1 == '_'))
                    {
                        strToReplace = char1.ToString() + char2.ToString();
                        if(char2 == 'l')
                        {
                            repStr = char1.ToString() + 'r'.ToString();
                        }
                        if (char2 == 'r')
                        {
                            repStr = char1.ToString() + 'l'.ToString();
                        }

                        break;
                    }
                }
            }
            if(strToReplace == "") { return null; }
            if (repStr == "") { return null; }
            string lStr = boneName.Replace(strToReplace,repStr);
            //Console.WriteLine(boneName);
            //Console.WriteLine(lStr);
            BonePanel othPanel = PanelsPerMappedBone[lStr];
            return othPanel;


        }

        Toggle x = null;
        Toggle y = null;
        Toggle z = null;

        Toggle a = null;
        Toggle b = null;

        public bool Symmetric { get { return x.isOn; } set { x.isOn = value; if (value) { antisymmetric = false; notSymmetric = false; } } }
        public bool antisymmetric { get { return y.isOn; } set { y.isOn = value;  if (value) {  Symmetric = false; notSymmetric = false; } } }
        public bool notSymmetric { get { return z.isOn; } set { z.isOn = value; if (value) {   Symmetric = false; antisymmetric = false; } } }



        public bool aa { get { return y.isOn; } set { a.isOn = value; DoRenderHelmet(value); } }
        public bool bb { get { return z.isOn; } set { b.isOn = value; bodyReplacement.renderBase = value; } }
        GameObject camTempParent = new GameObject("CamTempParent");
        Transform camOrigTrans;

        void DoRenderHelmet(bool render)
        {
            if (render)
            {
                controller.cameraContainerTransform.parent = camOrigTrans;
            }
            else
            {
                controller.cameraContainerTransform.parent = camTempParent.transform;
            }
            


        }

        void Awake()
        {
            controller = base.GetComponent<PlayerControllerB>();
            bodyReplacement = base.GetComponent<BodyReplacementBase>();
            map = bodyReplacement.Map;
            camOrigTrans = controller.cameraContainerTransform.parent;

            camTempParent.transform.parent = camOrigTrans;
            camTempParent.transform.localPosition = new Vector3(0, 100, 0);

            

            GameObject itemObj = UnityEngine.Object.Instantiate<GameObject>(UnityEngine.Object.FindObjectOfType<Terminal>().buyableItemsList[0].spawnPrefab);
            var go = itemObj.GetComponent<GrabbableObject>();
            go.itemProperties.canBeGrabbedBeforeGameStart = true;
            go.GrabItemOnClient();
            controller.SwitchToItemSlot(0, go);

            go.parentObject = controller.localItemHolder;



            foreach (var item in playerBoneNames)
            {
                mappedBonesInModel.Add(item, null);
            }

            map.GetMappedBones().ForEach(x =>
            {
                mappedBonesInModel[x.playerBoneString] = x;
                Console.WriteLine( x.rotationOffset.y );
                Console.WriteLine("BONE BONE");
            });


            Console.WriteLine("a");
            allbones = controller.thisPlayerModel.bones;
            string model_name = "ToolPanel";
            var CanvasBase = UnityEngine.Object.Instantiate<GameObject>(Assets.MainAssetBundle.LoadAsset<GameObject>(model_name));


            Console.WriteLine("a");
            string model_name2 = "BonePanel";
            var BonePanelBase = UnityEngine.Object.Instantiate<GameObject>(Assets.MainAssetBundle.LoadAsset<GameObject>(model_name2));
            BonePanelBase.AddComponent<BonePanel>();

            Console.WriteLine("a");
            string model_name22 = "BonePanel2";
            var BonePanelBase2 = UnityEngine.Object.Instantiate<GameObject>(Assets.MainAssetBundle.LoadAsset<GameObject>(model_name22));
            BonePanelBase2.AddComponent<BonePanel>();

            string model_name3 = "BonePanelRotation";
            var BonePanelRotationBase = UnityEngine.Object.Instantiate<GameObject>(Assets.MainAssetBundle.LoadAsset<GameObject>(model_name3));
            BonePanelRotationBase.AddComponent<BonePanel>();
            Console.WriteLine("a");

            var gameCanvas = GameObject.Find("Systems/UI/Canvas");
            Console.WriteLine(gameCanvas);
            CanvasBase.transform.parent = gameCanvas.transform;
            CanvasBase.transform.localPosition = new Vector3(0, 0, 0);
            CanvasBase.transform.localScale = Vector3.one * 0.75f;
            var rect = CanvasBase.GetComponent<RectTransform>();
            rect.offsetMin = new Vector2(-640, -360);
            rect.offsetMax = new Vector2(640, 340);

            var Content = CanvasBase.GetComponentInChildren<VerticalLayoutGroup>().gameObject;
            CanvasBase.GetComponentInChildren<ScrollRect>().elasticity = 1000 ;

            var butt = CanvasBase.GetComponentsInChildren<Button>();
            foreach (var item in butt)
            {
                Console.WriteLine(item.gameObject.name);
                if (item.gameObject.name == "ButtonSave")
                {
                    item.onClick.AddListener(RefreshPanel );
                }

            }
            var togs = CanvasBase.GetComponentsInChildren<Toggle>();
            foreach (var item in togs)
            {
                Console.WriteLine(item.gameObject.name);
                if (item.gameObject.name == "S1") { x = item; }
                if (item.gameObject.name == "S2") { y = item; }
                if (item.gameObject.name == "S3") { z = item; }
                if (item.gameObject.name == "S4") { a = item; }
                if (item.gameObject.name == "S5") { b = item; }

            }
            x.onValueChanged.AddListener((x) => { Symmetric = x; });
            y.onValueChanged.AddListener((x) => { antisymmetric = x; });
            z.onValueChanged.AddListener((x) => { notSymmetric = x; });
            a.onValueChanged.AddListener((x) => { aa = x; });
            b.onValueChanged.AddListener((x) => { bb = x; });

            var rooboneGO = UnityEngine.Object.Instantiate<GameObject>(BonePanelBase, Content.transform);
            rooboneGO.transform.localScale = Vector3.one * 0.7f;
            var bonePanel = rooboneGO.GetComponent<BonePanel>();
            bonePanel.map = map;
            bonePanel.isRootBone = true;
            bonePanel.Initialize(this);

            var itemHolderBoneGO = UnityEngine.Object.Instantiate<GameObject>(BonePanelBase2, Content.transform);
            itemHolderBoneGO.transform.localScale = Vector3.one * 0.7f;
            var itemPanel = itemHolderBoneGO.GetComponent<BonePanel>();
            itemPanel.map = map;
            itemPanel.isItemHolder = true;
            itemPanel.Initialize(this);

            foreach (var item in playerBoneNames)
            {
                var rotationBone = UnityEngine.Object.Instantiate<GameObject>(BonePanelRotationBase, Content.transform);
                rotationBone.transform.localScale = Vector3.one * 0.7f;
                var bpanel = rotationBone.GetComponent<BonePanel>();
                bpanel.SetMappedBone(mappedBonesInModel[item], item);
                bpanel.Initialize(this);

                PanelsPerMappedBone.Add(item.ToLower(), bpanel);
            }


            bodyReplacement.renderLocalDebug = true;
            bodyReplacement.renderBase = true;
            bodyReplacement.renderModel = true;

        }
        void RefreshPanel()
        {
            GUI.FocusControl(null);
            Console.WriteLine("REF");
            string jsonStr = map.SerializeToJsonString();
            jsonStr= JValue.Parse(jsonStr).ToString(Formatting.Indented);
            Console.WriteLine(jsonStr);
            string toPath = bodyReplacement.jsonPath;
            Console.WriteLine(toPath);
            File.WriteAllText(toPath, jsonStr);
        }


        void Start()
        {
            controller.gameObject.transform.position += new Vector3(0, 0, 1);
            controller.gameObject.transform.rotation *= Quaternion.AngleAxis(-90, Vector3.up);
            initialPlayerLocation = controller.gameObject.transform.position;
            intialPlayerRotation = controller.gameObject.transform.rotation;

            

        }

        void Update()
        {
            //controller.gameObject.transform.position = initialPlayerLocation;
            //controller.gameObject.transform.rotation = intialPlayerRotation;
            //UpdateCamera();

            foreach (var item in FindObjectsOfType<WalkieTalkie>())
            {
                if(item.gameObject.transform.parent == null) { continue; }
                Console.WriteLine($"{item.gameObject.name} {item.gameObject.transform.parent.gameObject.name} ");
                if (item.gameObject.transform.parent.gameObject.name == "HangarShip")
                {
                    Destroy(item.gameObject);
                }
            }
        }

        void LateUpdate()
        {
            controller.gameObject.transform.position = new Vector3(initialPlayerLocation.x, controller.gameObject.transform.position.y, initialPlayerLocation.z);
            controller.gameObject.transform.rotation = intialPlayerRotation;
            UpdateCamera();
        }

        Camera personCam2;
        public Vector3 initialPlayerLocation;
        Quaternion intialPlayerRotation;

        Vector2 previousMousePosition;
        Vector2 centerPosition;

        float desiredMoveSpeed = 10;
        public void UpdateCamera()
        {
            if (personCam2 == null)
            {
                GameObject kobj = GameObject.Find("Environment/HangarShip/Player/ScavengerModel/metarig/CameraContainer/MainCamera");
                Camera personCam = kobj.GetComponent<Camera>();
                Console.WriteLine(kobj);
                Console.WriteLine(personCam);
                Console.WriteLine("Set");
                GameObject cam = GameObject.Instantiate(kobj, kobj.transform);
                Console.WriteLine("Set2");
                personCam2 = cam.GetComponent<Camera>();
                Console.WriteLine("Set3");
                //cam.transform.position = personCam.transform.position;
                //cam.transform.rotation = personCam.transform.rotation;
                personCam2.enabled = true;
                personCam.enabled = false;
                personCam.gameObject.SetActive(false);
                //personCam2.cullingMask = personCam.cullingMask;
                personCam2.transform.parent = null;
                Cursor.lockState = CursorLockMode.None;
                centerPosition = Mouse.current.position.ReadValue();
                Cursor.visible = true;
                Console.WriteLine("Set4");
                // personCam2.depth = -0.65f;

                personCam2.transform.position += new Vector3(3, 0, 0);
                personCam2.transform.rotation *= Quaternion.AngleAxis(180, Vector3.up);
                personCam2.transform.LookAt(personCam.transform, Vector3.up);

                // Add the light component
                GameObject light = GameObject.Instantiate(GameObject.Find("Environment/HangarShip/ShipElectricLights/Area Light (8)"), cam.transform);
                light.transform.localPosition = Vector3.zero;
                light.GetComponent<Light>().intensity = 75;

            }


            float num = desiredMoveSpeed * Time.deltaTime;
            bool flag2 = Keyboard.current.leftShiftKey.isPressed;
            if (flag2)
            {
                num *= 10f;
            }

            bool flag3 = Keyboard.current.aKey.isPressed;
            if (flag3)
            {
                personCam2.transform.position += personCam2.transform.right * -1f * num;
            }
            bool flag4 = Keyboard.current.dKey.isPressed;
            if (flag4)
            {
                personCam2.transform.position += personCam2.transform.right * num;
            }
            bool flag5 = Keyboard.current.wKey.isPressed;
            if (flag5)
            {
                personCam2.transform.position += personCam2.transform.forward * num;
            }
            bool flag6 = Keyboard.current.sKey.isPressed;
            if (flag6)
            {
                personCam2.transform.position += personCam2.transform.forward * -1f * num;
            }
            bool flag7 = Keyboard.current.spaceKey.isPressed;
            if (flag7)
            {
                personCam2.transform.position += personCam2.transform.up * num;
            }
            bool flag8 = Keyboard.current.ctrlKey.isPressed;
            if (flag8)
            {
                personCam2.transform.position += personCam2.transform.up * -1f * num;
            }
            bool mouseButton = Mouse.current.rightButton.isPressed;
            Vector2 vector = Mouse.current.position.ReadValue() - previousMousePosition;

            if (mouseButton)
            {

                Console.WriteLine($"{vector.x} {vector.y}");
                float num2 = personCam2.transform.localEulerAngles.y + vector.x * 0.1f;
                float num3 = personCam2.transform.localEulerAngles.x - vector.y * 0.1f;
                personCam2.transform.localEulerAngles = new Vector3(num3, num2, 0f);
            }



            if (vector.magnitude == 0)
            {
                //Mouse.current.WarpCursorPosition(centerPosition);
            }
            previousMousePosition = Mouse.current.position.ReadValue();
        }




        private List<string> playerBoneNames = new List<string>()
        {
     "spine",
     "spine.001",
     "spine.002",
     "spine.003",
     "shoulder.L",
     "arm.L_upper",
     "arm.L_lower",
     "hand.L",
     "finger5.L",
     "finger5.L.001",
     "finger4.L",
     "finger4.L.001",
     "finger3.L",
     "finger3.L.001",
     "finger2.L",
     "finger2.L.001",
     "finger1.L",
     "finger1.L.001",
     "shoulder.R",
     "arm.R_upper",
     "arm.R_lower",
     "hand.R",
     "finger5.R",
     "finger5.R.001",
     "finger4.R",
     "finger4.R.001",
     "finger3.R",
     "finger3.R.001",
     "finger2.R",
     "finger2.R.001",
     "finger1.R",
     "finger1.R.001",
     "spine.004",
     "thigh.L",
     "shin.L",
     "foot.L",
     "toe.L",
     "heel.02.L",
     "thigh.R",
     "shin.R",
     "foot.R",
     "toe.R",
     "heel.02.R"
         };


    }


}
