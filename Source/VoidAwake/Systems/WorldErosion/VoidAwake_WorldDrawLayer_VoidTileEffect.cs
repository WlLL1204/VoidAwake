using System.Collections;
using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace VoidAwake
{
    /// <summary>浸食タイル上空の暗雲レイヤ（渦の上に描画）</summary>
    public class VoidAwake_WorldDrawLayer_VoidTileEffect : WorldDrawLayer
    {
        private const string CloudTexPath = "World/VoidErosion/Cloud";
        private const float CloudLift = 0.022f;

        private static Material cloudMat;
        private readonly List<Vector3> tmpVerts = new List<Vector3>();

        private static void EnsureCloudMat()
        {
            if (cloudMat != null) return;
            cloudMat = MaterialPool.MatFrom(
                CloudTexPath,
                ShaderDatabase.WorldOverlayTransparent,
                new Color(0.35f, 0.10f, 0.45f, 0.90f), 
                                         3520);
        }

        public static void UpdateCloudScroll()
        {
            EnsureCloudMat();
            float dt = Time.deltaTime;
            const float speed = 0.025f;
            cloudMat.mainTextureOffset += new Vector2(speed * dt, speed * 0.35f * dt);
        }

        public override IEnumerable Regenerate()
        {
            foreach (object o in base.Regenerate())
                yield return o;

            var erosion = Find.World?.GetComponent<VoidAwake_WorldComponent_VoidErosion>();
            if (erosion == null || !erosion.originTile.Valid)
            {
                FinalizeMesh(MeshParts.All);
                yield break;
            }

            EnsureCloudMat();

            Vector3 origin = Find.WorldGrid.GetTileCenter(erosion.originTile).normalized;
            Vector3 north = Vector3.ProjectOnPlane(Vector3.up, origin);
            if (north.sqrMagnitude < 0.001f)
                north = Vector3.ProjectOnPlane(Vector3.right, origin);
            north.Normalize();
            Vector3 east = Vector3.Cross(origin, north).normalized;

            float tileWorld = Find.WorldGrid.AverageTileSize;
            float worldRadius = Mathf.Max(erosion.radiusInTiles * tileWorld, 0.01f);
            // 雲は浸食円より少し広めにUVを取る（密度調整用）
            float inv = 1.2f / worldRadius;

            foreach (PlanetTile tile in erosion.ErodedTiles)
                AddCloudTile(tile, origin, east, north, inv);

            FinalizeMesh(MeshParts.All);
        }

        private void AddCloudTile(
            PlanetTile tile,
            Vector3 origin,
            Vector3 east,
            Vector3 north,
            float inv)
        {
            LayerSubMesh sub = GetSubMesh(cloudMat);
            Find.WorldGrid.GetTileVertices(tile, tmpVerts);
            if (tmpVerts.Count < 3) return;

            Vector3 center = Find.WorldGrid.GetTileCenter(tile);
            Vector3 lift = center.normalized * CloudLift;
            float planetRadius = center.magnitude;
            Color32 white = new Color32(255, 255, 255, 255);
            int start = sub.verts.Count;

            for (int i = 0; i < tmpVerts.Count; i++)
            {
                Vector3 p = tmpVerts[i] + lift;
                sub.verts.Add(p);

                // 直交UV（スライド＝雲の流れ）。渦の極座標とは別。
                Vector3 d = Vector3.ProjectOnPlane(
                    p.normalized * planetRadius - origin * planetRadius,
                    origin);
                float u = Vector3.Dot(d, east) * inv + 0.5f;
                float v = Vector3.Dot(d, north) * inv + 0.5f;
                sub.uvs.Add(new Vector2(u, v));
                sub.colors.Add(white);
            }

            for (int i = 0; i < tmpVerts.Count - 2; i++)
            {
                sub.tris.Add(start + i + 2);
                sub.tris.Add(start + i + 1);
                sub.tris.Add(start);
            }
        }
    }
}