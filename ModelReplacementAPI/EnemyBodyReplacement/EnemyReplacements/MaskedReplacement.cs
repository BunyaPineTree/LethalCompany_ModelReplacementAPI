using GameNetcodeStuff;
using ModelReplacement.Enemies;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace ModelReplacement.EnemyBodyReplacement.EnemyReplacements
{
    internal class MaskedReplacement : EnemyReplacementBase
    {
        protected override GameObject LoadAssetsAndReturnModel()
        {
            PlayerControllerB mimick = (enemyAI as MaskedPlayerEnemy).mimickingPlayer;

            if(mimick == null)
            {
                int n = StartOfRound.Instance.allPlayerScripts.Count();



            }


        
        }
    }
}
