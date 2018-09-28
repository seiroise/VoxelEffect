using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Seiro.VoxelEffect
{

    public class SkinnedMeshVoxelizer : MonoBehaviour
    {

        static readonly string BUF_VOXEL_KEY = "_VoxelBuffer";
        static readonly string BUF_VERT_KEY = "_VertBuffer";
        static readonly string BUF_TRI_KEY = "_TriBuffer";

        static readonly string V3_START_KEY = "_Start";

        static readonly string F_UNIT_KEY = "_Unit";
        static readonly string F_HUNIT_KEY = "_HUnit";

        static readonly string I_WIDTH_KEY = "_W";
        static readonly string I_HEIGHT_KEY = "_H";
        static readonly string I_DEPTH_KEY = "_D";

        static readonly string I_TRIANGLE_COUNT = "_TriangleCount";

        [Header("Voxelization")]
        [SerializeField]
        ComputeShader _voxelizer = null;
        [SerializeField, Range(2, 128)]
        int _resolution = 32;
        [SerializeField]
        int _bufferCount = 200000;
        [SerializeField]
        bool _surfaceOnly = true;
        [SerializeField]
        string _surfaceFrontKernelKey = "SurfaceFront";
        [SerializeField]
        string _surfaceBackKernelKey = "SurfaceBack";

        [Header("Skin")]
        [SerializeField]
        SkinnedMeshRenderer _skinnedMesh = null;

        [Header("Debug")]
        [SerializeField]
        bool _enableDebugDraw = true;
        [SerializeField]
        Color _debugVoxelColor = Color.green;

        bool _isReady = false;
        bool _isSettedUp = false;

        Kernel _surfFrontKer = null;
        Kernel _surfBackKer = null;
        ComputeBuffer _voxelBuffer = null;
        Voxel[] _voxels = null;

        int _triangleCount = 0;
        int _vertCount = 0;
        int _indicesCount = 0;
        ComputeBuffer _vertBuffer = null;
        ComputeBuffer _triBuffer = null;

        Mesh _meshForSample = null;
        Bounds _bounds;

        #region MonoBehaviour functions

        void Start()
        {
            _isReady = Setup();
            if (_isReady)
            {
                SampleSkinnedMesh(ref _meshForSample);
            }
        }

        void Update()
        {
            if (!_isReady) return;
            SampleSkinnedMesh(ref _meshForSample);
            Voxelize();
        }

        void OnDestroy()
        {
            ReleaseResources();
        }

        #endregion

        #region Public functions

        /// <summary>
        /// 前回のボクセライズ結果を格納してあるバッファを返す
        /// </summary>
        /// <returns></returns>
        public ComputeBuffer GetVoxelBuffer()
        {
            return _voxelBuffer;
        }

        #endregion

        #region Private functions

        /// <summary>
        /// 全体のセットアップ
        /// </summary>
        /// <returns></returns>
        bool Setup()
        {
            ReleaseResources();
            return SetupMesh(_skinnedMesh) && SetupKernel(_voxelizer);
        }

        /// <summary>
        /// メッシュの初期化
        /// </summary>
        /// <returns></returns>
        bool SetupMesh(SkinnedMeshRenderer skin)
        {
            if (skin == null || skin.sharedMesh == null) return false;

            var mesh = skin.sharedMesh;

            var vertices = mesh.vertices;
            var triangles = mesh.triangles;

            _vertCount = mesh.vertexCount;
            _indicesCount = triangles.Length;
            _triangleCount = triangles.Length / 3;

            _vertBuffer = new ComputeBuffer(_vertCount, Marshal.SizeOf(typeof(Vector3)));
            _triBuffer = new ComputeBuffer(_indicesCount, Marshal.SizeOf(typeof(int)));

            _voxels = new Voxel[_bufferCount];
            _voxelBuffer = new ComputeBuffer(_voxels.Length, Marshal.SizeOf(typeof(Voxel)));

            return true;
        }

        /// <summary>
        /// コンピュートシェーダの各種kernelの初期化
        /// </summary>
        /// <returns></returns>
        bool SetupKernel(ComputeShader compute)
        {
            if (compute == null)
            {
                return false;
            }

            return
                Kernel.TryGetKernel(compute, _surfaceFrontKernelKey, out _surfFrontKer) &&
                Kernel.TryGetKernel(compute, _surfaceBackKernelKey, out _surfBackKer);
        }

        /// <summary>
        /// 確保している各種リソースの解放
        /// </summary>
        void ReleaseResources()
        {
            if (_voxelBuffer != null)
            {
                _voxelBuffer.Release();
                _voxelBuffer = null;
            }
            if (_vertBuffer != null)
            {
                _vertBuffer.Release();
                _vertBuffer = null;
            }
            if (_triBuffer != null)
            {
                _triBuffer.Release();
                _triBuffer = null;
            }
        }

        /// <summary>
        /// ボクセル化
        /// </summary>
        void Voxelize()
        {
            Voxelize(_voxelizer, _meshForSample, _resolution, _surfFrontKer, _surfBackKer);
        }

        /// <summary>
        /// ボクセル化
        /// </summary>
        void Voxelize
        (
            ComputeShader voxelizer, Mesh mesh, int resolution,
            Kernel surfFrontKer, Kernel surfBackKer
        )
        {
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

            // ボクセル用のバッファの用意
            _voxelBuffer = new ComputeBuffer(
                w * h * d,
                Marshal.SizeOf(typeof(Voxel))
            );

            voxelizer.SetVector(V3_START_KEY, start);
            // voxelizer.SetVector("_End", end);
            // voxelizer.SetVector("_Size", size);
            voxelizer.SetFloat(F_UNIT_KEY, unit);
            voxelizer.SetFloat(F_HUNIT_KEY, hunit);
            // voxelizer.SetFloat("_InvUnit", 1f / unit);
            voxelizer.SetInt(I_WIDTH_KEY, w);
            voxelizer.SetInt(I_HEIGHT_KEY, h);
            voxelizer.SetInt(I_DEPTH_KEY, d);
            voxelizer.SetInt(I_TRIANGLE_COUNT, _triangleCount);

            voxelizer.SetBuffer(surfFrontKer, BUF_VOXEL_KEY, _voxelBuffer);
            voxelizer.SetBuffer(surfFrontKer, BUF_VERT_KEY, _vertBuffer);
            voxelizer.SetBuffer(surfFrontKer, BUF_TRI_KEY, _triBuffer);
            voxelizer.Dispatch(surfFrontKer, _triangleCount / surfFrontKer.threadX + 1, 1, 1);

            voxelizer.SetBuffer(surfBackKer, BUF_VOXEL_KEY, _voxelBuffer);
            voxelizer.SetBuffer(surfBackKer, BUF_VERT_KEY, _vertBuffer);
            voxelizer.SetBuffer(surfBackKer, BUF_TRI_KEY, _triBuffer);
            voxelizer.Dispatch(surfBackKer, _triangleCount / surfBackKer.threadX + 1, 1, 1);
        }

        /// <summary>
        /// SkinnedMeshをメッシュにサンプリングする
        /// </summary>
        void SampleSkinnedMesh(ref Mesh mesh)
        {
            _skinnedMesh.BakeMesh(mesh);
            _bounds.Encapsulate(mesh.bounds.min);
            _bounds.Encapsulate(mesh.bounds.max);

            var vertices = mesh.vertices;
            var triangles = mesh.triangles;

            _vertBuffer.SetData(vertices);
            _triBuffer.SetData(triangles);
        }

        #endregion
    }
}