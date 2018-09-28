using System.Collections.Generic;
using UnityEngine;

namespace Seiro.VoxelEffect
{

    /// <summary>
    /// CPUでのメッシュのボクせライズを行う。
    /// </summary>
    public class CPUVoxelizer
    {

        public struct Voxel
        {
            public Vector3 position;
            public bool fill;
            public bool front;
        }

        public class Triangle
        {
            public Vector3 a, b, c;
            public bool front;
            public Bounds bounds;

            public Triangle(Vector3 a, Vector3 b, Vector3 c, Vector3 frontDir)
            {
                this.a = a;
                this.b = b;
                this.c = c;

                var normal = Vector3.Cross(b - a, c - a);
                front = Vector3.Dot(normal, frontDir) <= 0f;

                var min = Vector3.Min(a, Vector3.Min(b, c));
                var max = Vector3.Max(a, Vector3.Max(b, c));
                bounds.SetMinMax(min, max);
            }
        }

        public static void Voxelize
        (
            Mesh mesh, int resolution, out List<Voxel> voxels, out float unit, bool surfaceOnly = false
        )
        {
            // 1. ボクせライズを行う範囲を定義する
            mesh.RecalculateBounds();
            var bounds = mesh.bounds;
            var maxLength = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
            unit = maxLength / resolution;
            var hunit = unit * 0.5f;
            var hunitVol = new Vector3(hunit, hunit, hunit);
            var start = bounds.min - hunitVol;
            var end = bounds.max + hunitVol;
            var size = end - start;

            var w = Mathf.CeilToInt(size.x / unit);
            var h = Mathf.CeilToInt(size.y / unit);
            var d = Mathf.CeilToInt(size.z / unit);

            // 2. ボクセルデータの生成
            var volume = new Voxel[w, h, d];
            var boxes = new Bounds[w, h, d];

            var unitVol = new Vector3(unit, unit, unit);
            var offset = bounds.min;
            // var offset = start;

            for (int x = 0; x < w; ++x)
            {
                for (int y = 0; y < h; ++y)
                {
                    for (int z = 0; z < d; ++z)
                    {
                        var p = new Vector3(x, y, z) * unit + offset;
                        boxes[x, y, z] = new Bounds(p, unitVol);
                    }
                }
            }

            // 3. メッシュ表面に位置するボクセルの検出
            var frontDir = Vector3.forward;
            var vertices = mesh.vertices;
            var indices = mesh.GetIndices(0);
            for (int i = 0; i < indices.Length; i += 3)
            {
                // 3-1. メッシュを構成するポリゴンから対象の三角形を抜き出す
                var a = vertices[indices[i]];
                var b = vertices[indices[i + 1]];
                var c = vertices[indices[i + 2]];
                var tri = new Triangle(a, b, c, frontDir);

                // 3-2. 三角形のと交差するボクセル番号の範囲を求める
                var min = tri.bounds.min - start;
                var max = tri.bounds.max - start;
                int iMinX = Mathf.FloorToInt(min.x / unit), iMinY = Mathf.FloorToInt(min.y / unit), iMinZ = Mathf.FloorToInt(min.z / unit);
                // int iMinX = Mathf.RoundToInt(min.x / unit), iMinY = Mathf.RoundToInt(min.y / unit), iMinZ = Mathf.RoundToInt(min.z / unit);
                int iMaxX = Mathf.FloorToInt(max.x / unit), iMaxY = Mathf.FloorToInt(max.y / unit), iMaxZ = Mathf.FloorToInt(max.z / unit);
                // int iMaxX = Mathf.RoundToInt(max.x / unit), iMaxY = Mathf.RoundToInt(max.y / unit), iMaxZ = Mathf.RoundToInt(max.z / unit);
                iMinX = Mathf.Clamp(iMinX, 0, w - 1);
                iMaxX = Mathf.Clamp(iMaxX, 0, w - 1);
                iMinY = Mathf.Clamp(iMinY, 0, h - 1);
                iMaxY = Mathf.Clamp(iMaxY, 0, h - 1);
                iMinZ = Mathf.Clamp(iMinZ, 0, d - 1);
                iMaxZ = Mathf.Clamp(iMaxZ, 0, d - 1);

                // 3-3. AABBと三角形の交差判定を行う。
                for (int x = iMinX; x <= iMaxX; ++x)
                {
                    for (int y = iMinY; y <= iMaxY; ++y)
                    {
                        for (int z = iMinZ; z <= iMaxZ; ++z)
                        {
                            if (Intersects(tri, boxes[x, y, z]))
                            {
                                var voxel = volume[x, y, z];
                                voxel.position = boxes[x, y, z].center;
                                if (!voxel.fill)
                                {
                                    voxel.front = tri.front;
                                }
                                else
                                {
                                    // ボクセルがすでにどれかしらの三角形と交差している場合は、
                                    // 背面フラグ(false)を優先する
                                    voxel.front = tri.front & voxel.front;
                                }
                                voxel.fill = true;
                                volume[x, y, z] = voxel;
                            }
                        }
                    }
                }
            }

            // 4. メッシュの内部を埋める
            if (!surfaceOnly)
            {
                for (int x = 0; x < w; ++x)
                {
                    for (int y = 0; y < h; ++y)
                    {
                        for (int z = 0; z < d; ++z)
                        {
                            var v = volume[x, y, z];
                            if (!v.fill || !v.front) continue;

                            // 前面に位置する中で一番奥のボクセルを探す
                            int iFront = z;
                            for (; iFront < d && volume[x, y, iFront].front; ++iFront) { }
                            if (iFront >= d) break;

                            // 背面に位置するボクセルを探す
                            int iBack = iFront;
                            for (; iBack < d && !volume[x, y, iBack].fill; ++iBack) { }
                            if (iBack >= d) break;

                            // iFrontからiBackまでを埋める
                            for (int i = iFront; i < iBack; ++i)
                            {
                                var t = volume[x, y, i];
                                t.position = boxes[x, y, i].center;
                                t.fill = true;
                                volume[x, y, i] = t;
                            }

                            z = iBack;
                        }
                    }
                }
            }

            // 5. 埋める必要のあるボクセルのリストを作成する
            voxels = new List<Voxel>();
            for (int x = 0; x < w; ++x)
            {
                for (int y = 0; y < h; ++y)
                {
                    for (int z = 0; z < d; ++z)
                    {
                        if (volume[x, y, z].fill)
                        {
                            voxels.Add(volume[x, y, z]);
                        }
                    }
                }
            }
        }

