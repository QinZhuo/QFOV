using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace QTool.FOV
{
    public class QFovView: MonoBehaviour
    {
        public Material mat;
        public QFovAgent agent;
        [Range(1f, 15)]
        public float meshAngle = 1;
        [Range(0,100)]
        public float maskRadius=50;
        private void Reset()
        {
            agent = GetComponentInParent<QFovAgent>();
        }
        public VertexInfo VI(Vector3 position)
        {
            var v= new VertexInfo
            {
                position = position
            };
            return v;
        }
        //public void OnRenderObject1()
        //{
        //    QGL.Start(mat,0,false);
        //    HitInfo ? lastInfo = null;
        //    foreach (var hit in agent.hitInfoList)
        //    {
        //        if (lastInfo != null)
        //        {
        //            QGL.DrawTriangle(
        //            VI(transform.position),
        //            VI(lastInfo.Value.point),
        //            VI(hit.point));
        //        }
        //        lastInfo = hit;
        //    }
        //    QGL.End();
        //}

        public void Draw(HitInfo last, HitInfo hit)
        {

            var hasObstacle = last.other != null&&hit.other!=null;
            var startAngle = last.angle;
            var endAngle = hit.angle;
            if (startAngle > endAngle)
            {
                endAngle += 360;
            }
            var offset = endAngle - startAngle;
            for (float angle = startAngle; angle < endAngle; angle += meshAngle)
            {
                var nextAngle = angle + meshAngle;
                if (angle + meshAngle > endAngle)
                {
                    nextAngle = endAngle;
                }
                var dir = agent.GetDir(angle);
                var nextDir = agent.GetDir(nextAngle);
                var a = hasObstacle ? Vector3.Lerp(last.point, hit.point, (angle - startAngle) / offset)
                    : (agent.transform.position + dir * agent.GetDistance(angle));
                var b = agent.transform.position + dir * maskRadius;
                var c = hasObstacle ? Vector3.Lerp(last.point, hit.point, (nextAngle - startAngle) / offset)
                    : (agent.transform.position + nextDir * agent.GetDistance(nextAngle));
                var d = agent.transform.position + nextDir * maskRadius;

                QGL.DrawTriangle(
                     VI(a),
                     VI(b),
                     VI(d));
                QGL.DrawTriangle(
                      VI(a),
                      VI(d),
                      VI(c));
            }
        }
        public void Draw(float startAngle,float endAngle,Vector3? startPos=null,Vector3? endPos=null)
        {
           
           
        }
        
        public void OnRenderObject()
        {
            
            QGL.Start(mat, 0, false);
            HitInfo? lastInfo = null;
            foreach (var hit in agent.hitInfoList)
            {
                if (lastInfo != null)
                {
                    Draw(lastInfo.Value, hit);
                }
                lastInfo = hit;
            }
            if (agent.hitInfoList.Count>=2)
            {
                Draw(lastInfo.Value, agent.hitInfoList[0]);
            }
            QGL.End();
        }
    }
}