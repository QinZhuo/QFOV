using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class QFovAgent : MonoBehaviour
{
    public float radius=10;
    [Range(0,360)]
    public float angle = 90;
    public Vector3 DirFromAngle(float angle,bool isGlobal)
    {
        if (!isGlobal)
        {
            angle += transform.eulerAngles.y;
        }
        return new Vector3(Mathf.Sin(angle * Mathf.Deg2Rad), 0, Mathf.Cos(angle * Mathf.Deg2Rad));
    }
    public void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}
