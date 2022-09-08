using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using QTool.Inspector;
using System.Threading.Tasks;

namespace QTool.FOV
{
    public class QFovAgent : MonoBehaviour
    {

        [QName("遮挡物Mask")]
        public LayerMask obstacleMask;
        [QName("感知半径")]
        [Range(0,30)]
        public float bodyRadius = 10;
        [QName("视野半径")]
        [Range(0, 100)]
        public float lookRadius = 15;
        [QName("视野角度")]
        [Range(0, 180)]
        public float lookAngle = 90;
        [QName("目标Mask")]
        public LayerMask targetMask;
        public readonly List<QFovTarget> visibleTargets = new List<QFovTarget>();
        public readonly List<HitInfo> hitInfoList = new List<HitInfo>();
        Vector3? lastPosition;
        protected virtual void LateUpdate()
        {
            FindObstacleFOV();
            FindTarget();
        }
     
        public float GetDistance(float angle)
        {
            while (angle<0)
            {
                angle += 360;
            }angle %= 360;
            return angle < lookAngle / 2|| angle>360-lookAngle/2 ? lookRadius : bodyRadius;
        }
        public Vector3 GetDir(float angle)
        {
            angle += transform.eulerAngles.y;
            return new Vector3(Mathf.Sin(angle * Mathf.Deg2Rad), 0, Mathf.Cos(angle * Mathf.Deg2Rad));
        }
        List<QFovTarget> disVisibleTargets = new List<QFovTarget>();
        protected void FindTarget()
        {
            disVisibleTargets.AddRange(visibleTargets);
            var maxRadius = Mathf.Max(lookRadius, bodyRadius);
            var others = Physics.OverlapSphere(transform.position, maxRadius, targetMask);
            foreach (var other in others)
            {
                var qTarget = other.GetComponentInParent<QFovTarget>();
                if (qTarget != null)
                {
                    var dir = (qTarget.transform.position - transform.position).normalized;
                    var maxDis = GetDistance(Vector3.Angle(dir, transform.forward)) ;
                    var dis = Vector3.Distance(qTarget.transform.position, transform.position);
                    if (dis < maxDis)
                    {
                        if (!Physics.Raycast(transform.position, dir, dis, obstacleMask)){
                            if (disVisibleTargets.Contains(qTarget))
                            {
                                disVisibleTargets.Remove(qTarget);
                            }
                            else
                            {
                                visibleTargets.Add(qTarget);
                                qTarget.View(true);
                               
                            }
                        }
                    }
                }
            }
            foreach (var v in disVisibleTargets)
            {
                visibleTargets.Remove(v);
                v.View(false);
            }
        }

        /// <summary>
        /// 根据角度进行射线检测
        /// </summary>
        /// <param name="angle">角度</param>
        protected HitInfo AngelCast(float angle,float minDistance=-1)
        {
           var distance = Mathf.Max(minDistance, GetDistance(angle));
            var dir =GetDir(angle) ;
            if (Physics.Raycast(transform.position,  dir, out var hit, distance, obstacleMask))
            {
                return new HitInfo(angle, dir, hit.distance, hit.point, hit.collider);
            }
            else
            {
                return new HitInfo(angle,dir, distance, transform.position + dir * distance);
            }
        }

        protected void OnDrawGizmosSelected()
        {
            foreach (var hit in hitInfoList)
            {
                Gizmos.color = hit.other!=null ? Color.red : Color.green;
                Gizmos.DrawRay(transform.position, hit.point - transform.position);
            }
         
        }

        protected Vector3 CheckPoint(Vector3 point,Vector3 up,Collider other,float checkDis)
        {
            var dir = Vector3.Cross( point - transform.position, up);
            var newPoint = other.ClosestPoint(point + dir.normalized * checkDis);
            if (Vector3.Distance( newPoint , point)<0.01f)
            {
                return point;
            }
            else
            {
                return CheckPoint(newPoint, up, other, checkDis);
            }
        }
        protected List<Collider> checkList = new List<Collider>();
        //public struct CheckAngle
        //{
        //    public float angle;
        //    public float distance;
        //    public HitInfo fromAngle;
        //}
        protected void FindObstacleFOV()
        {
            hitInfoList.Clear();
            var maxRadius = Mathf.Max(lookRadius, bodyRadius);
            checkList.Clear();
          //  checkAngle.Add(lookAngle / 2);
           // checkAngle.Add(lookAngle / 2);
            checkList.AddRange( Physics.OverlapSphere(transform.position, maxRadius, obstacleMask));
            foreach (var o in Physics.OverlapSphere(transform.position, 0.1f, obstacleMask))
            {
                checkList.Remove(o);
            }
            foreach (var other in checkList)
            {
                var checkDis = Mathf.Max(other.bounds.size.x, other.bounds.size.z)+1000;
                var center = new Vector3(other.transform.position.x, other.bounds.center.y-other.bounds.size.y, other.transform.position.z);
                var rightDir = Vector3.Cross(center - transform.position, Vector3.up).normalized;
                var point = other.ClosestPoint(center + rightDir * checkDis);
                point =CheckPoint(point, Vector3.up, other, checkDis);
                point.y = transform.position.y;
              
                var hit = new HitInfo(transform, point, other);
                var offsetHit = AngelCast(hit.angle - 0.01f, hit.distance);
                AddHitInfo(hit, offsetHit);
                 var leftPoint = other.ClosestPoint(center - rightDir * checkDis);
                leftPoint =CheckPoint(leftPoint, Vector3.down, other, checkDis);
                leftPoint.y = transform.position.y;
                hit = new HitInfo(transform, leftPoint, other);
                offsetHit = AngelCast(hit.angle + 0.01f, hit.distance);
                AddHitInfo(hit, offsetHit);
            }
            hitInfoList.Sort((a, b) =>
            {
                if (a.angle == b.angle) return 0;
                return (a.angle > b.angle) ? 1 : -1;
            });
        }
        public void AddHitInfo(HitInfo hit,HitInfo offsetHit)
        {
            if (offsetHit.distance < hit.distance)
            {
                hit.point += hit.dir * (offsetHit.distance-hit.distance);
                hit.distance = offsetHit.distance;
                hit.other = offsetHit.other;
            }
            hitInfoList.Add(hit);
            hitInfoList.Add(offsetHit);
        }
    }
    public struct HitInfo
    {
        public Collider other;
        public Vector3 point;
        public Vector3 dir;
        public float distance;
        public float angle;
        public HitInfo(float angle, Vector3 dir, float distance, Vector3 point, Collider other=null)
        {
            this.angle = angle;

            this.dir = dir;
            this.distance = distance;
            this.point = point;
            this.other = other;
        }
        public HitInfo(Transform agent,Vector3 point, Collider other)
        {
            this.other = other;
            this.point = point;
            var offset = point - agent.position;
            this.distance = offset.magnitude;
            this.dir = offset.normalized;
            this.angle = Vector3.Angle(agent.forward, dir);
            if (Vector3.Dot(agent.right, dir)  <0)
            {
                angle = 360-angle;
            }
        }
    }
}
