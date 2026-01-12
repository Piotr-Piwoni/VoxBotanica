using UnityEngine;

// Copied from my other project VoxelWorld which was based off the web course
// "Create Minecraft-Inspired Voxel Worlds - Unity 6 Compatible" By Penny de Byl.

namespace VoxBotanica.Types
{
// ──────────────────────────────────────────────────────────────
// Block Types
// ──────────────────────────────────────────────────────────────
public enum BlockType
{
	Air = 0,
	Dirt = 1,
	GrassSide = 2,
	GrassTop = 3,
	Sand = 4,
	Stone = 5,
	Water = 6,
}

public static class BlockUtils
{
// ──────────────────────────────────────────────────────────────
// Blocks UV Array
// ──────────────────────────────────────────────────────────────
	public static readonly Vector2[,] BlockUVs =
	{
			/* Dirt */
			{
					new(0.125f, 0.9375f), new(0.1875f, 0.9375f),
					new(0.125f, 1f), new(0.1875f, 1f),
			},
			/* GrassSide */
			{
					new(0.1875f, 0.9375f), new(0.25f, 0.9375f),
					new(0.1875f, 1f), new(0.25f, 1f),
			},
			/* GrassTop */
			{
					new(0.125f, 0.375f), new(0.1875f, 0.375f),
					new(0.125f, 0.4375f), new(0.1875f, 0.4375f),
			},
			/* Sand */
			{
					new(0.125f, 0.875f), new(0.1875f, 0.875f),
					new(0.125f, 0.9375f), new(0.1875f, 0.9375f),
			},
			/* Stone */
			{
					new(0.0625f, 0.9375f), new(0.125f, 0.9375f),
					new(0.0625f, 1f), new(0.125f, 1f),
			},
			/* Water */
			{
					new(0.8125f, 0.1875f), new(0.875f, 0.1875f),
					new(0.8125f, 0.25f), new(0.875f, 0.25f),
			},
	};
}
}
