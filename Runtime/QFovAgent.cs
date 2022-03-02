using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using QTool.Inspector;
using System.Threading.Tasks;

namespace QTool.FOV
{
    public class QFovAgent : MonoBehaviour
    {

        [ViewName("遮挡物Mask")]
        public LayerMask obstacleMask;
        [ViewName("感知半径")]
        [Range(0,30)]
        public float bodyRadius = 3;
        [ViewName("视野半径")]
        [Range(0, 100)]
        public float lookRadius = 10;
        [ViewName("视野角度")]
        [Range(0, 180)]
        public float lookAngle = 90;
        [ViewName("最小障碍物尺寸")]
        [Range(0.5f,3f)]
        public float minObstacleSize = 1;
        [ViewName("细化检测角度")]
        [Range(0.1f, 2)]
        public float minCastAngel = 1;
        [ViewName("边缘容忍角度")]
        [Range(5, 20)]
        public float maxHitAngle =10;

        [ViewName("目标Mask")]
        public LayerMask targetMask;
        public List<QFovTarget> visibleTargets = new List<QFovTarget>();
        Vector3? lastPosition;
        private void LateUpdate()
        {
          //  FindObstacle();
          //  FindTarget();
          // FOV();
        }
        List<QFovTarget> disVisibleTargets = new List<QFovTarget>();
        void FindTarget()
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
                    var maxDis = (Vector3.Angle(dir, transform.forward) < lookAngle / 2) ? lookRadius : bodyRadius;
                    var dis = Vector3.Distance(qTarget.transform.position, transform.position);
                    if (dis < maxDis)
                    {
                        if(!Physics.Raycast(transform.position, dir, dis, obstacleMask)){
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
        HitInfo AngelCast(float angle,float distance)
        {
            var a = (angle + transform.eulerAngles.y);
            var dir = new Vector3(Mathf.Sin(a * Mathf.Deg2Rad), 0, Mathf.Cos(a * Mathf.Deg2Rad));
            if (Physics.Raycast(transform.position,  dir, out var hit, distance, obstacleMask))
            {
                return new HitInfo(angle, dir, hit.distance, hit.point, hit.collider);
            }
            else
            {
                return new HitInfo(angle,dir, distance, transform.position + dir * distance);
            }
        }
   
        public readonly List<HitInfo> hitInfoList = new List<HitInfo>();
        HitInfo? lastHit;
        /// <summary>
        /// 根据前后两个角度的碰撞检测 进行细化的射线检测
        /// </summary>
        /// <param name="lastHit">上一个角度的碰撞信息</param>
        /// <param name="nextHit">下一个角度的碰撞信息</param>
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
                var offset =hit.other!=null?checkAngel*2: checkAngel;
                
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
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(viewPoint, viewDir );
            Gizmos.DrawSphere(nextPoint, 0.1f);
        }
        public Vector3 viewPoint;
        public Vector3 viewDir;
        public Vector3 nextPoint;
        async Task< Vector3> CheckPoint(Vector3 point,Vector3 up,Collider other,float checkDis)
        {
            await Task.Delay(500);
            //return point;
            // if (other.bounds.Contains(transform.position)) return point;
            viewPoint = point;
            var dir = Vector3.Cross( point - transform.position, up);
            viewDir = dir.normalized * checkDis;
            var newPoint = other.ClosestPoint(point + dir.normalized * checkDis);
            if (Vector3.Distance( newPoint , point)<0.05f)
            {
                return point;
            }
            else
            {
                await Task.Delay(300);
                nextPoint = newPoint;
                return await CheckPoint(newPoint, up, other, checkDis);
            }
        }
        [ViewButton("test")]
        async void FindObstacle()
        {
            hitInfoList.Clear();
            var maxRadius = Mathf.Max(lookRadius, bodyRadius);
            var obstacles = Physics.OverlapSphere(transform.position, maxRadius, obstacleMask);
            foreach (var other in obstacles)
            {


                var checkDis = Mathf.Max(other.bounds.size.x, other.bounds.size.z)+1000;
                var center = new Vector3(other.transform.position.x, other.bounds.center.y-other.bounds.size.y, other.transform.position.z);
                var rightDir = Vector3.Cross(center - transform.position, Vector3.up).normalized;
                var point = other.ClosestPoint(center + rightDir * checkDis);
                point = await CheckPoint(point, Vector3.up, other, checkDis);
                point.y = transform.position.y;
                hitInfoList.Add(new HitInfo(transform, point, other));
                var leftPoint = other.ClosestPoint(center - rightDir * checkDis);
                leftPoint =await CheckPoint(leftPoint, Vector3.down, other, checkDis);
                leftPoint.y = transform.position.y;
                hitInfoList.Add(new HitInfo(transform, leftPoint, other));
                //var dir = (collider.transform.position - transform.position).normalized;
                //var maxDis = (Vector3.Angle(dir, transform.forward) < lookAngle / 2) ? lookRadius : bodyRadius;
                //var dis = Vector3.Distance(collider.transform.position, transform.position);
                //if (dis < maxDis)
                //{
                //    if (!Physics.Raycast(transform.position, dir, dis, obstacleMask))
                //    {
                //        if (disVisibleTargets.Contains(collider))
                //        {
                //            disVisibleTargets.Remove(collider);
                //        }
                //        else
                //        {
                //            visibleTargets.Add(collider);
                //            collider.View(true);
                //        }
                //    }
                //}
            }
            //foreach (var v in disVisibleTargets)
            //{
            //    visibleTargets.Remove(v);
            //    v.View(false);
            //}
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
        }
    }
}