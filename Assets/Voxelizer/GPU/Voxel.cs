using System.Runtime.InteropServices;
using UnityEngine;

namespace Seiro.VoxelEffect
{

    public struct Voxel
    {
        public Vector3 position;
        public uint fill;
        public uint front;
    }
}