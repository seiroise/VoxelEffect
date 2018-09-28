using UnityEngine;

namespace Seiro.VoxelEffect
{

    /// <summary>
    /// AABBと三角形の交差判定
    /// </summary>
    public class AABBTriangleSolver : MonoBehaviour
    {

        [Header("AABB")]
        public Vector3 position;
        public Vector3 size;

        [Header("Triangle")]

        public Vector3 p1 = VectorOperation.GetPointAroundAxis(90f, Vector3.forward);
        public Vector3 p2 = VectorOperation.GetPointAroundAxis(90f + 120f, Vector3.forward);
        public Vector3 p3 = VectorOperation.GetPointAroundAxis(90f + 240f, Vector3.forward);

        #region MonoBehaviour events

        void Update()
        {

        }

        void OnDrawGizmos()
        {

        }

        #endregion

        #region Private functions

        bool Intersections(AABB aabb, Triangle tri)
        {
            return false;
        }

        #endregion
    }
}