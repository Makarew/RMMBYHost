using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using MelonLoader;

namespace RMMBYHost
{
    internal class ModButtonSetup : MonoBehaviour
    {
        private int ran = 5;

        public void Update()
        {
            if (ran > 0)
            {
                ran--;
            } else if (ran == 0)
            {
                Melon<Plugin>.Instance.FinshMenuModButton(gameObject);
                ran = -1;
            }
        }
    }
}
