﻿using System;
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

namespace ModelReplacement.Monobehaviors
{
    public abstract class ManagerBase : MonoBehaviour
    {
        protected bool bodyReplacementExists = false;
        protected BodyReplacementBase bodyReplacement;
        protected PlayerControllerB controller;
        protected GameObject replacementModel;
        protected GameObject replacementViewModel;
        public bool localPlayer => GameNetworkManager.Instance.localPlayerController == controller;
        protected virtual void Awake()
        {
            controller = GetComponent<PlayerControllerB>();
        }
        protected virtual void Update()
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
        public virtual void ReportBodyReplacementAddition(BodyReplacementBase replacement)
        {
            bodyReplacement = replacement;
            replacementModel = replacement.replacementModel;
            replacementViewModel = replacement.replacementViewModel;
            bodyReplacementExists = true;
        }
        public virtual void ReportBodyReplacementRemoval()
        {
            bodyReplacement = null;
            replacementModel = null;
            replacementViewModel = null;
            bodyReplacementExists = false;
        }

        public abstract void UpdatePlayer();
        public abstract void UpdateModelReplacement();

    }
}
