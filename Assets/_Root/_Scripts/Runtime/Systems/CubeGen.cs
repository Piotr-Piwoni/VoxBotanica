using Sirenix.OdinInspector;
using UnityEngine;
using VoxBotanica.Components;
using VoxBotanica.Types;
using VoxBotanica.Utilities;

namespace VoxBotanica.Systems
{
/// <summary>
///     Prototype cube/voxel generator used to experiment with runtime mesh generation.
///     <para>
///         This system was created to test and validate block rendering logic imported from
///         the VoxelWorld project, where cube meshes are generated procedurally at runtime.
///     </para>
///     <para>
///         The system:
///         <list type="bullet">
///             <item>
///                 <description>Tracks voxel positions using a shared BlockData container.</description>
///             </item>
///             <item>
///                 <description>Creates individual cube blocks at given positions.</description>
///             </item>
///             <item>
///                 <description>Rebuilds and merges meshes into a single optimised mesh.</description>
///             </item>
///             <item>
///                 <description>Supports generating simple voxel chunks for testing.</description>
///             </item>
///         </list>
///     </para>
///     <para>
///         This prototype was later integrated into the main tree generator for efficient voxel rendering.
///     </para>
/// </summary>
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

	/// <summary>
	///     Creates a cube at the specified position and rebuilds the combined mesh.
	///     <para>
	///         Prevents duplicate cubes and regenerates all block meshes before merging them
	///         into a single mesh for rendering.
	///     </para>
	/// </summary>
	/// <param name="position">Optional grid position for the cube; defaults to inspector position.</param>
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

	/// <summary>
	///     Generates a full chunk of cubes based on the configured chunk size.
	///     <para>
	///         Iterates over a 3D grid and creates cubes at each position.
	///     </para>
	/// </summary>
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
