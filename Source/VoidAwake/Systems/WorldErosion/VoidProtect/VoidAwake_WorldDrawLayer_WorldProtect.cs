using System.Collections;
using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace VoidAwake
{
    public class VoidAwake_WorldDrawLayer_WorldProtect : WorldDrawLayer
    {
        private static Material mat;
        private readonly List<Vector3> tmpVerts = new List<Vector3>();

        private static Material Mat
        {
            get
            {
                if (mat == null)
                    mat = new Material(WorldMaterials.VertexColorTransparent);
                return mat;
            }
        }

        public override IEnumerable Regenerate()
        {
            foreach (object o in base.Regenerate()) yield return o;

            // OFFなら何も描かない
            if (VoidAwake_Mod.Settings == null || !VoidAwake_Mod.Settings.showGravshipTrail)
            {
                FinalizeMesh(MeshParts.All);
                yield break;
            }

            var path = Find.World?.GetComponent<VoidAwake_WorldComponent_VoidProtect>();
            if (path == null)
            {
                FinalizeMesh(MeshParts.All);
                yield break;
            }

            foreach (PlanetTile tile in path.PurifiedTiles)
                AddTile(tile);

            FinalizeMesh(MeshParts.All);
        }

        private void AddTile(PlanetTile tile)
        {
            LayerSubMesh sub = GetSubMesh(Mat);
            Find.WorldGrid.GetTileVertices(tile, tmpVerts);
            int start = sub.verts.Count;
            Vector3 lift = Find.WorldGrid.GetTileCenter(tile).normalized * 0.025f; // 浸食より少し上
            Color32 white = new Color32(255, 255, 255, 255);

            for (int i = 0; i < tmpVerts.Count; i++)
            {
                sub.verts.Add(tmpVerts[i] + lift);
                sub.colors.Add(white);
            }
            // 三角形は浸食レイヤと同じ巻き順（バニラ向き）
            for (int i = 0; i < tmpVerts.Count - 2; i++)
            {
                sub.tris.Add(start + i + 2);
                sub.tris.Add(start + i + 1);
                sub.tris.Add(start);
            }
        }
    }
}