using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Sirenix.Utilities;
using UnityEngine;
using VoxBotanica.Types;
using VoxBotanica.Utilities;

// Copied from my other project VoxelWorld which was based off the web course
// "Create Minecraft-Inspired Voxel Worlds - Unity 6 Compatible" By Penny de Byl.

namespace VoxBotanica.Components
{
[Serializable]
public class Block
{
	public Mesh Mesh { get; }


	public Block(Vector3 offset, [CanBeNull] List<Vector3Int> blocks, BlockType bType = 0)
	{
		if (bType == BlockType.Air) return;

		// Create mesh.
		List<Quad> quads = new();
		Vector3Int intLocalPos = new((int)offset.x, (int)offset.y, (int)offset.z);
		if (!HasSolidNeighbour(intLocalPos.x, intLocalPos.y, intLocalPos.z + 1, blocks))
			quads.Add(new Quad(MeshUtils.BlockSide.Front, offset, bType));
		if (!HasSolidNeighbour(intLocalPos.x, intLocalPos.y, intLocalPos.z - 1, blocks))
			quads.Add(new Quad(MeshUtils.BlockSide.Back, offset, bType));
		if (!HasSolidNeighbour(intLocalPos.x - 1, intLocalPos.y, intLocalPos.z, blocks))
			quads.Add(new Quad(MeshUtils.BlockSide.Left, offset, bType));
		if (!HasSolidNeighbour(intLocalPos.x + 1, intLocalPos.y, intLocalPos.z, blocks))
			quads.Add(new Quad(MeshUtils.BlockSide.Right, offset, bType));
		if (!HasSolidNeighbour(intLocalPos.x, intLocalPos.y + 1, intLocalPos.z, blocks))
			quads.Add(new Quad(MeshUtils.BlockSide.Top, offset, bType));
		if (!HasSolidNeighbour(intLocalPos.x, intLocalPos.y - 1, intLocalPos.z, blocks))
			quads.Add(new Quad(MeshUtils.BlockSide.Bottom, offset, bType));

		if (quads.Count == 0) return;

		var sideMeshes = new Mesh[quads.Count];
		for (var i = 0; i < quads.Count; i++)
			sideMeshes[i] = quads[i].Mesh;

		// Merge meshes.
		Mesh = MeshUtils.MergeMeshes(sideMeshes);
		Mesh.name = "CubeMesh";
	}


	public bool HasSolidNeighbour(int x, int y, int z, List<Vector3Int> blocks)
	{
		return HasSolidNeighbour(new Vector3Int(x, y, z), blocks);
	}

	public bool HasSolidNeighbour(Vector3Int position, List<Vector3Int> blocks)
	{
		if (!blocks.IsNullOrEmpty()) return blocks.Contains(position);

		Debug.LogWarning($"No {nameof(List<Vector3Int>)} provided to the " +
						 $"{nameof(HasSolidNeighbour)}() function!");
		return false;
	}
}
}
