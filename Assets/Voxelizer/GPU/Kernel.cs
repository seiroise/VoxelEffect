using UnityEngine;

namespace Seiro.VoxelEffect
{
    public class Kernel
    {
        public int index;
        public int threadX;
        public int threadY;
        public int threadZ;

        Kernel(ComputeShader shader, string key)
        {
            index = shader.FindKernel(key);
            if (index < 0)
            {
                Debug.LogWarningFormat("Can't find {0} kernel.", key);
                return;
            }
            uint x, y, z;
            shader.GetKernelThreadGroupSizes(index, out x, out y, out z);
            threadX = (int)x;
            threadY = (int)y;
            threadZ = (int)z;
        }

        public static implicit operator int(Kernel src)
        {
            return src.index;
        }

        public static implicit operator bool(Kernel src)
        {
            return src != null;
        }

        public static bool TryGetKernel(ComputeShader shader, string key, out Kernel dst)
        {
            dst = null;

            if (!shader)
            {
                return false;
            }

            var index = shader.FindKernel(key);
            if (index < 0)
            {
                return false;
            }

            dst = new Kernel(shader, key);
            return true;
        }
    }
}