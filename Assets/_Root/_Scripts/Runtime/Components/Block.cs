using System;
using System.Collections.Generic;
using UnityEngine;
using VoxBotanica.Systems;
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


	public Block(Vector3 offset, BlockType bType = 0)
	{
		Vector3 blockLocalPosition = offset;

		if (bType == BlockType.Air) return;

		// Create mesh.
		List<Quad> quads = new();
		Vector3Int intLocalPos = new((int)blockLocalPosition.x,
									 (int)blockLocalPosition.y,
									 (int)blockLocalPosition.z);
		if (!HasSolidNeighbour(intLocalPos.x, intLocalPos.y, intLocalPos.z + 1))
			quads.Add(new Quad(MeshUtils.BlockSide.Front, offset, bType));
		if (!HasSolidNeighbour(intLocalPos.x, intLocalPos.y, intLocalPos.z - 1))
			quads.Add(new Quad(MeshUtils.BlockSide.Back, offset, bType));
		if (!HasSolidNeighbour(intLocalPos.x - 1, intLocalPos.y, intLocalPos.z))
			quads.Add(new Quad(MeshUtils.BlockSide.Left, offset, bType));
		if (!HasSolidNeighbour(intLocalPos.x + 1, intLocalPos.y, intLocalPos.z))
			quads.Add(new Quad(MeshUtils.BlockSide.Right, offset, bType));
		if (!HasSolidNeighbour(intLocalPos.x, intLocalPos.y + 1, intLocalPos.z))
			quads.Add(new Quad(MeshUtils.BlockSide.Top, offset, bType));
		if (!HasSolidNeighbour(intLocalPos.x, intLocalPos.y - 1, intLocalPos.z))
			quads.Add(new Quad(MeshUtils.BlockSide.Bottom, offset, bType));

		if (quads.Count == 0) return;

		var sideMeshes = new Mesh[quads.Count];
		for (var i = 0; i < quads.Count; i++)
			sideMeshes[i] = quads[i].Mesh;

		// Merge meshes.
		Mesh = MeshUtils.MergeMeshes(sideMeshes);
		Mesh.name = "CubeMesh";
	}


	public bool HasSolidNeighbour(int x, int y, int z)
	{
		return HasSolidNeighbour(new Vector3Int(x, y, z));
	}

	public bool HasSolidNeighbour(Vector3Int position)
	{
		return CubeGen.BlockData.Contains(position);
	}
}
}
