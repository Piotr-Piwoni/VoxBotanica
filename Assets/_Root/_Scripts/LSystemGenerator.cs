using System;
using System.Collections.Generic;
using System.Linq;
using AYellowpaper.SerializedCollections;
using UnityEngine;
using Sirenix.OdinInspector;

namespace VoxBotanica
{
public class LSystemGenerator : MonoBehaviour
{
	[Title("L-System Settings")]
	public string Axiom = "F";
	[MinValue(1)]
	public int Iterations = 4;
	[Range(5f, 90f)]
	public float Angle = 25f;
	[MinValue(0f)]
	public float TrunkHeight = 3f;
	[MinValue(0f)]
	public float MaxHeight = 20f;
	public HashSet<Vector3Int> Voxels = new();

	[SerializeField]
	private SerializedDictionary<string, string> _Rules = new()
	{
		{ "F", "F[+F]F[-F]F" }
	};
	[SerializeField]
	private GameObject _VoxelPrefab;

	private string _CurrentString;


	[Button]
	public void Generate()
	{
		Clear();

		_CurrentString = GenerateLSystem();
		Debug.Log(_CurrentString);

		GenerateVoxelsFromString();
		SpawnVoxels();
	}

	[Button]
	private void Clear()
	{
		_CurrentString = string.Empty;
		Voxels.Clear();
		// Clear existing voxels.
		foreach (Transform child in transform)
			Destroy(child.gameObject);
	}

	private void OnDrawGizmos()
	{
		if (string.IsNullOrEmpty(_CurrentString))
			return;

		var stack = new Stack<TurtleState>();
		Vector3 position = Vector3.zero;
		Vector3 direction = Vector3.up;
		const float LENGTH = 1f;

		Gizmos.color = Color.green;

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
				Vector3 newPos = position + direction * LENGTH;

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

	private void GenerateVoxelsFromString()
	{
		if (string.IsNullOrEmpty(_CurrentString))
			return;

		Voxels.Clear();
		var stack = new Stack<TurtleState>();
		Vector3 position = Vector3.zero;
		Vector3 direction = Vector3.up;
		const float LENGTH = 1f;

		Gizmos.color = Color.green;

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
				Vector3 newPos = position + direction * LENGTH;

				// Only draw if under allowed max height.
				if (newPos.y <= MaxHeight)
					Voxels.Add(Vector3Int.RoundToInt(newPos));

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

	private struct TurtleState
	{
		public TurtleState(Vector3 pos, Vector3 dir)
		{
			Position = pos;
			Direction = dir;
		}

		public Vector3 Position;
		public Vector3 Direction;
	}

	private string GenerateLSystem()
	{
		string result = Axiom;

		for (var i = 0; i < Iterations; i++)
		{
			string newString = result.Aggregate(string.Empty, (current, c) =>
				current + (_Rules.ContainsKey(c.ToString())
					? _Rules[c.ToString()]
					: c.ToString()));

			result = newString;
		}

		return result;
	}

	private void SpawnVoxels()
	{
		if (_VoxelPrefab == null)
		{
			Debug.LogWarning("No voxel prefab assigned!");
			return;
		}

		// Clear existing voxels.
		foreach (Transform child in transform)
			DestroyImmediate(child.gameObject);

		// Spawn new voxels.
		foreach (Vector3Int voxelPos in Voxels)
			// Make them children for organization.
			Instantiate(_VoxelPrefab, voxelPos, Quaternion.identity, transform);
	}
}
}