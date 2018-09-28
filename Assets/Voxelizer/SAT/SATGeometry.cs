using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Seiro.VoxelEffect
{

    /// <summary>
    /// SAT用幾何オブジェクト
    /// </summary>
    public class SATGeometry : MonoBehaviour
    {

        /// <summary>
        /// 分離軸テスト用の法線情報を返す
        /// </summary>
        /// <returns></returns>
        public virtual Vector3[] GetSATNormals() { return null; }

        /// <summary>
        /// 分離軸テスト用の辺情報を返す
        /// </summary>
        /// <returns></returns>
        public virtual Vector3[] GetSATEdge() { return null; }

        /// <summary>
        /// 指定した軸に射影したときの範囲を計算する
        /// </summary>
        /// <param name="axis"></param>
        /// <param name="min"></param>
        /// <param name="max"></param>
        public virtual void CalcProjectionRange(Vector3 axis, out float min, out float max) { min = max = 0f; }
    }
}