        public static bool Intersects(Triangle tri, Bounds aabb)
        {
            // AABBの中心が原点に鳴るように三角形を移動する。
            var center = aabb.center;
            var extents = aabb.extents;

            var v0 = tri.a - center;
            var v1 = tri.b - center;
            var v2 = tri.c - center;

            var f0 = v1 - v0;
            var f1 = v2 - v1;
            var f2 = v0 - v2;

            // 三角形の各辺とAABBの各辺のクロス積を分離軸として分離軸テストを行う。
            // AABBの各辺はそれぞれ方向ベクトルなので、クロス積の計算は省略できる。
            var a00 = new Vector3(0f, -f0.z, f0.y);
            var a01 = new Vector3(0f, -f1.z, f1.y);
            var a02 = new Vector3(0f, -f2.z, f2.y);
            var a10 = new Vector3(f0.z, 0f, -f0.x);
            var a11 = new Vector3(f1.z, 0f, -f1.x);
            var a12 = new Vector3(f2.z, 0f, -f2.x);
            var a20 = new Vector3(-f0.y, f0.x, 0f);
            var a21 = new Vector3(-f1.y, f1.x, 0f);
            var a22 = new Vector3(-f2.y, f2.x, 0f);

            // どれか1つでも交差していない軸が見つかれば、
            // 三角形とAABBの間に分離直線が存在するということになる。
            if (
                !Intersects(v0, v1, v2, extents, a00) ||
                !Intersects(v0, v1, v2, extents, a01) ||
                !Intersects(v0, v1, v2, extents, a02) ||
                !Intersects(v0, v1, v2, extents, a10) ||
                !Intersects(v0, v1, v2, extents, a11) ||
                !Intersects(v0, v1, v2, extents, a12) ||
                !Intersects(v0, v1, v2, extents, a20) ||
                !Intersects(v0, v1, v2, extents, a21) ||
                !Intersects(v0, v1, v2, extents, a22)
            )
            {
                return false;
            }

            // AABBの法線を分離軸として分離軸テストを行う。
            // AABBの法線はxyz軸に平行である(射影するとその軸の成分だけになる)ので、
            // 頂点のそれぞれの成分の最大値とextentsの正負の成分との大小を比較するだけで、分離軸テストを行える。
            if (
                Mathf.Min(v0.x, v1.x, v2.x) > extents.x ||
                Mathf.Max(v0.x, v1.x, v2.x) < -extents.x
            )
            {
                return false;
            }
            if (
                Mathf.Min(v0.y, v1.y, v2.y) > extents.y ||
                Mathf.Max(v0.y, v1.y, v2.y) < -extents.y
            )
            {
                return false;
            }
            if (
                Mathf.Min(v0.z, v1.z, v2.z) > extents.z ||
                Mathf.Max(v0.z, v1.z, v2.z) < -extents.z
            )
            {
                return false;
            }

            // 三角形の法線を分離軸として分離軸テストを行う。
            var normal = Vector3.Cross(f1, f0).normalized;
            var plane = new Plane(normal, Vector3.Dot(normal, tri.a));
            return Intersects(plane, aabb);
        }

