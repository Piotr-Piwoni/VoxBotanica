using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using VoxBotanica.Components;

namespace VoxBotanica.Utilities
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

	public void AddRange(IEnumerable<Vector3Int> collection)
	{
		Positions.AddRange(collection);
	}

	public void AddRange(IEnumerable<Block> collection)
	{
		Blocks.AddRange(collection);
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

	public void Remove(Vector3Int position)
	{
		Positions.Remove(position);
	}

	public void Remove(Block block)
	{
		Blocks.Remove(block);
	}
}
}
