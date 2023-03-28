using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace QTool.FOV
{
    public class QFovView: MonoBehaviour
    {
        public Material mat;
        public QFovAgent agent;
        [Range(0.1f, 15)]
        public float meshAngle = 1;
        [Range(0,100)]
        public float maskRadius=50;
        private void Reset()
        {
            agent = GetComponentInParent<QFovAgent>();
        }
  
        public void Draw(QFovHitInfo last, QFovHitInfo hit)
        {

            var hasObstacle = last.other != null&&hit.other!=null;
            var startAngle = last.angle;
            var endAngle = hit.angle;
            if (startAngle > endAngle)
            {
                startAngle -= 360;
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
                Vector3 GetPos(float angle, Vector3 dir)
                {
                    var pos= Vector3.Lerp(last.point, hit.point, (angle - startAngle) / offset);
                    var dis = (pos - agent.transform.position).magnitude;
                    var maxDis = agent.GetDistance(angle);
                    if (dis > maxDis)
                    {
                        return (agent.transform.position + dir * agent.GetDistance(angle)); ;
                    }
                    return pos;
                }
                var nextDir = agent.GetDir(nextAngle);
                var a = hasObstacle ? GetPos(angle, dir)
                    : (agent.transform.position + dir * agent.GetDistance(angle));
                var b = agent.transform.position + dir * maskRadius;
                var c = hasObstacle ? GetPos(nextAngle, nextDir)
                    : (agent.transform.position + nextDir * agent.GetDistance(nextAngle));
                var d = agent.transform.position + nextDir * maskRadius;

				QGL.DrawTriangle(a, b, d);
				QGL.DrawTriangle(a, d, c);
            }
        }
        public void Draw(float startAngle,float endAngle)
        {
            Draw(new QFovHitInfo { angle = startAngle }, new QFovHitInfo { angle = endAngle });
        }
        
        public void OnRenderObject()
        {
            if (agent == null) return;
			QGL.Start(mat,false);
			QFovHitInfo? lastInfo = null;
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
            else
            {
                Draw(0, 360);
            }
            QGL.End();
        }
    }
}