        /// <summary>
        /// v0, v1, v2からなる三角形とextentsとの分離軸axisによる分離軸テストを行う。
        /// </summary>
        /// <param name="v0"></param>
        /// <param name="v1"></param>
        /// <param name="v2"></param>
        /// <param name="extents"></param>
        /// <param name="axis"></param>
        /// <returns></returns>
        public static bool Intersects(Vector3 v0, Vector3 v1, Vector3 v2, Vector3 extents, Vector3 axis)
        {
            // 分離軸上への射影を求める
            var p0 = Vector3.Dot(v0, axis);
            var p1 = Vector3.Dot(v1, axis);
            var p2 = Vector3.Dot(v2, axis);

            // AABBは原点に移動してある想定で処理している(centerを使用していない)ので
            // extentsをaxisに射影した[r]の[-r ~ r]が射影区間である。
            // それによってaabbのすべての頂点について射影する必要がなくなっている。
            var r = extents.x * Mathf.Abs(axis.x) + extents.y * Mathf.Abs(axis.y) + extents.z * Mathf.Abs(axis.z);

            // 射影区間の重なりを判定する。
            var minP = Mathf.Min(p0, p1, p2);
            var maxP = Mathf.Max(p0, p1, p2);
            return !(minP > r || maxP < -r);
        }

        /// <summary>
        /// 平面とAABBとの交差判定を行う。
        /// </summary>
        /// <param name="plane"></param>
        /// <param name="aabb"></param>
        /// <returns></returns>
        public static bool Intersects(Plane plane, Bounds aabb)
        {
            var center = aabb.center;
            var extents = aabb.extents;

            // planeのnormal上にextentsを射影する
            var r = extents.x * Mathf.Abs(plane.normal.x) + extents.y * Mathf.Abs(plane.normal.y) + extents.z * Mathf.Abs(plane.normal.z);

            // 射影区間の重なりを判定する
            var s = Vector3.Dot(plane.normal, center) - plane.distance;
            return Mathf.Abs(s) <= r;
        }
    }
}