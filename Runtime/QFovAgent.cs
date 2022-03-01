using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using QTool.Inspector;
namespace QTool.FOV
{
    public class QFovAgent : MonoBehaviour
    {
        [ViewName("�ڵ���Mask")]
        public LayerMask obstacleMask;
        [ViewName("��֪�뾶")]
        [Range(0,30)]
        public float bodyRadius = 3;
        [ViewName("��Ұ�뾶")]
        [Range(0, 100)]
        public float lookRadius = 10;
        [ViewName("��Ұ�Ƕ�")]
        [Range(0, 180)]
        public float lookAngle = 90;
        [ViewName("��С�ϰ���ߴ�")]
        [Range(0.5f,3f)]
        public float minObstacleSize = 1;
        [ViewName("ϸ�����Ƕ�")]
        [Range(0.1f, 2)]
        public float minCastAngel = 1;
        [ViewName("��Ե���̽Ƕ�")]
        [Range(5, 20)]
        public float maxHitAngle =10;

        private void LateUpdate()
        {
            FOV();
        }
     
        /// <summary>
        /// ���ݽǶȽ������߼��
        /// </summary>
        /// <param name="angle">�Ƕ�</param>
        HitInfo AngelCast(float angle,float distance)
        {
            var a = (angle + transform.eulerAngles.y);
            var dir = new Vector3(Mathf.Sin(a * Mathf.Deg2Rad), 0, Mathf.Cos(a * Mathf.Deg2Rad));
            if (Physics.Raycast(transform.position,  dir, out var hit, distance, obstacleMask))
            {
                return new HitInfo(hit.collider,dir, hit.point, hit.distance, angle);
            }
            else
            {
                return new HitInfo(null,dir, transform.position + dir * distance, distance, angle);
            }
        }
   
        public readonly List<HitInfo> hitInfoList = new List<HitInfo>();
        HitInfo? lastHit;
        /// <summary>
        /// ����ǰ�������Ƕȵ���ײ��� ����ϸ�������߼��
        /// </summary>
        /// <param name="lastHit">��һ���Ƕȵ���ײ��Ϣ</param>
        /// <param name="nextHit">��һ���Ƕȵ���ײ��Ϣ</param>
        void CastMinAngel(HitInfo lastHit,HitInfo nextHit,float distance)
        {
            if (lastHit.other == null && nextHit.other == null) return;
            var angleOffset = nextHit.angle - lastHit.angle;
            if (angleOffset <= minCastAngel) return;
            var midHit = AngelCast(lastHit.angle + angleOffset / 2, distance);
            if (lastHit.other == nextHit.other&& Vector3.Angle(midHit.point-lastHit.point, nextHit.point - midHit.point) <= maxHitAngle)
            {
                return;
            }
            CastMinAngel(lastHit, midHit, distance);
            hitInfoList.Add(midHit);
            CastMinAngel(midHit, nextHit, distance);
        }
      
        void AngelFov(float startAngel,float endAngel,float checkAngel,float distance)
        {
            if (distance == 0 || checkAngel <= 0) return;
            for (float angel = startAngel; angel <= endAngel;)
            {
                var hit = AngelCast(angel, distance);
                if (lastHit != null)
                {
                    CastMinAngel(lastHit.Value, hit,distance);
                }
                hitInfoList.Add(hit);
                lastHit = hit;
                var offset =hit.other!=null?checkAngel*5: checkAngel;
                
                if (angel<endAngel&& angel + offset > endAngel)
                {
                    angel = endAngel;
                }
                else 
                {
                    angel += offset;
                }
            }
        }
        public float lookCheckAngle => Mathf.Asin(minObstacleSize / lookRadius) * Mathf.Rad2Deg;
        public float bodyCheckAngle => Mathf.Asin(minObstacleSize / bodyRadius) * Mathf.Rad2Deg;
        
        void FOV()
        {
            hitInfoList.Clear();
            AngelFov(-lookAngle/2,lookAngle/2,lookCheckAngle,lookRadius);
            AngelFov(lookAngle/2 , 360-lookAngle/2, bodyCheckAngle, bodyRadius);
        }
       
        private void OnDrawGizmosSelected()
        {
            foreach (var hit in hitInfoList)
            {
                Gizmos.color = hit.other!=null ? Color.red : Color.green;
                Gizmos.DrawRay(transform.position, hit.point - transform.position);
            }
        }
    }
    public struct HitInfo
    {
        public Collider other;
        public Vector3 point;
        public Vector3 dir;
        public float distance;
        public float angle;
        public HitInfo(Collider other,Vector3 dir, Vector3 point, float distance, float angle)
        {
            this.dir = dir;
            this.point = point;
            this.distance = distance;
            this.angle = angle;
            this.other = other;
        }
    }
}