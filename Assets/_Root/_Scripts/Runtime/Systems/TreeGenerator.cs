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
	[SerializeField, Range(0f, 360f),]
	private float AngleXZ = 25f;

	[SerializeField]
	private Dictionary<string, string> _Rules = new() { { "F", "F[+F]F[-F]F" }, };
	[SerializeField, DisplayAsString,]
	private string _CurrentString;
	[SerializeField, ReadOnly,]
	private BlockData _BodyData = new();
	[SerializeField]
	private MeshFilter _MeshFilter;

	private void Awake()
	{
		_MeshFilter = GetComponent<MeshFilter>();
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
		_BodyData.Clear();
		if (_MeshFilter.sharedMesh)
			_MeshFilter.sharedMesh.Clear();
	}

	[Button]
	public void Generate()
	{
		Clear();

		_CurrentString = GenerateLSystem();
		Debug.Log($"Current String: <color=yellow>{_CurrentString}</color>");

		GenerateVoxelsFromString();

		// Create visuals.
		_BodyData.ClearBlocks(); //< Update visuals.
		foreach (Vector3 pos in _BodyData.Positions)
			_BodyData.Add(new Block(pos, _BodyData.Positions, BlockType.Dirt));

		// Combine all cube meshes.
		Mesh finalMesh = MeshUtils.MergeMeshes(_BodyData.GetMeshes());
		_MeshFilter.sharedMesh = finalMesh;
	}

	private string GenerateLSystem()
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

		return result;
	}

	private void GenerateVoxelsFromString()
	{
		if (string.IsNullOrEmpty(_CurrentString))
			return;

		_BodyData.Clear();

		Vector3 position = Vector3.zero;
		Vector3 direction = Vector3.up.normalized;

		_BodyData.Add(Vector3Int.RoundToInt(position));

		// Stack to save branch states.
		Stack<TurtleState> stack = new();
		foreach (char c in _CurrentString)
		{
			float height = position.y;

			if (height >= MaxHeight)
				break;

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
					Vector3 p = Vector3.Lerp(position, next, i / (float)steps);
					_BodyData.Add(Vector3Int.RoundToInt(p));
				}

				position = next;
				break;
			}
			case '+':
			{
				if (height >= TrunkHeight)
				{
					Quaternion rotZ = Quaternion.Euler(0f, 0f, AngleXY);
					Quaternion rotX = Quaternion.Euler(AngleXZ, 0f, 0f);
					direction = rotZ * rotX * direction;
				}

				break;
			}
			case '-':
			{
				if (height >= TrunkHeight)
				{
					Quaternion rotZ = Quaternion.Euler(0f, 0f, -AngleXY);
					Quaternion rotX = Quaternion.Euler(-AngleXZ, 0f, 0f);
					direction = rotZ * rotX * direction;
				}

				break;
			}
			case '[':
			{
				if (height >= TrunkHeight)
					stack.Push(new TurtleState(position, direction));
				break;
			}
			case ']':
			{
				if (stack.Count > 0)
				{
					TurtleState state = stack.Pop();
					position = state.Position;
					direction = state.Direction;
				}

				break;
			}
			}
		}
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
