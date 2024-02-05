using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace ModelReplacement.AvatarBodyUpdater
{
    #region model setup classes
    public class RotationOffset : MonoBehaviour
    {
        public Quaternion offset = Quaternion.identity;

        public bool RenderMCC = false;
        public Vector3 MCCPosition = Vector3.zero;
        public Quaternion MCCRotation = Quaternion.identity;
        public Vector3 MCCScale = Vector3.one;
    }
    public class OffsetBuilder : MonoBehaviour
    {
        public Vector3 rootPositionOffset = new Vector3(0, 0, 0);
        public Vector3 rootScale = new Vector3(1, 1, 1);
        public Vector3 itemPositonOffset = new Vector3(0, 0, 0);
        public Quaternion itemRotationOffset = Quaternion.identity;
        public GameObject itemHolder = null;
        public bool UseNoPostProcessing = false;
        public bool GenerateViewModel = false;
        public bool RemoveHelmet = false;
        public GameObject ViewModel = null;
    }
    #endregion
}
