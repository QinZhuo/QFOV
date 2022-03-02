using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using QTool.Inspector;
using System.Threading.Tasks;

namespace QTool.FOV
{
    public class QFovAgentTest : QFovAgent
    {
        [ViewName("最小障碍物尺寸")]
        [Range(0.5f,3f)]
        public float minObstacleSize = 1;
        [ViewName("细化检测角度")]
        [Range(0.1f, 2)]
        public float minCastAngel = 1;
        [ViewName("边缘容忍角度")]
        [Range(5, 20)]
        public float maxHitAngle =10;
        Vector3? lastPosition;
        protected override void LateUpdate()
        {
        }
        [ContextMenu("测试")]
        public void Test()
        {
            Tool.RunTimeCheck("老版本测试", () =>
            {
                for (int i = 0; i < 1000; i++)
                {
                    FOV();
                }
            });
            Tool.RunTimeCheck("新版本测试", () =>
            {
                for (int i = 0; i < 1000; i++)
                {
                    FindObstacle();
                }
            });
        }
     
        List<QFovTarget> disVisibleTargets = new List<QFovTarget>();
      
        HitInfo? lastHit;
        /// <summary>
        /// 根据前后两个角度的碰撞检测 进行细化的射线检测
        /// </summary>
        /// <param name="lastHit">上一个角度的碰撞信息</param>
        /// <param name="nextHit">下一个角度的碰撞信息</param>
        void CastMinAngel(HitInfo lastHit,HitInfo nextHit)
        {
            if (lastHit.other == null && nextHit.other == null) return;
            var angleOffset = nextHit.angle - lastHit.angle;
            if (angleOffset <= minCastAngel) return;
            var midHit = AngelCast(lastHit.angle + angleOffset / 2);
            if (lastHit.other == nextHit.other&& Vector3.Angle(midHit.point-lastHit.point, nextHit.point - midHit.point) <= maxHitAngle)
            {
                return;
            }
            CastMinAngel(lastHit, midHit);
            hitInfoList.Add(midHit);
            CastMinAngel(midHit, nextHit);
        }
      
        void AngelFov(float startAngel,float endAngel,float checkAngel)
        {
            if ( checkAngel <= 0) return;
            for (float angel = startAngel; angel <= endAngel;)
            {
                var hit = AngelCast(angel);
                if (lastHit != null)
                {
                    CastMinAngel(lastHit.Value, hit);
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
            AngelFov(-lookAngle/2,lookAngle/2,lookCheckAngle);
            AngelFov(lookAngle/2 , 360-lookAngle/2, bodyCheckAngle);
        }
    }
    
}