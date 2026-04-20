using System;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using Sirenix.Utilities;
using UnityEngine;
using UnityEngine.Serialization;
using VoxBotanica.Components;
using VoxBotanica.Types;
using VoxBotanica.Utilities;
using Random = UnityEngine.Random;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace VoxBotanica.Systems
{
using TurtleState = TurtleState<Vector3>;

/// <summary>
///     Final procedural tree generator built on an L-system and voxel-based rendering pipeline.
///     <para>
///         This system:
///         <list type="bullet">
///             <item>
///                 <description>Uses a Lindenmayer System to generate structural strings.</description>
///             </item>
///             <item>
///                 <description>Parses trunk and branch data separately for controlled growth.</description>
///             </item>
///             <item>
///                 <description>Generates voxel data for trunk, branches, and leaves.</description>
///             </item>
///             <item>
///                 <description>Merges voxel meshes into optimised submeshes for rendering.</description>
///             </item>
///             <item>
///                 <description>Supports dynamic updates to canopy and leaf distribution.</description>
///             </item>
///         </list>
///     </para>
///     <para>
///         The generator distinguishes between trunk and branches, allowing independent control
///         over thickness, direction, and leaf generation. Leaves are generated as spherical
///         clusters at branch endpoints and trunk tops.
///     </para>
///     <para>
///         Editor-only debug tools are included for visualising the L-system structure and branch endpoints.
///     </para>
/// </summary>
[HideMonoScript, RequireComponent(typeof(MeshFilter), typeof(MeshRenderer)),]
public class TreeGenerator : SerializedMonoBehaviour
{
	public event Action OnTotalCanopyValueChanged;

	private const int _LENGTH = 1;

	[FormerlySerializedAs("AngleXY"), Range(5f, 90f),
	 OnValueChanged(nameof(Generate)),]
	public float BranchTilt = 25f;
	[MinValue(0f), OnValueChanged(nameof(Generate)),]
	public float TrunkHeight = 3f;
	[MinValue(1f), OnValueChanged(nameof(Generate)),]
	public float MaxHeight = 20f;
	[FormerlySerializedAs("AngleXZ"), Range(0f, 360f),
	 OnValueChanged(nameof(Generate)),]
	public float BranchRadialPosition = 25f;
	[MinValue(1), OnValueChanged(nameof(Generate)),]
	public int TrunkThickness = 2;
	[OnValueChanged(nameof(Generate))]
	public TrunkThicknessShape TrunkShape = TrunkThicknessShape.Circular;
	[FormerlySerializedAs("_LSystem"), OdinSerialize, OnValueChanged(nameof(Generate)),]
	public LindenmayerSystem LSystem;
	[FormerlySerializedAs("_LeafRadius"), MinValue(0),]
	public int LeafRadius = 2;
	[FormerlySerializedAs("_TrunkLeafRadius"), MinValue(0),]
	public int TrunkLeafRadius = 2;
	[FormerlySerializedAs("_BranchesLeafRadii"), OdinSerialize, MinValue(0),]
	public List<int> BranchesLeafRadii = new();
	[FormerlySerializedAs("_TrunkColour"), ColorUsage(true, true),]
	public Color TrunkColour = Color.saddleBrown;
	[FormerlySerializedAs("_LeafColour"), ColorUsage(true, true),]
	public Color LeafColour = Color.oliveDrab;

	[SerializeField, ReadOnly,]
	private BlockData _TrunkData = new();
	[SerializeField, ReadOnly,]
	private BlockData _BranchData = new();
	[SerializeField, ReadOnly,]
	private BlockData _LeafData = new();
	[SerializeField, ReadOnly,]
	private MeshFilter _MeshFilter;
	[SerializeField, ReadOnly,]
	private MeshRenderer _Renderer;
	[SerializeField, DisplayAsString,]
	private string _TrunkString = string.Empty;
	[SerializeField, ReadOnly,]
	private List<string> _BranchStrings = new();
	[SerializeField, ReadOnly,]
	private Shader _ShaderResource;

	private List<List<Vector3Int>> _Branches = new();
	private int _OldLeafRadius;
	private int _OldTrunkLeafRadius;
	private List<int> _OldBranchesLeafRadii = new();


	private void Awake()
	{
		_BranchStrings.Capacity = 10;
		_MeshFilter = GetComponent<MeshFilter>();
		_Renderer = GetComponent<MeshRenderer>();
		_ShaderResource = Resources.Load<Material>("Lit").shader;
		_OldLeafRadius = LeafRadius;
		TrunkLeafRadius = LeafRadius;
		_OldTrunkLeafRadius = TrunkLeafRadius;
	}

	private void Start()
	{
		Clear();
		Generate();
	}

	private void Update()
	{
		if (!_Renderer.sharedMaterials.IsNullOrEmpty())
		{
			Material[] materials = _Renderer.sharedMaterials;
			materials[0].color = TrunkColour; //< Trunk.
			materials[1].color = TrunkColour; //< Branches.
			materials[2].color = LeafColour;  //< Leaves.
		}

		UpdateLeafGeneration();
	}


	[Button]
	public void Clear()
	{
		LSystem.Clear();
		_TrunkString = string.Empty;
		_BranchStrings.Clear();
		_BranchData.Clear();
		_TrunkData.Clear();
		_LeafData.Clear();
		if (_MeshFilter) _MeshFilter.sharedMesh.Clear();
		_Branches.Clear();
		BranchesLeafRadii.Clear();
		_OldBranchesLeafRadii.Clear();
	}

	/// <summary>
	///     Generates the full tree from the current configuration.
	///     <para>
	///         Executes L-system generation, parsing, voxel generation, leaf creation,
	///         and final mesh rendering.
	///     </para>
	/// </summary>
	[Button]
	public void Generate()
	{
		if (!_MeshFilter)
		{
			Debug.LogError("There's no valid Mesh Filter to use!");
			return;
		}

		Clear();
		LSystem.Generate();

		if (string.IsNullOrEmpty(LSystem.SententialForm)) return;
		ParseSystem();
		GenerateVoxels();
		UpdateVoxelData();
		GenerateLeaves();
		Render();
	}

	/// <summary>
	///     Adds trunk voxels at a given position based on trunk thickness and shape.
	/// </summary>
	/// <param name="position">World position for trunk voxel placement.</param>
	private void AddTrunkVoxels(Vector3 position)
	{
		Vector3Int center = Vector3Int.RoundToInt(position);

		int start = -(TrunkThickness / 2);
		int end = start + TrunkThickness - 1;

		for (int x = start; x <= end; x++)
		for (int z = start; z <= end; z++)
		{
			// Handle the maths differently for circular trunks.
			if (TrunkShape == TrunkThicknessShape.Circular && TrunkThickness > 1)
			{
				float radius = (TrunkThickness - 1) / 2f;
				if (Mathf.Pow(x, 2f) + Mathf.Pow(z, 2f) > Mathf.Pow(radius, 2f))
					continue;
			}

			_TrunkData.Add(center + new Vector3Int(x, 0, z));
		}
	}

	/// <summary>
	///     Generates a branch from a parsed L-system segment.
	/// </summary>
	/// <param name="sententialForm">Branch-specific L-system string.</param>
	/// <param name="trunkPosition">Starting position on the trunk.</param>
	/// <param name="direction">Initial growth direction.</param>
	private void GenerateBranch(string sententialForm,
			Vector3 trunkPosition,
			Vector3 direction)
	{
		if (string.IsNullOrEmpty(sententialForm)) return;

		Vector3 position = trunkPosition;
		Stack<TurtleState> stack = new();

		// Add the starting position.
		var branch = new List<Vector3Int> { Vector3Int.RoundToInt(position), };
		foreach (char c in sententialForm.TakeWhile(_ => !(position.y >= MaxHeight)))
			switch (c)
			{
			case 'F': //< "Place" a new block and move towards the target direction.
			{
				Vector3 next = position + direction * _LENGTH;
				branch.Add(Vector3Int.RoundToInt(next));
				position = next;
				break;
			}
			case '+': //< Tilt clockwise.
			{
				Quaternion rot = Quaternion.Euler(0f, 0f, BranchTilt);
				direction = rot * direction;
				break;
			}
			case '-': //< Tilt anti-clockwise.
			{
				Quaternion rot = Quaternion.Euler(0f, 0f, -BranchTilt);
				direction = rot * direction;
				break;
			}
			case '[': //< Start a new branch.
				stack.Push(new TurtleState(position, direction));
				break;
			case ']': //< End the current branch and return to the trunk.
				if (stack.Count > 0)
				{
					TurtleState state = stack.Pop();
					position = state.Position;
					direction = state.Direction;
				}

				break;
			}

		// Add the constructed branch and it associated leaf radius value.
		_Branches.Add(branch);
		BranchesLeafRadii.Add(LeafRadius);
	}

	/// <summary>
	///     Generates a spherical cluster of leaf voxels.
	/// </summary>
	/// <param name="center">Centre position of the cluster.</param>
	/// <param name="radius">Radius of the cluster.</param>
	private void GenerateCluster(Vector3Int center, int radius)
	{
		for (int x = -radius; x <= radius; x++)
		for (int y = -radius; y <= radius; y++)
		for (int z = -radius; z <= radius; z++)
		{
			Vector3Int offset = new(x, y, z);
			if (offset.sqrMagnitude > radius * radius) continue;

			Vector3Int leafPos = center + offset;
			if (leafPos.y < TrunkHeight) continue;

			// Avoid overwriting trunk and branch.
			if (_TrunkData.Contains(leafPos)) continue;
			if (_BranchData.Contains(leafPos)) continue;

			_LeafData.Add(leafPos);
		}
	}

	/// <summary>
	///     Generates all leaf voxels for trunk and branches.
	/// </summary>
	/// <remarks>
	///     Uses trunk top and branch endpoints as cluster origins.
	/// </remarks>
	private void GenerateLeaves()
	{
		_LeafData.Clear();

		// Generate for the top of the canopy.
		int maxY = _TrunkData.Positions.Last().y;
		foreach (Vector3Int position in _TrunkData.Positions
												  .Where(position => position.y == maxY))
			GenerateCluster(position, TrunkLeafRadius);

		// Generate for the branches.
		for (var i = 0; i < _Branches.Count; i++)
		{
			List<Vector3Int> branch = _Branches[i];
			if (branch.IsNullOrEmpty()) continue;
			GenerateCluster(branch.Last(), BranchesLeafRadii[i]);
		}

		// Create meshes for each leaf block.
		foreach (Vector3 pos in _LeafData.Positions)
			_LeafData.Add(new Block(pos, _LeafData.Positions, BlockType.Dirt));

		// Create a tracking list if it doesn't already exist or the size is outdated.
		if (_OldBranchesLeafRadii.IsNullOrEmpty() ||
			_OldBranchesLeafRadii.Count != BranchesLeafRadii.Count)
			_OldBranchesLeafRadii = new List<int>(BranchesLeafRadii);
	}

	/// <summary>
	///     Generates trunk and branch voxel positions from the parsed trunk string.
	/// </summary>
	private void GenerateVoxels()
	{
		if (string.IsNullOrEmpty(_TrunkString)) return;

		Vector3 position = Vector3.zero;
		Vector3 direction = Vector3.up.normalized;

		AddTrunkVoxels(position);
		_TrunkData.Add(Vector3Int.RoundToInt(position));

		var branchIndex = 0;
		foreach (char c in _TrunkString.TakeWhile(c => !(position.y >= MaxHeight)))
			switch (c)
			{
			case 'F': //< "Place" a new block and move towards the target direction.
			{
				// Compute next position along the current direction.
				Vector3 next = position + direction * _LENGTH;

				// Interpolate between position and next to place voxels on grid.
				int steps = Mathf.CeilToInt(_LENGTH);
				for (var i = 1; i <= steps; i++)
				{
					Vector3 newPos = Vector3.Lerp(position, next, i / (float)steps);
					AddTrunkVoxels(newPos);
				}

				position = next;
				break;
			}
			case 'B': //< Start generate a branch.
			{
				if (position.y < TrunkHeight) break;

				Vector3 trunkDir = direction.normalized;

				// Pick an arbitrary perpendicular axis
				Vector3 axis = Vector3.Cross(trunkDir, Vector3.up);
				if (axis == Vector3.zero)
					axis = Vector3.Cross(trunkDir, Vector3.forward);
				axis.Normalize();

				// Random rotation around trunk
				float randomAzimuth = Random.Range(0f, 360f);
				Quaternion rot = Quaternion.AngleAxis(randomAzimuth, axis);

				// Base XZ rotation
				Quaternion baseRot = Quaternion.Euler(0f, BranchRadialPosition, 0f);

				// Apply rotations
				Vector3 branchDirection = rot * baseRot * trunkDir;

				GenerateBranch(_BranchStrings[branchIndex], position, branchDirection);
				branchIndex++;
				break;
			}

			default:
				Debug.LogWarning($"Invalid symbol <color=yellow>{c}</color> " +
								 "was trying to be parsed!");
				break;
			}
	}

	/// <summary>
	///     Parses the L-system output into trunk and branch segments.
	/// </summary>
	/// <remarks>
	///     Replaces branch segments with a placeholder and stores them separately.
	/// </remarks>
	private void ParseSystem()
	{
		_TrunkString = string.Empty;
		_BranchStrings.Clear();

		const char BRANCH_SYMBOL = 'B';
		var depth = 0;
		var currentBranch = string.Empty;

		// Takes into account nested branches by doing a depth search.
		foreach (char c in LSystem.SententialForm)
		{
			switch (c)
			{
			case '[': //< Found a start of a branch.
			{
				if (depth == 0)
				{
					_TrunkString += BRANCH_SYMBOL;
					currentBranch = "[";
				}
				else currentBranch += c;

				depth++;
				continue;
			}
			case ']': //< Found the end of a branch.
			{
				depth--;
				currentBranch += ']';
				if (depth == 0) _BranchStrings.Add(currentBranch);
				continue;
			}
			}

			if (depth == 0) _TrunkString += c;
			else currentBranch += c;
		}
	}

	/// <summary>
	///     Builds and assigns the final mesh from trunk, branch, and leaf voxel data.
	/// </summary>
	private void Render()
	{
		_MeshFilter.sharedMesh.Clear();

		// Merge meshes individually.
		List<Mesh> meshes = new()
		{
				MeshUtils.MergeMeshes(_TrunkData.GetMeshes()),
				MeshUtils.MergeMeshes(_BranchData.GetMeshes()),
				MeshUtils.MergeMeshes(_LeafData.GetMeshes()),
		};

		// Create a new mesh for final output.
		Mesh finalMesh = MeshUtils.CreateSubMeshes(meshes);
		finalMesh.name = "Tree_Final";

		_MeshFilter.sharedMesh = finalMesh;

		// Add materials based on the order of the submeshes.
		if (!_Renderer.sharedMaterials.IsNullOrEmpty()) return;
		var trunkMaterial = new Material(_ShaderResource) { name = "TrunkMat", };
		var branchMaterial = new Material(_ShaderResource) { name = "BranchMat", };
		var leafMaterial = new Material(_ShaderResource) { name = "LeavesMat", };

		trunkMaterial.color = TrunkColour;
		branchMaterial.color = TrunkColour;
		leafMaterial.color = LeafColour;

		_Renderer.sharedMaterials = new[]
		{
				trunkMaterial,  //< Trunk Mesh.
				branchMaterial, //< Branches Mesh.
				leafMaterial,   //< Leaves Mesh.
		};
	}


	/// <summary>
	///     Updates leaf generation when canopy-related values change.
	/// </summary>
	/// <remarks>
	///     Ensures values remain valid and triggers regeneration when needed.
	/// </remarks>
	private void UpdateLeafGeneration()
	{
		var hasChanged = false;

		// Fist check if any leaf radius of a branch changed.
		int count = Mathf.Min(BranchesLeafRadii.Count, _OldBranchesLeafRadii.Count);
		for (var i = 0; i < count; i++)
		{
			int clamped = Mathf.Clamp(BranchesLeafRadii[i], 0, int.MaxValue);
			if (clamped == _OldBranchesLeafRadii[i]) continue;

			BranchesLeafRadii[i] = clamped;
			_OldBranchesLeafRadii[i] = clamped;

			hasChanged = true;
		}

		// Secondary, check if the top canopy radius has changed.
		if (_OldTrunkLeafRadius != TrunkLeafRadius)
		{
			TrunkLeafRadius = Mathf.Clamp(TrunkLeafRadius, 0, int.MaxValue);
			_OldTrunkLeafRadius = TrunkLeafRadius;
			hasChanged = true;
		}

		// Lastly, if the total canopy radius has changed, propagate that to
		// the other radii parameters.
		if (LeafRadius != _OldLeafRadius)
		{
			LeafRadius = Mathf.Clamp(LeafRadius, 0, int.MaxValue);
			_OldLeafRadius = LeafRadius;

			// Update Trunk Leaf Radius.
			TrunkLeafRadius = LeafRadius;
			_OldTrunkLeafRadius = TrunkLeafRadius;

			// Update individual branch leaf radii.
			for (var i = 0; i < BranchesLeafRadii.Count; i++)
				BranchesLeafRadii[i] = LeafRadius;

			hasChanged = true;
			OnTotalCanopyValueChanged?.Invoke();
		}

		if (!hasChanged) return;
		GenerateLeaves();
		Render();
	}

	/// <summary>
	///     Converts voxel position data into renderable block data.
	/// </summary>
	/// <remarks>
	///     Applies offsets and generates mesh data for trunk and branches.
	/// </remarks>
	private void UpdateVoxelData()
	{
		// Offset the branches based on the trunk thickness.
		foreach (List<Vector3Int> branch in _Branches)
		{
			Vector3Int startPoint = branch.First();
			Vector3Int endPoint = branch.Last();
			Vector3Int difference = endPoint - startPoint;

			int start = -(TrunkThickness / 2);
			int end = start + TrunkThickness - 1;

			// Dominant direction.
			if (Mathf.Abs(difference.x) > Mathf.Abs(difference.z)) //< X dominant.
			{
				// Figure out if positive or negative.
				int offsetX = difference.x > 0 ? end : start;

				for (var k = 0; k < branch.Count; k++)
				{
					Vector3Int point = branch[k];
					point.x += offsetX;
					branch[k] = point;
				}
			}

			{
				// Figure out if positive or negative.
				int offsetZ = difference.z > 0 ? end : start;

				for (var k = 0; k < branch.Count; k++)
				{
					Vector3Int point = branch[k];
					point.z += offsetZ;
					branch[k] = point;
				}
			}
		}

		// Update the "_BranchData" position.
		// Ignore the first intersecting branch block.
		foreach (List<Vector3Int> branch in _Branches)
			_BranchData.AddRange(_TrunkData.Contains(branch.First()) ?
										 branch.GetRange(1, branch.Count - 1) :
										 branch);

		// Create visuals.
		_TrunkData.ClearBlocks(); //< Update visuals.
		foreach (Vector3 pos in _TrunkData.Positions)
			_TrunkData.Add(new Block(pos, _TrunkData.Positions, BlockType.Dirt));

		_BranchData.ClearBlocks(); //< Update visuals.
		foreach (Vector3 pos in _BranchData.Positions)
			_BranchData.Add(new Block(pos, _BranchData.Positions, BlockType.Dirt));
	}

	public enum TrunkThicknessShape
	{
		Circular,
		Square,
	}

// For Debugging.
	#if UNITY_EDITOR
	[SerializeField]
	private Color _DebugTreeColour = Color.magenta;
	[SerializeField]
	private Color _DebugEndPointsColour = new(0.6f, 0.3f, 1f);
	[SerializeField, Range(1f, 5f),]
	private float _GizmosLineWidth = 4f;
	[SerializeField, Range(0.1f, 2f),]
	private float _GizmosEndPointRadius = 0.45f;
	[SerializeField]
	private bool _ShowGizmos;
	[SerializeField]
	private bool _ShowTree;
	[SerializeField]
	private bool _ShowEndPoints;


	private void OnDrawGizmos()
	{
		if (!_ShowGizmos || string.IsNullOrEmpty(LSystem.SententialForm)) return;

		RenderDebugTree();
		RenderBranchEndPoints();
	}

	private void RenderBranchEndPoints()
	{
		if (!_ShowEndPoints) return;
		Gizmos.color = _DebugEndPointsColour;

		IEnumerable<Vector3Int> endPoints = _Branches.Select(branch => branch.Last());
		foreach (Vector3Int point in endPoints)
			Gizmos.DrawSphere(point, _GizmosEndPointRadius);
	}

	private void RenderDebugTree()
	{
		if (!_ShowTree) return;

		var stack = new Stack<TurtleState>();
		Vector3 renderOffset = Vector3.back;
		Vector3 position = Vector3.zero + renderOffset;
		Vector3 direction = Vector3.up;

		Handles.color = _DebugTreeColour;

		foreach (char c in LSystem.SententialForm)
		{
			float height = position.y;

			if (height >= MaxHeight)
				break;

			switch (c)
			{
			case 'F':
			{
				Vector3 newPos = position + direction * _LENGTH;

				if (newPos.y <= MaxHeight)
					Handles.DrawAAPolyLine(_GizmosLineWidth, position,
										   position + direction * _LENGTH);

				position = newPos;
				break;
			}
			case '+':
				if (height >= TrunkHeight)
				{
					Quaternion rotZ = Quaternion.Euler(0f, 0f, BranchTilt);
					Quaternion rotX = Quaternion.Euler(BranchRadialPosition, 0f, 0f);
					direction = rotZ * rotX * direction;
				}

				break;
			case '-':
				if (height >= TrunkHeight)
				{
					Quaternion rotZ = Quaternion.Euler(0f, 0f, -BranchTilt);
					Quaternion rotX = Quaternion.Euler(-BranchRadialPosition, 0f, 0f);
					direction = rotZ * rotX * direction;
				}

				break;
			case '[':
				if (height >= TrunkHeight)
					stack.Push(new TurtleState(position, direction));
				break;
			case ']':
				if (height >= TrunkHeight && stack.Count > 0)
				{
					TurtleState state = stack.Pop();
					position = state.Position;
					direction = state.Direction;
				}

				break;
			}
		}
	}
	#endif
}
}
