using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Seiro.VoxelEffect
{

    /// <summary>
    /// SATで交差判定
    /// </summary>
    public class SATSolver : MonoBehaviour
    {

        public SATGeometry a;
        public SATGeometry b;

        void Update()
        {
            Debug.Log(Test(a, b) ? "交差" : "分離");
        }

        /// <summary>
        /// 交差している場合はtrueを返す
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        bool Test(SATGeometry a, SATGeometry b)
        {
            if (!(a && b)) return false;
            var aNormals = a.GetSATNormals();
            if (Test(aNormals, a, b)) return true;
            var bNormals = b.GetSATNormals();
            if (Test(bNormals, a, b)) return true;

            return false;
        }

        /// <summary>
        /// どれか1つでも交差している軸があればtrueを返す
        /// </summary>
        /// <param name="axes"></param>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        bool Test(Vector3[] axes, SATGeometry a, SATGeometry b)
        {
            for (int i = 0; i < axes.Length; ++i)
            {
                if (Test(axes[i], a, b)) return true;
            }
            return false;
        }

        /// <summary>
        /// 交差している場合はtrueを返す
        /// </summary>
        /// <param name="axis"></param>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        bool Test(Vector3 axis, SATGeometry a, SATGeometry b)
        {
            float aMin, aMax, bMin, bMax;
            a.CalcProjectionRange(axis, out aMin, out aMax);
            b.CalcProjectionRange(axis, out bMin, out bMax);

            return !(aMin > bMax || aMax < bMin);
        }
    }
}