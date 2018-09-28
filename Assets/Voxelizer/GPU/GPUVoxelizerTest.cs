using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Stopwatch = System.Diagnostics.Stopwatch;

namespace Seiro.VoxelEffect
{

    public class GPUVoxelizerTest : MonoBehaviour
    {

        public ComputeShader voxelizer;
        public Mesh source = null;
        [Range(4, 256)]
        public int resolution = 16;
        public bool surfaceOnly = false;

        public Color boundsColor = Color.blue;
        public Color extendBoundsColor = Color.cyan;
        public Color voxelColor = Color.green;
        public Color frontVoxelColor = Color.yellow;

        List<GPUVoxelizer.Voxel> _voxels = null;
        float _unit = 0f;
        Vector3 _unitVol = Vector3.zero;

        #region MonoBehaviour events

        void Start()
        {
            if (source)
            {
                var sw = Stopwatch.StartNew();
                var voxelData = GPUVoxelizer.Voxelize(voxelizer, source, resolution);
                sw.Stop();

                var volume = voxelData.GetData();

                _voxels = new List<GPUVoxelizer.Voxel>();
                for (int i = 0; i < volume.Length; ++i)
                {
                    if (volume[i].fill > 0)
                    {
                        _voxels.Add(volume[i]);
                    }
                }

                _unit = voxelData.unitLength;

                Debug.Log("Voxel count: " + _voxels.Count);
                Debug.Log("Elapsed time: " + sw.ElapsedMilliseconds + "(ms)");

                _unitVol = Vector3.one * _unit;

                voxelData.Dispose();
                voxelData = null;
                volume = null;
            }
        }

        void OnDestroy()
        {
            _voxels = null;
        }

        void OnDrawGizmos()
        {
            if (Application.isPlaying)
            {
                if (source != null)
                {
                    var bounds = source.bounds;
                    Gizmos.color = boundsColor;
                    Gizmos.DrawWireCube(bounds.center, bounds.size);

                    Gizmos.color = extendBoundsColor;
                    Gizmos.DrawWireCube(bounds.center, bounds.size + _unitVol);
                }

                if (_voxels != null)
                {
                    Gizmos.color = voxelColor;
                    for (int i = 0; i < _voxels.Count; ++i)
                    {
                        Gizmos.color = _voxels[i].front > 0 ? frontVoxelColor : voxelColor;
                        Gizmos.DrawWireCube(_voxels[i].position, _unitVol);
                    }
                }
            }
        }

        #endregion
    }
}