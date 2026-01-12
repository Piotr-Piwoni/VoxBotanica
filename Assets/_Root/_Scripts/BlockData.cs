using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace VoxBotanica
{
[Serializable]
public class BlockData
{
	[ShowInInspector, ReadOnly,]
	public List<Block> Blocks { get; private set; } = new();
	[ShowInInspector, ReadOnly,]
	public List<Vector3Int> Positions { get; private set; } = new();


	public void Add(Vector3Int position)
	{
		Positions.Add(position);
	}

	public void Add(Block block)
	{
		Blocks.Add(block);
	}

	public void Clear()
	{
		Positions.Clear();
		Blocks.Clear();
	}

	public void ClearBlocks()
	{
		Blocks.Clear();
	}

	public void ClearPositions()
	{
		Positions.Clear();
	}

	public bool Contains(Vector3Int position)
	{
		return Positions.Contains(position);
	}

	public bool Contains(Block block)
	{
		return Blocks.Contains(block);
	}

	public Mesh[] GetMeshes()
	{
		var meshes = new Mesh[Blocks.Count];
		for (var i = 0; i < Blocks.Count; i++)
			meshes[i] = Blocks[i].Mesh;
		return meshes;
	}
}
}
