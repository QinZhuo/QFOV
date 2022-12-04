using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using QTool.Inspector;
using System.Threading.Tasks;

namespace QTool.FOV
{
    public class QFovAgent : MonoBehaviour
    {
		[QGroup(true)]
        [QName("感知半径")]
        [Range(0,30)]
        public float bodyRadius = 10;
        [QName("视野半径")]
        [Range(0, 100)]
        public float lookRadius = 15;
        [QName("视野角度")]
        [Range(0, 180)]
		public float lookAngle = 90;
		[QName("遮挡物Mask")]
		public LayerMask obstacleMask;
		[QName("模式")]
		[QGroup(false)]
		public QFovMode Mode = QFovMode.边缘检测;
		[QGroup(true)]
		[QName("最小障碍物尺寸", nameof(Mode) + "==" + nameof(QFovMode.射线检测))]
		[Range(0.5f, 3f)]
		public float minObstacleSize = 1;
		[QName("细化检测角度", nameof(Mode) + "==" + nameof(QFovMode.射线检测))]
		[Range(0.1f, 2)]
		public float minCastAngel = 1;
		[QName("边缘容忍角度", nameof(Mode) + "==" + nameof(QFovMode.射线检测))]
		[Range(5, 20)]
		[QGroup(false)]
		public float maxHitAngle = 10;


		public readonly List<QFovHitInfo> hitInfoList = new List<QFovHitInfo>();
		Vector3? lastPosition;
		protected virtual void LateUpdate()
		{
			if (Mode == QFovMode.射线检测)
			{
				RayFOV();
			}
			else
			{
				FindObstacleFOV();
			}
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
       
        /// <summary>
        /// 根据角度进行射线检测
        /// </summary>
        /// <param name="angle">角度</param>
        protected QFovHitInfo AngelCast(float angle,float minDistance=-1)
        {
           var distance = Mathf.Max(minDistance, GetDistance(angle));
            var dir =GetDir(angle) ;
            if (Physics.Raycast(transform.position,  dir, out var hit, distance, obstacleMask))
            {
                return new QFovHitInfo(angle, dir, hit.distance, hit.point, hit.collider);
            }
            else
            {
                return new QFovHitInfo(angle,dir, distance, transform.position + dir * distance);
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
        protected void FindObstacleFOV()
        {
            hitInfoList.Clear();
            var maxRadius = Mathf.Max(lookRadius, bodyRadius);
            checkList.Clear();
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
              
                var hit = new QFovHitInfo(transform, point, other);
                var offsetHit = AngelCast(hit.angle - 0.01f, hit.distance);
                AddHitInfo(hit, offsetHit);
                 var leftPoint = other.ClosestPoint(center - rightDir * checkDis);
                leftPoint =CheckPoint(leftPoint, Vector3.down, other, checkDis);
                leftPoint.y = transform.position.y;
                hit = new QFovHitInfo(transform, leftPoint, other);
                offsetHit = AngelCast(hit.angle + 0.01f, hit.distance);
                AddHitInfo(hit, offsetHit);
            }
            hitInfoList.Sort((a, b) =>
            {
                if (a.angle == b.angle) return 0;
                return (a.angle > b.angle) ? 1 : -1;
            });
        }
	
		public void AddHitInfo(QFovHitInfo hit,QFovHitInfo offsetHit)
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
		void RayFOV()
		{
			hitInfoList.Clear();
			AngelFov(-lookAngle / 2, lookAngle / 2, lookCheckAngle);
			AngelFov(lookAngle / 2, 360 - lookAngle / 2, bodyCheckAngle);
		}
		QFovHitInfo? lastHit;
		/// <summary>
		/// 根据前后两个角度的碰撞检测 进行细化的射线检测
		/// </summary>
		/// <param name="lastHit">上一个角度的碰撞信息</param>
		/// <param name="nextHit">下一个角度的碰撞信息</param>
		void CastMinAngel(QFovHitInfo lastHit, QFovHitInfo nextHit)
		{
			if (lastHit.other == null && nextHit.other == null) return;
			var angleOffset = nextHit.angle - lastHit.angle;
			if (angleOffset <= minCastAngel) return;
			var midHit = AngelCast(lastHit.angle + angleOffset / 2);
			if (lastHit.other == nextHit.other && Vector3.Angle(midHit.point - lastHit.point, nextHit.point - midHit.point) <= maxHitAngle)
			{
				return;
			}
			CastMinAngel(lastHit, midHit);
			hitInfoList.Add(midHit);
			CastMinAngel(midHit, nextHit);
		}

		void AngelFov(float startAngel, float endAngel, float checkAngel)
		{
			if (checkAngel <= 0) return;
			for (float angel = startAngel; angel <= endAngel;)
			{
				var hit = AngelCast(angel);
				if (lastHit != null)
				{
					CastMinAngel(lastHit.Value, hit);
				}
				hitInfoList.Add(hit);
				lastHit = hit;
				var offset = hit.other != null ? checkAngel * 2 : checkAngel;

				if (angel < endAngel && angel + offset > endAngel)
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
	}
	/// <summary>
	/// 迷雾碰撞信息
	/// </summary>
    public struct QFovHitInfo
    {
        public Collider other;
        public Vector3 point;
        public Vector3 dir;
        public float distance;
        public float angle;
        public QFovHitInfo(float angle, Vector3 dir, float distance, Vector3 point, Collider other=null)
        {
            this.angle = angle;

            this.dir = dir;
            this.distance = distance;
            this.point = point;
            this.other = other;
        }
        public QFovHitInfo(Transform agent,Vector3 point, Collider other)
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
	public enum QFovMode
	{
		边缘检测 = 0,
		射线检测 = 1,
	}
}
