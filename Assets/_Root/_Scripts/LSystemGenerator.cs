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
	[MinValue(0.1f)]
	public float TrunkHeight = 3f;
	[MinValue(1f)]
	public float MaxHeight = 20f;

	[Title("Leaf Settings"), MinValue(0.1f)]
	public float LeafThickness = 2f;
	[MinValue(1)]
	public int LeafDensity = 3;

	[SerializeField]
	private SerializedDictionary<string, string> _Rules = new()
	{
		{ "F", "F[+F]F[-F]F" }
	};
	[SerializeField]
	private GameObject _VoxelPrefab;

	private string _CurrentString;
	private HashSet<Vector3Int> _Voxels = new();
	private HashSet<Vector3Int> _BranchVoxels = new();


	[Button]
	public void Generate()
	{
		Clear();

		_CurrentString = GenerateLSystem();
		Debug.Log(_CurrentString);

		GenerateVoxelsFromString();
		SpawnVoxels();
		//GenerateLeaves();
	}

	[Button]
	private void Clear()
	{
		_CurrentString = string.Empty;
		_Voxels.Clear();
		_BranchVoxels.Clear();
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

		_Voxels.Clear();
		_BranchVoxels.Clear();

		var stack = new Stack<TurtleState>();
		Vector3 position = Vector3.zero;
		Vector3 direction = Vector3.up;
		const float LENGTH = 1f;

		var insideBranch = false; // track when we are branching

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
				Vector3Int voxelPos = Vector3Int.RoundToInt(newPos);

				// Only draw if under allowed max height.
				if (newPos.y <= MaxHeight)
				{
					// Decide if this voxel is trunk or branch.
					if (insideBranch)
						_BranchVoxels.Add(voxelPos);
					else
						_Voxels.Add(voxelPos);
				}

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
				{
					stack.Push(new TurtleState(position, direction));
					insideBranch = true;
				}

				break;
			}
			case ']':
			{
				if (height >= TrunkHeight && stack.Count > 0)
				{
					TurtleState state = stack.Pop();
					position = state.Position;
					direction = state.Direction;

					// leaving branch
					insideBranch = stack.Count > 0;
				}

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

		foreach (Transform child in transform)
			DestroyImmediate(child.gameObject);

		// Spawn trunk voxels
		foreach (Vector3Int voxelPos in _Voxels)
			Instantiate(_VoxelPrefab, voxelPos, Quaternion.identity, transform);

		// Spawn branch voxels
		foreach (Vector3Int voxelPos in _BranchVoxels)
			Instantiate(_VoxelPrefab, voxelPos, Quaternion.identity, transform);
	}

	[Button("Spawn Leaves")]
	private void GenerateLeaves()
	{
		if (_VoxelPrefab == null || _BranchVoxels.Count == 0)
		{
			Debug.LogWarning("No leaf prefab or no branch voxels.");
			return;
		}

		List<int> branchHeights = _BranchVoxels.Select(v => v.y).Distinct()
			.OrderBy(y => y).ToList();
		if (branchHeights.Count < 2) return;

		float currentLayerScale = LeafThickness;
		var branchesInLayer = 0;

		foreach (int height in branchHeights)
		{
			List<Vector3Int> layerVoxels =
				_BranchVoxels.Where(v => v.y == height).ToList();
			branchesInLayer += layerVoxels.Count;

			if (branchesInLayer > LeafDensity)
			{
				currentLayerScale *= 0.8f; // taper
				branchesInLayer = layerVoxels.Count;
			}

			// Compute bounds of the layer
			float minX = layerVoxels.Min(v => v.x);
			float maxX = layerVoxels.Max(v => v.x);
			float minZ = layerVoxels.Min(v => v.z);
			float maxZ = layerVoxels.Max(v => v.z);

			var layerPos = new Vector3((minX + maxX) / 2f, height,
				(minZ + maxZ) / 2f);

			float scaleX = (maxX - minX + 1) * currentLayerScale;
			float scaleZ = (maxZ - minZ + 1) * currentLayerScale;
			float scaleY = currentLayerScale;

			GameObject leaf = Instantiate(_VoxelPrefab, layerPos,
				Quaternion.identity, transform);
			leaf.transform.localScale = new Vector3(scaleX, scaleY, scaleZ);
		}
	}
}
}