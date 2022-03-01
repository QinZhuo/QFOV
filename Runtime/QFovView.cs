using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace QTool.FOV
{
    public class QFovView: MonoBehaviour
    {
        public Material mat;
        public QFovAgent agent;
        [Range(20,100)]
        public float maskRadius=30;
        public VertexInfo VI(Vector3 position)
        {
            var v= new VertexInfo
            {
                position = position
            };
          //  v.position.z = 0;
            return v;
        }
        public void OnRenderObject1()
        {
            QGL.Start(mat,0,false);
            HitInfo ? lastInfo = null;
            foreach (var hit in agent.hitInfoList)
            {
                if (lastInfo != null)
                {
                    QGL.DrawTriangle(
                    VI(transform.position),
                    VI(lastInfo.Value.point),
                    VI(hit.point));
                }
                lastInfo = hit;
            }
            QGL.End();
        }

        public void Draw(HitInfo last, HitInfo hit)
        {
            
            var lastMaskPoint =transform.position+ last.dir * maskRadius;
            var hitMaskPoint = transform.position + hit.dir * maskRadius;
            QGL.DrawTriangle(
                    VI(last.point),
                    VI(lastMaskPoint),
                    VI(hitMaskPoint));
            QGL.DrawTriangle(
                  VI(last.point),
                  VI(hitMaskPoint),
                  VI(hit.point));
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
            QGL.End();
        }
    }
}