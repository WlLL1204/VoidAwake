using System.Collections;
using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

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
                    // 仮: 頂点色付き半透明（汚染の代替）
                    erosionMat = MaterialPool.MatFrom(
                        BaseContent.WhiteTex,
                        ShaderDatabase.WorldOverlayTransparent,
                        new Color(0.55f, 0.15f, 0.7f, 0.45f),
                        3510); // renderQueue は要調整
                    // Shader が無い場合は ShaderDatabase.Transparent 等を試す
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
                AddTileMesh(tile);

            FinalizeMesh(MeshParts.All);
        }
        private void AddTileMesh(PlanetTile tile)
        {
            LayerSubMesh subMesh = GetSubMesh(ErosionMat);
            Find.WorldGrid.GetTileVertices(tile, tmpVerts);

            int start = subMesh.verts.Count;
            // 少し外側に押し出して地形とZ-fightしないようにする
            Vector3 center = Find.WorldGrid.GetTileCenter(tile);
            Vector3 lift = center.normalized * 0.012f;

            for (int i = 0; i < tmpVerts.Count; i++)
            {
                subMesh.verts.Add(tmpVerts[i] + lift);
                subMesh.colors.Add(new Color32(140, 40, 180, 110));
            }

            // 扇状に三角形を張る（タイルは多角形）
            for (int i = 0; i < tmpVerts.Count - 2; i++)
            {
                subMesh.tris.Add(start);
                subMesh.tris.Add(start + i + 1);
                subMesh.tris.Add(start + i + 2);
            }
        }

        /// <summary>半径が変わったときに WorldComponent から呼ぶ</summary>
        public void NotifyErosionChanged()
        {
            SetDirty(); // 基底に無ければ RegenerateNow() / レイヤ dirty API を dnSpy で確認
        }
    }
}