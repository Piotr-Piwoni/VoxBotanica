using System;
using System.Collections.Generic;
using System.Linq;
using AYellowpaper.SerializedCollections;
using Sirenix.OdinInspector;
using UnityEngine;

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

	private readonly HashSet<Vector3Int> _BranchVoxels = new();
	private readonly HashSet<Vector3Int> _Voxels = new();
	private string _CurrentString;

	private void Start()
	{
		Clear();
	}

	private void OnApplicationQuit()
	{
		Clear();
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

	[Button("Spawn Leaves")]
	private void GenerateLeaves()
	{
		if (_VoxelPrefab == null || _Voxels.Count == 0 ||
		    _BranchVoxels.Count == 0)
		{
			Debug.LogWarning(
				"No prefab, no trunk voxels, or no branch voxels.");
			return;
		}

		List<int> trunkHeights = _Voxels.Select(v => v.y).Distinct()
			.OrderBy(y => y).ToList();
		int maxY = trunkHeights.Max();

		HashSet<Vector3Int>
			usedBranchVoxels = new(); // to avoid double-counting

		var i = 0;
		while (i < trunkHeights.Count)
		{
			int y = trunkHeights[i];
			if (y < TrunkHeight)
			{
				i++;
				continue;
			}

			Vector3Int trunkVoxel = _Voxels.First(v => v.y == y);

			List<List<Vector3Int>> branches = new();

			// Find branch starting nodes around trunk
			foreach (Vector3Int offset in new Vector3Int[]
			         {
				         new(-1, 0, 0),
				         new(1, 0, 0),
				         new(0, 0, -1),
				         new(0, 0, 1)
			         })
			{
				Vector3Int start = trunkVoxel + offset;
				if (_BranchVoxels.Contains(start) &&
				    !usedBranchVoxels.Contains(start))
				{
					// BFS/flood-fill to get full branch
					List<Vector3Int> fullBranch = new();
					Queue<Vector3Int> queue = new();
					queue.Enqueue(start);
					usedBranchVoxels.Add(start);

					while (queue.Count > 0)
					{
						Vector3Int current = queue.Dequeue();
						fullBranch.Add(current);

						// Check all 6 neighbours
						foreach (Vector3Int nOffset in new Vector3Int[]
						         {
							         new(1, 0, 0), new(-1, 0, 0),
							         new(0, 1, 0), new(0, -1, 0),
							         new(0, 0, 1), new(0, 0, -1)
						         })
						{
							Vector3Int neighbour = current + nOffset;
							if (_BranchVoxels.Contains(neighbour) &&
							    !usedBranchVoxels.Contains(neighbour))
							{
								queue.Enqueue(neighbour);
								usedBranchVoxels.Add(neighbour);
							}
						}
					}

					branches.Add(fullBranch);
				}
			}

			// Group branches into clusters based on LeafDensity
			var b = 0;
			while (b < branches.Count)
			{
				int count = Math.Min(LeafDensity, branches.Count - b);
				List<List<Vector3Int>> clusterBranches =
					branches.GetRange(b, count);

				List<Vector3Int> clusterVoxels =
					clusterBranches.SelectMany(x => x).ToList();

				// Compute exact bounds
				float minX = clusterVoxels.Min(v => v.x);
				float maxX = clusterVoxels.Max(v => v.x);
				float minY = clusterVoxels.Min(v => v.y);
				float maxYCluster = clusterVoxels.Max(v => v.y);
				float minZ = clusterVoxels.Min(v => v.z);
				float maxZ = clusterVoxels.Max(v => v.z);

				Vector3 clusterPos = new(
					(minX + maxX) / 2f,
					(minY + maxYCluster) / 2f,
					(minZ + maxZ) / 2f
				);

				float scaleX = Mathf.Max(1f, maxX - minX + 1);
				float scaleZ = Mathf.Max(1f, maxZ - minZ + 1);
				float scaleY = Mathf.Max(1f, maxYCluster - minY + 1) +
				               LeafThickness;

				GameObject leaf = Instantiate(_VoxelPrefab, clusterPos,
					Quaternion.identity, transform);
				leaf.transform.localScale = new Vector3(scaleX, scaleY, scaleZ);

				b += count;
			}

			i++;
		}
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

	private struct TurtleState
	{
		public TurtleState(Vector3 pos, Vector3 dir)
		{
			Position = pos;
			Direction = dir;
		}

		public readonly Vector3 Position;
		public readonly Vector3 Direction;
	}
}
}