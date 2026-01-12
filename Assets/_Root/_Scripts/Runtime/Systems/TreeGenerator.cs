using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;
using VoxBotanica.Components;
using VoxBotanica.Types;
using VoxBotanica.Utilities;

namespace VoxBotanica.Systems
{
using TurtleStateInt = TurtleState<Vector3Int>;
using TurtleState = TurtleState<Vector3>;

[HideMonoScript, RequireComponent(typeof(MeshFilter), typeof(MeshRenderer)),]
public class TreeGenerator : SerializedMonoBehaviour
{
	private const int _LENGTH = 1;
	public string Axiom = "F";
	[MinValue(1)]
	public int Iterations = 3;
	[Range(5f, 90f)]
	public float Angle = 25f;
	[MinValue(0.1f)]
	public float TrunkHeight = 3f;
	[MinValue(1f)]
	public float MaxHeight = 20f;

	[SerializeField]
	private Dictionary<string, string> _Rules = new() { { "F", "F[+F]F[-F]F" }, };
	[SerializeField, DisplayAsString,]
	private string _CurrentString;
	[SerializeField, ReadOnly,]
	private BlockData _Data = new();
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
		_Data.Clear();
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
		_Data.ClearBlocks(); //< Update visuals.
		foreach (Vector3 pos in _Data.Positions)
			_Data.Add(new Block(pos, _Data.Positions, BlockType.Dirt));

		// Combine all cube meshes.
		Mesh finalMesh = MeshUtils.MergeMeshes(_Data.GetMeshes());
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

		_Data.Clear();

		Vector3Int position = Vector3Int.zero;
		Vector3Int direction = Vector3Int.up;

		_Data.Add(position);

		Stack<TurtleStateInt> stack = new();

		foreach (char c in _CurrentString)
		{
			float height = position.y;

			if (height >= MaxHeight)
				break;

			switch (c)
			{
			case 'F':
			{
				Vector3Int next = position + direction * _LENGTH;

				if (next.y <= MaxHeight)
				{
					position = next;
					_Data.Add(position);
				}

				break;
			}

			case '+':
			{
				if (height >= TrunkHeight)
					direction = RotateCw(direction);
				break;
			}

			case '-':
			{
				if (height >= TrunkHeight)
					direction = RotateCcw(direction);
				break;
			}

			case '[':
			{
				if (height >= TrunkHeight)
					stack.Push(new TurtleStateInt(position, direction));
				break;
			}

			case ']':
			{
				if (height >= TrunkHeight && stack.Count > 0)
				{
					TurtleStateInt state = stack.Pop();
					position = state.Position;
					direction = state.Direction;
				}

				break;
			}
			}
		}
	}

	private static Vector3Int RotateCcw(Vector3Int dir)
	{
		return new Vector3Int(-dir.y, dir.x, dir.z);
	}

	private static Vector3Int RotateCw(Vector3Int dir)
	{
		return new Vector3Int(dir.y, -dir.x, dir.z);
	}


	#if UNITY_EDITOR
	[SerializeField]
	private Color _GizmosColour = Color.magenta;

	private void OnDrawGizmos()
	{
		if (string.IsNullOrEmpty(_CurrentString))
			return;

		var stack = new Stack<TurtleState>();
		Vector3 position = Vector3.zero;
		Vector3 direction = Vector3.up;

		Gizmos.color = _GizmosColour;
		foreach (char c in _CurrentString)
		{
			float height = position.y;

			// Avoid growth beyond maximum height.
			if (height >= MaxHeight)
				break;

			switch (c)
			{
			case 'F':
			{
				Vector3 newPos = position + direction * _LENGTH;

				// Only draw if under allowed max height.
				if (newPos.y <= MaxHeight)
					Gizmos.DrawLine(position, newPos);

				position = newPos;
				break;
			}
			case '+':
			{
				// No rotation until trunk height reached.
				if (height >= TrunkHeight)
					direction = Quaternion.Euler(0f, 0f, Angle) * direction;
				break;
			}
			case '-':
			{
				// No rotation until trunk height reached.
				if (height >= TrunkHeight)
					direction = Quaternion.Euler(0f, 0f, -Angle) * direction;
				break;
			}
			case '[':
			{
				// Don’t store branches before trunk height.
				if (height >= TrunkHeight)
					stack.Push(new TurtleState(position, direction));
				break;
			}
			case ']':
			{
				if (!(height >= TrunkHeight) || stack.Count <= 0)
					break;

				TurtleState state = stack.Pop();
				position = state.Position;
				direction = state.Direction;
				break;
			}
			}
		}
	}
	#endif
}
}
