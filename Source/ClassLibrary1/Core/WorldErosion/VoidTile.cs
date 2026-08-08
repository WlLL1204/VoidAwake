using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using static VoidAwake.VoidAwake_VoidErosion;

namespace VoidAwake
{
    public class VoidAwake_VoidTile : WorldDrawLayer
    {
        private static Material erosionMat;
        private readonly List<Vector3> tmpVerts = new List<Vector3>();
        private readonly List<Vector2> tmpUvs = new List<Vector2>();
        private static Material swirlLight;
        private static Material swirlMedium;
        private static Material swirlHeavy;
        private static Material swirlExtreme;

        private static Material ErosionMat
        {
            get
            {
                if (erosionMat == null)
                {
                    erosionMat = new Material(WorldMaterials.VertexColorTransparent);
                }
                return erosionMat;
            }
        }

        public override IEnumerable Regenerate()
        {
            foreach (object obj in base.Regenerate())
                yield return obj;
            var erosion = Find.World?.GetComponent<VoidAwake_VoidErosion>();
            if (erosion == null)
            {
                FinalizeMesh(MeshParts.All);
                yield break;
            }
            foreach (PlanetTile tile in erosion.ErodedTiles)
                AddTileMesh(tile, erosion);
            FinalizeMesh(MeshParts.All);
        }

        private void AddTileMesh(PlanetTile tile, VoidAwake_VoidErosion erosion)
        {
            VoidErosionLevel level = erosion.GetErosionLevel(tile);
            if (level == VoidErosionLevel.None)
                return;
            Color32 color = ColorFor(level);
            LayerSubMesh subMesh = GetSubMesh(ErosionMat);
            Find.WorldGrid.GetTileVertices(tile, tmpVerts);
            int start = subMesh.verts.Count;
            Vector3 center = Find.WorldGrid.GetTileCenter(tile);
            Vector3 lift = center.normalized * 0.012f;
            for (int i = 0; i < tmpVerts.Count; i++)
            {
                subMesh.verts.Add(tmpVerts[i] + lift);
                subMesh.colors.Add(color); 
            }
            for (int i = 0; i < tmpVerts.Count - 2; i++)
            {
                subMesh.tris.Add(start + i + 2);
                subMesh.tris.Add(start + i + 1);
                subMesh.tris.Add(start);
            }
            AddSwirlMesh(tile, erosion, level);

        }
        /// <summary>半径が変わったときに WorldComponent から呼ぶ</summary>
                // Atan2 の 0/1 継ぎ目をまたがないよう、三角形内の U を連続化
        private static void UnwrapUVs(ref Vector2 uv0, ref Vector2 uv1, ref Vector2 uv2)
        {
            float u0 = uv0.x, u1 = uv1.x, u2 = uv2.x;
            if (u1 - u0 > 0.5f) u1 -= 1f;
            if (u1 - u0 < -0.5f) u1 += 1f;
            if (u2 - u0 > 0.5f) u2 -= 1f;
            if (u2 - u0 < -0.5f) u2 += 1f;
            // u1 基準でもう一度 u2（稀なケース）
            if (u2 - u1 > 0.5f) u2 -= 1f;
            if (u2 - u1 < -0.5f) u2 += 1f;
            uv0.x = u0;
            uv1.x = u1;
            uv2.x = u2;
        }

        //テクスチャマテリアルの初回の作製
        private static void EnsureSwirlMats()
        {
            if (swirlLight != null) return;
            swirlLight = MaterialPool.MatFrom(
                "World/VoidErosion/Swirl",
                ShaderDatabase.WorldOverlayTransparent,
                new Color(1f, 1f, 1f, 0.55f),
                3510);
            swirlMedium = MaterialPool.MatFrom(
                "World/VoidErosion/Swirl",
                ShaderDatabase.WorldOverlayTransparent,
                new Color(1f, 1f, 1f, 0.65f),
                3510);
            swirlHeavy = MaterialPool.MatFrom(
                "World/VoidErosion/Swirl",
                ShaderDatabase.WorldOverlayTransparent,
                new Color(0.85f, 0.75f, 1f, 0.75f),
                3510);
            swirlExtreme = MaterialPool.MatFrom(
                "World/VoidErosion/Swirl",
                ShaderDatabase.WorldOverlayTransparent,
                new Color(0.6f, 0.6f, 0.6f, 0.85f),
                3510);
        }

        //渦マテリアルの浸食レベルによる切り替え
        private static Material SwirlMatFor(VoidErosionLevel level)
        {
            EnsureSwirlMats();
            switch (level)
            {
                case VoidErosionLevel.Extreme: return swirlExtreme;
                case VoidErosionLevel.Heavy: return swirlHeavy;
                case VoidErosionLevel.Medium: return swirlMedium;
                default: return swirlLight;
            }
        }

