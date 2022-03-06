using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using QTool.Inspector;
using System.Threading.Tasks;

namespace QTool.FOV
{
    public class QFovAgentTest : QFovAgent
    {
     
        public enum TestType
        {
            ���÷���,
            �Ż�����,
        }
        public TestType type = TestType.�Ż�����;
        public bool OldType => type == TestType.���÷���;
        [ViewName("��С�ϰ���ߴ�",nameof(OldType))]
        [Range(0.5f,3f)]
        public float minObstacleSize = 1;
        [ViewName("ϸ�����Ƕ�", nameof(OldType))]
        [Range(0.1f, 2)]
        public float minCastAngel = 1;
        [ViewName("��Ե���̽Ƕ�", nameof(OldType))]
        [Range(5, 20)]
        public float maxHitAngle =10;
        public Transform moveTarget;
        Vector3? lastPosition;
        protected override void LateUpdate()
        {
            FindTarget();
            if (OldType)
            {
                RayFOV();
            }
            else
            { 
                FindObstacleFOV();
            }
           
        }
        private void Update()
        {
            var moveDir = new Vector3(
                Input.GetKey(KeyCode.D) ? 1 : (Input.GetKey(KeyCode.A) ? -1 : 0), 0,
                Input.GetKey(KeyCode.W) ? 1 : (Input.GetKey(KeyCode.S) ? -1 : 0));
            moveTarget.position += moveDir * 2 * Time.deltaTime;
            transform.LookAt(Camera.main.ScreenPointToRay(Input.mousePosition).RayCastPlane(Vector3.up, Vector3.zero));
        }
        [ContextMenu("����")]
        public void Test()
        {
            Tool.RunTimeCheck("���÷���", () =>
            {
                for (int i = 0; i < 1000; i++)
                {
                    RayFOV();
                }
            });
            Tool.RunTimeCheck("�Ż���", () =>
            {
                for (int i = 0; i < 1000; i++)
                {
                    FindObstacleFOV();
                }
            });
        }
     
        List<QFovTarget> disVisibleTargets = new List<QFovTarget>();
      
        HitInfo? lastHit;
        /// <summary>
        /// ����ǰ�������Ƕȵ���ײ��� ����ϸ�������߼��
        /// </summary>
        /// <param name="lastHit">��һ���Ƕȵ���ײ��Ϣ</param>
        /// <param name="nextHit">��һ���Ƕȵ���ײ��Ϣ</param>
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
        
        void RayFOV()
        {
            hitInfoList.Clear();
            AngelFov(-lookAngle/2,lookAngle/2,lookCheckAngle);
            AngelFov(lookAngle/2 , 360-lookAngle/2, bodyCheckAngle);
        }
    }
    
}