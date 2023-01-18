using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using QTool.Inspector;
using System.Threading.Tasks;

namespace QTool.FOV
{
    public class QFovTest : MonoBehaviour
    {
        public Transform moveTarget;
        private void Update()
        {
			moveTarget.position +=new Vector3( QDemoInput.MoveDirection.x,0,QDemoInput.MoveDirection.y) * 2 * Time.deltaTime;
			transform.LookAt(Camera.main.ScreenPointToRay(QDemoInput.PointerPosition).RayCastPlane(Vector3.up, Vector3.zero));
        }
    }
    
}
