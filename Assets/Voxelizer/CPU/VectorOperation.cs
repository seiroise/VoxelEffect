using UnityEngine;

namespace Seiro.VoxelEffect
{
    public static class VectorOperation
    {
        public static Vector3 GetPointAroundAxis(float angle, Vector3 axis)
        {
            return Quaternion.AngleAxis(angle, axis) * Vector3.right;
        }

        public static Vector3 GetNormal(Vector3 p1, Vector3 p2, Vector3 p3)
        {
            return Vector3.Cross(p2 - p1, p3 - p2).normalized;
        }

        public static Vector3 GetGravity(Vector3 p1, Vector3 p2, Vector3 p3)
        {
            return (p1 + p2 + p3) * 0.33333f;
        }
    }
}