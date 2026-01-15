using Sirenix.OdinInspector;
using UnityEngine;
using VoxBotanica.Components;
using VoxBotanica.Types;
using VoxBotanica.Utilities;

namespace VoxBotanica.Systems
{
[HideMonoScript]
public class CubeGen : SerializedMonoBehaviour
{
	[ShowInInspector, ReadOnly,]
	public static BlockData BlockData { get; private set; } = new();

	[SerializeField]
	private Vector3 _Position;
	[SerializeField]
	private Vector3Int _ChunkSize = new(3, 3, 3);
	[SerializeField]
	private MeshFilter _MeshFilter;
	[SerializeField]
	private MeshRenderer _Renderer;

	private void Awake()
	{
		_MeshFilter = GetComponent<MeshFilter>();
		_Renderer = GetComponent<MeshRenderer>();
	}

	[Button]
	private void Clear()
	{
		_MeshFilter.sharedMesh.Clear();
		BlockData.Clear();
	}

	[Button]
	private void CreateCube(Vector3Int? position = null)
	{
		Vector3Int intPos = position ?? new Vector3Int((int)_Position.x,
													   (int)_Position.y,
													   (int)_Position.z);

		// Create a cube at different position.
		if (BlockData.Contains(intPos)) return;
		BlockData.Add(intPos);

		// Create visuals.
		BlockData.ClearBlocks(); //< Update visuals.
		foreach (Vector3 pos in BlockData.Positions)
			BlockData.Add(new Block(pos, BlockData.Positions, BlockType.Dirt));

		// Combine all cube meshes.
		Mesh finalMesh = MeshUtils.MergeMeshes(BlockData.GetMeshes());
		_MeshFilter.sharedMesh = finalMesh;
	}

	[Button]
	private void GenerateChunk()
	{
		Clear();

		for (var z = 0; z < _ChunkSize.z; z++)
		for (var y = 0; y < _ChunkSize.y; y++)
		for (var x = 0; x < _ChunkSize.z; x++)
			CreateCube(new Vector3Int(x, y, z));
	}
}
}
