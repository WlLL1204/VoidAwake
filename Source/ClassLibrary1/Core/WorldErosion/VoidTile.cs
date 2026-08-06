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
        }
        /// <summary>半径が変わったときに WorldComponent から呼ぶ</summary>
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