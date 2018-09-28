using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Seiro.VoxelEffect
{

    public class AABB : MonoBehaviour
    {
        public Vector3 size = Vector3.one;

        #region MonoBehaviour events

        void OnDrawGizmos()
        {
            Gizmos.DrawWireCube(transform.position, size);
        }

        #endregion

    }
}