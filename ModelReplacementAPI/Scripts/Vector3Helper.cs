using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace ModelReplacement.Scripts
{
    public static class Vector3Helper
    {
        public static void MoveSmoothly(this Vector3 from, Vector3 to)
        {
            from = Vector3.Lerp(from, to, 0.1f);
        }
    }
}
