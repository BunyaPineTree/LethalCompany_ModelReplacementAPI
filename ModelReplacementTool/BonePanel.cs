using ModelReplacement;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using Unity;
//using UnityEngine.UIElements;
using TMPro;
using System.Linq;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace ModelReplacementTool
{
    public class BonePanel : MonoBehaviour
    {
        public string playerBoneString;
        public string modelBoneString;
        public Quaternion rotationOffset = Quaternion.identity;

        public Transform playerTransform;
        public Transform modelTransform;

        TextMeshProUGUI playerText = null;
        TMP_Dropdown dropDown;

        Slider x = null;
        Slider y = null;
        Slider z = null;

        

        public float xf { get { return x.value; } set { x.value = value; }  }
        public float yf { get { return y.value; } set { y.value = value; } }
        public float zf { get { return z.value; } set { z.value = value; } }

      


        public BoneMap.MappedBone mapped = null;
        public BoneMap map = null;
        public bool isRootBone = false;
        public bool isItemHolder = false;

        void Awake()
        {
            

            var texts = base.GetComponentsInChildren<TextMeshProUGUI>();
            foreach (var item in texts)
            {
                Console.WriteLine(item.gameObject.name);
                if(item.gameObject.name == "playerName") { playerText = item; }
            }

            var inputs = base.GetComponentsInChildren<Slider>();
            foreach (var item in inputs)
            {
                Console.WriteLine(item.gameObject.name);
                if (item.gameObject.name == "SliderX") { x = item; }
                if (item.gameObject.name == "SliderY") { y = item; }
                if (item.gameObject.name == "SliderZ") { z = item; }
            }

            var drop = base.GetComponentsInChildren<TMP_Dropdown>();
            foreach (var item in drop)
            {
                Console.WriteLine(item.gameObject.name);
                if (item.gameObject.name == "boneDropDown") { dropDown = item; }
            }
            dropDown.onValueChanged.AddListener(delegate {
                SetModelBone(dropDown);
            });

            var butt = base.GetComponentsInChildren<Button>();
            foreach (var item in butt)
            {
                Console.WriteLine(item.gameObject.name);
                if (item.gameObject.name == "ButtonSet") 
                {
                    item.onClick.AddListener(SetValues);
                }
                if (item.gameObject.name == "ButtonReset")
                {
                    item.onClick.AddListener(ResetValues);
                }
            }


        }

        void SetValues()
        {
            if ((map != null))
            {
                if (isRootBone)
                {
                    map.UpdateRootBoneAndOffset(map.RootBone(), new Vector3(xf, yf, zf));


                }
                if (isItemHolder)
                {
                    map.UpdateItemHolderBoneAndOffset(map.ItemHolder(), new Vector3(xf, yf, zf));

                }
            }
            if (mapped != null)
            {
                comp.map.UpdateMappedBone(mapped.playerBoneString, mapped.modelBoneString, Quaternion.Euler(xf, yf, zf));
            }


        }

        void ResetValues()
        {
            xf = 0;
            yf = 0;
            zf = 0;
        }

        void SetModelBone(TMP_Dropdown change)
        {
            string newModelBone = change.options[change.value].text;
            if(newModelBone == GetModelBoneIfExists()) { return; }

            if ((map != null))
            {
                if (isRootBone)
                {
                    


                }
                if (isItemHolder)
                {
                   

                    var b = map.modelBoneList.Where(x => x.name == modelBoneString);

                    Transform mdTransform = null;

                    if (b.Any()) { map.UpdateItemHolderBoneAndOffset(b.First(),map.ItemHolderPositionOffset()); }
                    else
                    {
                        Console.WriteLine("AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA");
                    }
                }
            }
            if (mapped != null)
            {
                comp.map.UpdateMappedBone(mapped.playerBoneString, newModelBone, mapped.rotationOffset);


            }

            comp.UpdateDropDowns();
        }

        void Update()
        {
            if (!Mouse.current.leftButton.isPressed) { GUI.FocusControl(null); }
             SetValues();
            if(comp != null)
            {
                BonePanel othPanel = comp.MapSymmetric(this);
                if(othPanel != null)
                {

                    othPanel.xf = xf;
                    othPanel.yf = -yf;
                    othPanel.zf = -zf;
                }
                
            }
            

        }

        public void SetMappedBone(BoneMap.MappedBone mappedBone, string name)
        {
            mapped = mappedBone;
            playerText.text = name;

        }

        private ToolComponent comp;
        public void Initialize(ToolComponent comp)
        {
            this.comp = comp;
            PopulateDropdown();

            //Rotaation
            if ((mapped != null))
            {

                var tempRot = mapped.rotationOffset.eulerAngles;
                xf = tempRot.x;
                yf = tempRot.y;
                zf = tempRot.z;


            }
            //position
            if ((map != null))
            {
                if (isRootBone)
                {
                    playerText.text = "player RootBone";
                    xf = map.PositionOffset().x;
                    yf = map.PositionOffset().y;
                    zf = map.PositionOffset().z;



                }
                if (isItemHolder)
                {
                    playerText.text = "player ItemHolder";
                    xf = map.ItemHolderPositionOffset().x;
                    yf = map.ItemHolderPositionOffset().y;
                    zf = map.ItemHolderPositionOffset().z;
                }

            }


        }

        private string? GetModelBoneIfExists()
        {
            if ((map != null))
            {
                if (isRootBone)
                {
                    return map.RootBone().name;
                }
                if (isItemHolder)
                {
                    return map.ItemHolder().name;
                }
            }
            if(mapped != null)
            {
                return mapped.modelBoneString;
            }
            return null;
        }

        public void PopulateDropdown()
        {
            var a = GetModelBoneIfExists();
            string thisName = "Null";
            if (a != null)
            {
                thisName = a;
            }
            List<string> options = new List<string>();
            options.Add(thisName);
            if (!this.isRootBone)
            {
                if (thisName != "Null")
                {
                    options.Add("Null");
                }
                options.AddRange(comp.GetUnmappedBoneNames());

            }



            dropDown.ClearOptions();
            dropDown.AddOptions(options);

            dropDown.value = 0;
        }


    }
}
