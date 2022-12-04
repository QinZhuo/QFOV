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
        Vector3? lastPosition;
        private void Update()
        {
#if ENABLE_INPUT_SYSTEM
			var mousePos = UnityEngine.InputSystem.Mouse.current.position.ReadValue();
			var keyboard = UnityEngine.InputSystem.Keyboard.current;
			var moveDir = new Vector3(
				keyboard.dKey.isPressed ? 1 : (keyboard.aKey.isPressed ? -1 : 0), 0,
				keyboard.wKey.isPressed ? 1 : (keyboard.sKey.isPressed ? -1 : 0));
#else
			var mousePos = Input.mousePosition;
			var moveDir = new Vector3(
                Input.GetKey(KeyCode.D) ? 1 : (Input.GetKey(KeyCode.A) ? -1 : 0), 0,
                Input.GetKey(KeyCode.W) ? 1 : (Input.GetKey(KeyCode.S) ? -1 : 0));
#endif
			moveTarget.position += moveDir * 2 * Time.deltaTime;
            transform.LookAt(Camera.main.ScreenPointToRay(mousePos).RayCastPlane(Vector3.up, Vector3.zero));
        }
    }
    
}
