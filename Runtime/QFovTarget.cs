using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace QTool.FOV
{
    public class QFovTarget : MonoBehaviour
    {
        public BoolEvent OnVisible;
        public void View(bool visible)
        {
            OnVisible?.Invoke(visible);
        }
        public void Start()
        {
            View(false);
        }

    }
}