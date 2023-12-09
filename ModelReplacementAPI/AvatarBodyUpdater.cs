using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace ModelReplacement
{
    internal class AvatarBodyUpdater : MonoBehaviour
    {

        void LateUpdate()
        {
            Animator a = new Animator();
            Animator b = new Animator();

            var c =Enum.GetValues(typeof(HumanBodyBones));

            foreach (var value in c)
            {
                Transform at = a.GetBoneTransform((HumanBodyBones)value);
                Transform bt = a.GetBoneTransform((HumanBodyBones)value);
                if((at == null) || (bt == null)) { continue; }

                bt.rotation = new Quaternion(at.rotation.x, at.rotation.y, at.rotation.z, at.rotation.w);
                //bt.rotation *= rotationOffset;


            }





        }



    }
}