        private void AddSwirlMesh(PlanetTile tile, VoidAwake_VoidErosion erosion, VoidErosionLevel level)
        {
            if (!erosion.originTile.Valid) return;

            Material mat = SwirlMatFor(level);
            LayerSubMesh subMesh = GetSubMesh(mat);
            Find.WorldGrid.GetTileVertices(tile, tmpVerts);
            if (tmpVerts.Count < 3) return;

            Vector3 origin = Find.WorldGrid.GetTileCenter(erosion.originTile).normalized;
            Vector3 north = Vector3.ProjectOnPlane(Vector3.up, origin);
            if (north.sqrMagnitude < 0.001f)
                north = Vector3.ProjectOnPlane(Vector3.right, origin);
            north.Normalize();
            Vector3 east = Vector3.Cross(origin, north).normalized;

            float tileWorld = Find.WorldGrid.AverageTileSize;
            float worldRadius = Mathf.Max(erosion.radiusInTiles * tileWorld, 0.01f);
            float inv = 0.5f / worldRadius;

            Vector3 center = Find.WorldGrid.GetTileCenter(tile);
            Vector3 lift = center.normalized * 0.015f;
            float planetRadius = center.magnitude;
            Color32 white = new Color32(255, 255, 255, 255);

            // 位置と生UVを一時保持（まだメッシュに入れない）
            tmpUvs.Clear();
            List<Vector3> positions = new List<Vector3>(tmpVerts.Count);
            for (int i = 0; i < tmpVerts.Count; i++)
            {
                Vector3 p = tmpVerts[i] + lift;
                positions.Add(p);

                Vector3 d = Vector3.ProjectOnPlane(
                    p.normalized * planetRadius - origin * planetRadius,
                    origin);
                float x = Vector3.Dot(d, east);
                float y = Vector3.Dot(d, north);
                float radius = Mathf.Sqrt(x * x + y * y);
                float u = Mathf.Atan2(y, x) / (Mathf.PI * 2f);
                if (u < 0f) u += 1f;
                float v = radius * inv;
                tmpUvs.Add(new Vector2(u, v));
            }

            // 三角形ごとに U 継ぎ目を直してから追加
            for (int i = 0; i < positions.Count - 2; i++)
            {
                Vector2 uvA = tmpUvs[0];
                Vector2 uvB = tmpUvs[i + 1];
                Vector2 uvC = tmpUvs[i + 2];
                UnwrapUVs(ref uvA, ref uvB, ref uvC);

                int start = subMesh.verts.Count;
                subMesh.verts.Add(positions[0]);
                subMesh.verts.Add(positions[i + 1]);
                subMesh.verts.Add(positions[i + 2]);
                subMesh.uvs.Add(uvA);
                subMesh.uvs.Add(uvB);
                subMesh.uvs.Add(uvC);
                subMesh.colors.Add(white);
                subMesh.colors.Add(white);
                subMesh.colors.Add(white);

                subMesh.tris.Add(start + 2);
                subMesh.tris.Add(start + 1);
                subMesh.tris.Add(start);
            }
        }
        public static void UpdateSwirlScroll()
        {
            EnsureSwirlMats();
            float dt = Time.deltaTime;
            const float speed = 0.08f;
            swirlLight.mainTextureOffset += new Vector2(speed * dt, 0f);
            swirlMedium.mainTextureOffset += new Vector2(-speed * 1.2f * dt, 0f);
            swirlHeavy.mainTextureOffset += new Vector2(speed * 1.2f * dt, 0f);
            swirlExtreme.mainTextureOffset += new Vector2(-speed * 1.6f * dt, 0f);
        }
        public void NotifyErosionChanged()
        {
            SetDirty(); // 基底に無ければ RegenerateNow() / レイヤ dirty API を dnSpy で確認
        }

        //タイル毎の浸食率の描画
        private static Color32 ColorFor(VoidErosionLevel level)
        {
            switch (level)
            {
                case VoidErosionLevel.Extreme: return new Color32(0, 0, 0, 220);       // 黒
                case VoidErosionLevel.Heavy: return new Color32(60, 10, 90, 180);    // 濃い紫
                case VoidErosionLevel.Medium: return new Color32(120, 30, 170, 140);  // 紫
                case VoidErosionLevel.Light: return new Color32(160, 80, 200, 90);   // 薄い紫
                default: return new Color32(0, 0, 0, 0);
            }
        }


    }
}