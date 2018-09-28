using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Seiro.VoxelEffect
{

    /// <summary>
    /// 三角形
    /// </summary>
    public class Triangle : SATGeometry
    {

        public Vector3 a = VectorOperation.GetPointAroundAxis(90f, Vector3.forward);
        public Vector3 b = VectorOperation.GetPointAroundAxis(90f + 120f, Vector3.forward);
        public Vector3 c = VectorOperation.GetPointAroundAxis(90f + 240f, Vector3.forward);

        #region MonoBehaviour events

        public void OnDrawGizmos()
        {
            Vector3 ta, tb, tc;
            TransformPoints(out ta, out tb, out tc);

            Gizmos.DrawLine(ta, tb);
            Gizmos.DrawLine(tb, tc);
            Gizmos.DrawLine(tc, ta);

            var g = VectorOperation.GetGravity(ta, tb, tc);

            Gizmos.DrawLine(g, g + VectorOperation.GetNormal(ta, tb, tc));
        }

        #endregion

        #region Public functions

        public void TransformPoints(out Vector3 a, out Vector3 b, out Vector3 c)
        {
            a = transform.localToWorldMatrix.MultiplyPoint3x4(this.a);
            b = transform.localToWorldMatrix.MultiplyPoint3x4(this.b);
            c = transform.localToWorldMatrix.MultiplyPoint3x4(this.c);
        }

        public override Vector3[] GetSATNormals()
        {
            Vector3 ta, tb, tc;
            TransformPoints(out ta, out tb, out tc);
            return new Vector3[] { VectorOperation.GetNormal(ta, tb, tc) };
        }

        public override Vector3[] GetSATEdge()
        {
            Vector3 ta, tb, tc;
            TransformPoints(out ta, out tb, out tc);

            return new Vector3[]
            {
                tb - ta,
                tc - tb,
                ta - tc
            };
        }

        public override void CalcProjectionRange(Vector3 axis, out float min, out float max)
        {
            Vector3 ta, tb, tc;
            TransformPoints(out ta, out tb, out tc);

            var p0 = Vector3.Dot(ta, axis);
            var p1 = Vector3.Dot(tb, axis);
            var p2 = Vector3.Dot(tc, axis);

            min = Mathf.Min(p0, p1, p2);
            max = Mathf.Max(p0, p1, p2);
        }

        #endregion
    }
}