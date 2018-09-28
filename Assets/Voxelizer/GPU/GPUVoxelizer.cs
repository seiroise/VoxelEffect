using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Seiro.VoxelEffect
{

    /// <summary>
    /// ComputeShaderを使用してGPU上でメッシュのボクせライズを行う。
    /// </summary>
    public class GPUVoxelizer
    {

        [StructLayout(LayoutKind.Sequential)]
        public struct Voxel
        {
            public Vector3 position;
            public uint fill;
            public uint front;
        }

        public class GPUVoxelData : System.IDisposable
        {
            public ComputeBuffer buffer { get; private set; }
            public int width { get; private set; }
            public int height { get; private set; }
            public int depth { get; private set; }
            public float unitLength { get; private set; }

            Voxel[] _cache = null;

            public GPUVoxelData(ComputeBuffer buffer, int w, int h, int d, float u)
            {
                this.buffer = buffer;
                this.width = w;
                this.height = h;
                this.depth = d;
                this.unitLength = u;
            }

            public Voxel[] GetData()
            {
                if (_cache == null)
                {
                    _cache = new Voxel[buffer.count];
                    buffer.GetData(_cache);
                }
                return _cache;
            }

            public void Dispose()
            {
                if (buffer != null)
                {
                    buffer.Release();
                    buffer = null;
                }
                _cache = null;
            }
        }

        public static GPUVoxelData Voxelize(ComputeShader voxelizer, Mesh mesh, int resolution)
        {
            // 1. ボクせライズ領域の定義
            mesh.RecalculateBounds();
            var bounds = mesh.bounds;
            var maxLength = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
            var unit = maxLength / resolution;
            var hunit = unit * 0.5f;
            var hunitVol = Vector3.one * hunit;
            var start = bounds.min - hunitVol;
            var end = bounds.max + hunitVol;
            var size = end - start;
            var w = Mathf.CeilToInt(size.x / unit);
            var h = Mathf.CeilToInt(size.y / unit);
            var d = Mathf.CeilToInt(size.z / unit);

            // 2. 各種データの用意
            var voxelBuffer = new ComputeBuffer
            (
                w * h * d,
                Marshal.SizeOf(typeof(Voxel))
            );
            var voxels = new Voxel[voxelBuffer.count];
            voxelBuffer.SetData(voxels);

            var vertices = mesh.vertices;
            var vertBuffer = new ComputeBuffer(vertices.Length, Marshal.SizeOf(typeof(Vector3)));
            vertBuffer.SetData(vertices);

            var triangles = mesh.triangles;
            var triBuffer = new ComputeBuffer(triangles.Length, Marshal.SizeOf(typeof(int)));
            triBuffer.SetData(triangles);

            // 3. 各種データをGPUにセット
            voxelizer.SetVector("_Start", start);
            // voxelizer.SetVector("_End", end);
            // voxelizer.SetVector("_Size", size);
            voxelizer.SetFloat("_Unit", unit);
            voxelizer.SetFloat("_HUnit", hunit);
            // voxelizer.SetFloat("_InvUnit", 1f / unit);
            voxelizer.SetInt("_W", w);
            voxelizer.SetInt("_H", h);
            voxelizer.SetInt("_D", d);
            var triangleCount = triangles.Length / 3;
            voxelizer.SetInt("_TriangleCount", triangleCount);

            // 4. カーネルの実行
            Kernel surfaceFrontKer;
            Kernel.TryGetKernel(voxelizer, "SurfaceFront", out surfaceFrontKer);
            voxelizer.SetBuffer(surfaceFrontKer, "_VoxelBuffer", voxelBuffer);
            voxelizer.SetBuffer(surfaceFrontKer, "_VertBuffer", vertBuffer);
            voxelizer.SetBuffer(surfaceFrontKer, "_TriBuffer", triBuffer);
            voxelizer.Dispatch(surfaceFrontKer, triangleCount / surfaceFrontKer.threadX + 1, 1, 1);

            Kernel surfaceBackKer;
            Kernel.TryGetKernel(voxelizer, "SurfaceBack", out surfaceBackKer);
            voxelizer.SetBuffer(surfaceBackKer, "_VoxelBuffer", voxelBuffer);
            voxelizer.SetBuffer(surfaceBackKer, "_VertBuffer", vertBuffer);
            voxelizer.SetBuffer(surfaceBackKer, "_TriBuffer", triBuffer);
            voxelizer.Dispatch(surfaceBackKer, triangleCount / surfaceBackKer.threadX + 1, 1, 1);

            // 5. 後始末
            vertices = null;
            triangles = null;
            vertBuffer.Release();
            triBuffer.Release();

            return new GPUVoxelData(voxelBuffer, w, h, d, unit);
        }
    }
}