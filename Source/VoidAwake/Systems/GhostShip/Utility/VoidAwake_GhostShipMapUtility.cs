using System.IO;
using UnityEngine;
using Verse;

namespace VoidAwake
{
	public static class VoidAwake_GhostShipMapUtility
	{
		public const string LayoutRelativePath = "Textures/Entities/Pirate/Ship/ShipMap.png";

		public static readonly Color32 ColorSea = new Color32(0x63, 0x9B, 0xFF, 0xFF);
		public static readonly Color32 ColorWood = new Color32(0x8F, 0x56, 0x3B, 0xFF);
		public static readonly Color32 ColorWall = new Color32(0x00, 0x00, 0x00, 0xFF);
		public static readonly Color32 ColorDoor = new Color32(0xFF, 0xFF, 0xFF, 0xFF);
		public static readonly Color32 ColorExit = new Color32(0xFB, 0xF2, 0x36, 0xFF);
		public static readonly Color32 ColorStairs = new Color32(0xAC, 0x32, 0x32, 0xFF);

		public static bool TryLoadLayout(out Color32[] pixels, out int width, out int height)
		{
			pixels = null;
			width = 0;
			height = 0;

			string path = FindLayoutPath();
			if (path == null)
			{
				Log.Error("[VoidAwake] Ghost ship layout PNG missing: " + LayoutRelativePath);
				return false;
			}

			byte[] bytes;
			try
			{
				bytes = File.ReadAllBytes(path);
			}
			catch (IOException e)
			{
				Log.Error("[VoidAwake] Failed to read ghost ship layout PNG: " + e);
				return false;
			}

			Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
			tex.filterMode = FilterMode.Point;
			if (!tex.LoadImage(bytes))
			{
				Object.Destroy(tex);
				Log.Error("[VoidAwake] Failed to decode ghost ship layout PNG.");
				return false;
			}

			width = tex.width;
			height = tex.height;
			pixels = tex.GetPixels32();
			Object.Destroy(tex);
			return pixels != null && width > 0 && height > 0;
		}

		public static IntVec3 MapSizeOrFallback(int fallback)
		{
			if (TryLoadLayout(out _, out int width, out int height))
			{
				return new IntVec3(width, 1, height);
			}

			return new IntVec3(fallback, 1, fallback);
		}

		public static bool Matches(Color32 a, Color32 b)
		{
			return a.r == b.r && a.g == b.g && a.b == b.b;
		}

		private static string FindLayoutPath()
		{
			foreach (ModContentPack pack in LoadedModManager.RunningModsListForReading)
			{
				string candidate = Path.Combine(pack.RootDir, LayoutRelativePath.Replace('/', Path.DirectorySeparatorChar));
				if (File.Exists(candidate))
				{
					return candidate;
				}
			}

			return null;
		}
	}
}
