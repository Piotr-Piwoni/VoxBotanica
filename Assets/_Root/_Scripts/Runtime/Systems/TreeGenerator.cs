using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;
using VoxBotanica.Components;
using VoxBotanica.Types;
using VoxBotanica.Utilities;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace VoxBotanica.Systems
{
using TurtleState = TurtleState<Vector3>;

[HideMonoScript, RequireComponent(typeof(MeshFilter), typeof(MeshRenderer)),]
public class TreeGenerator : SerializedMonoBehaviour
{
	private const int _LENGTH = 1;
	public string Axiom = "F";
	[MinValue(1)]
	public int Iterations = 3;
	[Range(5f, 90f)]
	public float AngleXY = 25f;
	[MinValue(0.1f)]
	public float TrunkHeight = 3f;
	[MinValue(1f)]
	public float MaxHeight = 20f;
	[Range(0f, 360f),]
	public float AngleXZ = 25f;
	[MinValue(1)]
	public int TrunkThickness = 2;
	public TrunkThicknessShape TrunkShape = TrunkThicknessShape.Circular;

	[SerializeField]
	private Dictionary<string, string> _Rules = new() { { "F", "F[+F]F[-F]F" }, };
	[SerializeField, DisplayAsString,]
	private string _CurrentString = string.Empty;
	[SerializeField, ReadOnly,]
	private BlockData _TrunkData = new();
	[SerializeField, ReadOnly,]
	private BlockData _BranchData = new();
	[SerializeField]
	private MeshFilter _MeshFilter;
	[SerializeField, DisplayAsString,]
	private string _TrunkString = string.Empty;
	[SerializeField, ReadOnly,]
	private List<string> _BranchStrings = new();

	private List<List<Vector3Int>> _Branches = new();


	private void Awake()
	{
		_MeshFilter = GetComponent<MeshFilter>();

		_BranchStrings.Capacity = 10;
	}

	private void Start()
	{
		Clear();
		Generate();
	}

	[Button]
	public void Clear()
	{
		_CurrentString = string.Empty;
		_TrunkString = string.Empty;
		_BranchStrings.Clear();
		_BranchData.Clear();
		_TrunkData.Clear();
		if (_MeshFilter.sharedMesh)
			_MeshFilter.sharedMesh.Clear();
		_Branches.Clear();
	}

	[Button]
	public void Generate()
	{
		Clear();

		GenerateLSystem();
		ParseSystem();
		GenerateVoxelsFromString();

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
			else //< Z dominant.
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
		foreach (Vector3Int point in _Branches.SelectMany(branch => branch))
			_BranchData.Add(point);

		// Create visuals.
		_TrunkData.ClearBlocks(); //< Update visuals.
		foreach (Vector3 pos in _TrunkData.Positions)
			_TrunkData.Add(new Block(pos, _TrunkData.Positions, BlockType.Dirt));

		_BranchData.ClearBlocks(); //< Update visuals.
		foreach (Vector3 pos in _BranchData.Positions)
			_BranchData.Add(new Block(pos, _BranchData.Positions, BlockType.Dirt));

		// Gather all meshes.
		List<Mesh> meshes = new();
		meshes.AddRange(_TrunkData.GetMeshes());
		meshes.AddRange(_BranchData.GetMeshes());

		// Combine all cube meshes.
		Mesh finalMesh = MeshUtils.MergeMeshes(meshes.ToArray());
		_MeshFilter.sharedMesh = finalMesh;
	}

	private void AddTrunkVoxels(Vector3 position)
	{
		Vector3Int center = Vector3Int.RoundToInt(position);

		int start = -(TrunkThickness / 2);
		int end = start + TrunkThickness - 1;

		for (int x = start; x <= end; x++)
		for (int z = start; z <= end; z++)
		{
			if (TrunkShape == TrunkThicknessShape.Circular && TrunkThickness > 1)
			{
				float radius = (TrunkThickness - 1) / 2f;
				if (Mathf.Pow(x, 2f) + Mathf.Pow(z, 2f) > Mathf.Pow(radius, 2f))
					continue;
			}

			_TrunkData.Add(center + new Vector3Int(x, 0, z));
		}
	}

	private void GenerateBranch(string sententialForm,
			Vector3 trunkPosition,
			Vector3 direction)
	{
		if (string.IsNullOrEmpty(sententialForm)) return;

		Vector3 position = trunkPosition;
		Stack<TurtleState> stack = new();

		// Add the starting position.
		var branch = new List<Vector3Int> { Vector3Int.RoundToInt(position), };
		foreach (char c in sententialForm.TakeWhile(c => !(position.y >= MaxHeight)))
			switch (c)
			{
			case 'F':
			{
				Vector3 next = position + direction * _LENGTH;
				branch.Add(Vector3Int.RoundToInt(next));
				position = next;
				break;
			}
			case '+':
			{
				Quaternion rotZ = Quaternion.Euler(0f, 0f, AngleXY);
				Quaternion rotX = Quaternion.Euler(AngleXZ, 0f, 0f);
				direction = rotZ * rotX * direction;
				break;
			}
			case '-':
			{
				Quaternion rotZ = Quaternion.Euler(0f, 0f, -AngleXY);
				Quaternion rotX = Quaternion.Euler(-AngleXZ, 0f, 0f);
				direction = rotZ * rotX * direction;
				break;
			}
			case '[':
				stack.Push(new TurtleState(position, direction));
				break;
			case ']':
				if (stack.Count > 0)
				{
					TurtleState state = stack.Pop();
					position = state.Position;
					direction = state.Direction;
				}

				break;
			}

		// Add the constructed branch.
		_Branches.Add(branch);
	}

	private void GenerateLSystem()
	{
		string result = Axiom;

		for (var i = 0; i < Iterations; i++)
		{
			string newString = result.Aggregate(string.Empty,
												(current, c) => current +
													(_Rules.ContainsKey(c.ToString()) ?
															 _Rules[c.ToString()] :
															 c.ToString()));
			result = newString;
		}

		_CurrentString = result;
		Debug.Log($"Current String: <color=yellow>{_CurrentString}</color>");
	}

	private void GenerateVoxelsFromString()
	{
		if (string.IsNullOrEmpty(_TrunkString))
			return;

		Vector3 position = Vector3.zero;
		Vector3 direction = Vector3.up.normalized;

		AddTrunkVoxels(position);
		_TrunkData.Add(Vector3Int.RoundToInt(position));

		var branchIndex = 0;
		foreach (char c in _TrunkString.TakeWhile(c => !(position.y >= MaxHeight)))
			switch (c)
			{
			case 'F':
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
			case '+':
			{
				Quaternion rotZ = Quaternion.Euler(0f, 0f, AngleXY);
				Quaternion rotX = Quaternion.Euler(AngleXZ, 0f, 0f);
				direction = rotZ * rotX * direction;
				break;
			}
			case '-':
			{
				Quaternion rotZ = Quaternion.Euler(0f, 0f, -AngleXY);
				Quaternion rotX = Quaternion.Euler(-AngleXZ, 0f, 0f);
				direction = rotZ * rotX * direction;
				break;
			}
			case 'B':
			{
				if (position.y < TrunkHeight) break;
				GenerateBranch(_BranchStrings[branchIndex], position, direction);
				branchIndex++;
				break;
			}

			default:
				Debug.LogWarning($"Invalid symbol <color=yellow>{c}</color> " +
								 "was trying to be parsed!");
				break;
			}
	}

	private void ParseSystem()
	{
		_TrunkString = string.Empty;
		_BranchStrings.Clear();

		const char BRANCH_SYMBOL = 'B';
		var depth = 0;
		var currentBranch = string.Empty;

		foreach (char c in _CurrentString)
		{
			switch (c)
			{
			case '[':
			{
				if (depth == 0)
				{
					_TrunkString += BRANCH_SYMBOL;
					currentBranch = "[";
				}
				else
					currentBranch += c;

				depth++;
				continue;
			}
			case ']':
			{
				depth--;
				currentBranch += ']';

				if (depth == 0)
					_BranchStrings.Add(currentBranch);

				continue;
			}
			}

			if (depth == 0)
				_TrunkString += c;
			else
				currentBranch += c;
		}
	}

	public enum TrunkThicknessShape
	{
		Circular,
		Square,
	}


	#if UNITY_EDITOR
	[SerializeField]
	private Color _GizmosColour = Color.magenta;
	[SerializeField, Range(1f, 5f),]
	private float _GizmosLineWidth = 4f;

	private void OnDrawGizmos()
	{
		if (string.IsNullOrEmpty(_CurrentString))
			return;

		var stack = new Stack<TurtleState>();
		Vector3 renderOffset = Vector3.back;
		Vector3 position = Vector3.zero + renderOffset;
		Vector3 direction = Vector3.up;

		Handles.color = _GizmosColour;

		foreach (char c in _CurrentString)
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
					Quaternion rotZ = Quaternion.Euler(0f, 0f, AngleXY);
					Quaternion rotX = Quaternion.Euler(AngleXZ, 0f, 0f);
					direction = rotZ * rotX * direction;
				}

				break;
			case '-':
				if (height >= TrunkHeight)
				{
					Quaternion rotZ = Quaternion.Euler(0f, 0f, -AngleXY);
					Quaternion rotX = Quaternion.Euler(-AngleXZ, 0f, 0f);
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

	private void OnValidate()
	{
		if (!_MeshFilter || _CurrentString == string.Empty) return;

		Generate();
	}
	#endif
}
}
