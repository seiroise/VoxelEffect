using System.Runtime.InteropServices;
using UnityEngine;

namespace Seiro.VoxelEffect
{

    public class GPUVoxelParticleSystem : MonoBehaviour
    {

        [StructLayout(LayoutKind.Sequential)]
        public struct VoxelParticle
        {
            public Vector3 position;
            public Vector3 scale;
            public Vector3 velocity;
            public float lifeTime;
        }

        [Header("Voxelization")]
        public ComputeShader voxelizer = null;
        [Range(4, 64)]
        public int resolution = 32;

        [Header("Particle System")]
        public ComputeShader particleCompute = null;
        public float initLifeTime = 1f;
        public Vector3 initScale = Vector3.one;

        [Header("Rendering")]
        public SkinnedMeshRenderer skinnedMesh = null;
        public Material material = null;
        public Mesh instancingMesh = null;

        Mesh _mesh = null;
        Bounds _bounds;
        GPUVoxelizer.GPUVoxelData _voxelData = null;

        uint[] _args = { 0, 0, 0, 0, 0 };
        ComputeBuffer _argsBuffer = null;

        #region MonoBehaviour events

        void Start()
        {
            _mesh = new Mesh();

            SetupResources();
        }

        void OnDestroy()
        {
            ReleaseResources();
        }

        #endregion

        #region Private functions

        void SetupResources()
        {
            ReleaseResources();
            _argsBuffer = new ComputeBuffer(1, _args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
        }

        void ReleaseResources()
        {
            if (_argsBuffer != null)
            {
                _argsBuffer.Release();
                _argsBuffer = null;
            }
            if (_voxelData != null)
            {
                _voxelData.Dispose();
                _voxelData = null;
            }
        }

        void SampleMesh()
        {
            skinnedMesh.BakeMesh(_mesh);

            _bounds.Encapsulate(_mesh.bounds.min);
            _bounds.Encapsulate(_mesh.bounds.max);

            if (_voxelData != null)
            {
                _voxelData.Dispose();
                _voxelData = null;
            }
            _voxelData = GPUVoxelizer.Voxelize(voxelizer, _mesh, resolution);
        }

        void RenderVoxels()
        {

        }

        #endregion

    }
}