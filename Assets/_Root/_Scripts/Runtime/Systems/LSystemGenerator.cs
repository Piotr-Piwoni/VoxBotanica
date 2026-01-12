using System;
using System.Collections.Generic;
using System.Linq;
using AYellowpaper.SerializedCollections;
using Sirenix.OdinInspector;
using UnityEngine;

namespace VoxBotanica.Systems
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

	[Title("Leaf Settings"), MinValue(0.1f),]
	public float LeafThickness = 2f;
	[MinValue(1)]
	public int LeafDensity = 3;

	[SerializeField]
	private SerializedDictionary<string, string> _Rules = new()
	{
			{ "F", "F[+F]F[-F]F" },
	};

	[Title("Visual Settings"), SerializeField,]
	private GameObject _VoxelPrefab;
	[SerializeField]
	private Color _BarkColour = new(0.451f, 0.325f, 0.165f); //< Brown.
	[SerializeField]
	private Color _LeafColour = Color.green;

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
		if (!_VoxelPrefab || _Voxels.Count == 0 || _BranchVoxels.Count == 0)
		{
			Debug.LogWarning("No prefab, no trunk voxels, or no branch voxels.");
			return;
		}

		List<int> trunkHeights = _Voxels.Select(v => v.y).Distinct()
										.OrderBy(y => y).ToList();
		int maxY = trunkHeights.Max();

		HashSet<Vector3Int> usedBranchVoxels = new();

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

			// Find the branch starting nodes around the trunk.
			foreach (Vector3Int offset in new Vector3Int[]
					 {
							 new(-1, 0, 0),
							 new(1, 0, 0),
							 new(0, 0, -1),
							 new(0, 0, 1),
					 })
			{
				Vector3Int start = trunkVoxel + offset;
				if (!_BranchVoxels.Contains(start) ||
					usedBranchVoxels.Contains(start)) continue;

				// Flood-fill to get full branch.
				List<Vector3Int> fullBranch = new();
				Queue<Vector3Int> queue = new();
				queue.Enqueue(start);
				usedBranchVoxels.Add(start);

				while (queue.Count > 0)
				{
					Vector3Int current = queue.Dequeue();
					fullBranch.Add(current);

					// Check all 6 neighbours.
					foreach (Vector3Int nOffset in new Vector3Int[]
							 {
									 new(1, 0, 0), new(-1, 0, 0),
									 new(0, 1, 0), new(0, -1, 0),
									 new(0, 0, 1), new(0, 0, -1),
							 })
					{
						Vector3Int neighbour = current + nOffset;
						if (!_BranchVoxels.Contains(neighbour) ||
							usedBranchVoxels.Contains(neighbour)) continue;

						queue.Enqueue(neighbour);
						usedBranchVoxels.Add(neighbour);
					}
				}

				branches.Add(fullBranch);
			}

			// Group branches into clusters based on "LeafDensity" variable.
			var b = 0;
			while (b < branches.Count)
			{
				int count = Math.Min(LeafDensity, branches.Count - b);
				List<List<Vector3Int>> clusterBranches =
						branches.GetRange(b, count);

				List<Vector3Int> clusterVoxels =
						clusterBranches.SelectMany(x => x).ToList();

				// Calculate Bounds.
				float minX = clusterVoxels.Min(v => v.x);
				float maxX = clusterVoxels.Max(v => v.x);
				float minY = clusterVoxels.Min(v => v.y);
				float maxYCluster = clusterVoxels.Max(v => v.y);
				float minZ = clusterVoxels.Min(v => v.z);
				float maxZ = clusterVoxels.Max(v => v.z);

				Vector3 clusterPos = new((minX + maxX) / 2f,
										 (minY + maxYCluster) / 2f,
										 (minZ + maxZ) / 2f);

				float scaleX = Mathf.Max(1f, maxX - minX + 1);
				float scaleZ = Mathf.Max(1f, maxZ - minZ + 1);
				float scaleY = Mathf.Max(1f, maxYCluster - minY + 1) +
							   LeafThickness;

				GameObject leaf = Instantiate(_VoxelPrefab, clusterPos,
											  Quaternion.identity, transform);
				leaf.transform.localScale = new Vector3(scaleX, scaleY, scaleZ);
				leaf.GetComponent<MeshRenderer>().material.color = _LeafColour;

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
														current +
														(_Rules.ContainsKey(
																 c.ToString()) ?
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

		_Voxels.Clear();
		_BranchVoxels.Clear();

		var stack = new Stack<TurtleState>();
		Vector3 position = Vector3.zero;
		Vector3 direction = Vector3.up;
		const float LENGTH = 1f;

		var insideBranch = false;

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

					// Leave branch.
					insideBranch = stack.Count > 0;
				}

				break;
			}
			}
		}
	}

	private void GenerateVoxelSphere(Vector3 centre, int radius = 3)
	{
		int r2 = radius * radius;

		// Loop through a cubic volume and keep only points inside the sphere.
		for (int x = -radius; x <= radius; x++)
		for (int y = -radius; y <= radius; y++)
		for (int z = -radius; z <= radius; z++)
		{
			// Check if the voxel lies within the spherical radius.
			if (x * x + y * y + z * z > r2) continue;
			Vector3 pos = centre + new Vector3(x, y, z);
			GameObject obj = Instantiate(_VoxelPrefab, pos, Quaternion.identity,
										 transform);
			obj.GetComponent<MeshRenderer>().material.color = _LeafColour;
		}
	}

	[Button("Spawn Leaves V2")]
	private void GenLeavesV2()
	{
		List<Vector3Int> endPoints = GetBranchEndPositions();

		// Generate a sphere of leaves at for each branch end point.
		foreach (Vector3Int EndPoint in endPoints)
			GenerateVoxelSphere(EndPoint, (int)LeafThickness);
	}

	private List<Vector3Int> GetBranchEndPositions()
	{
		var branchEnds = new List<Vector3Int>();
		var stack = new Stack<TurtleState>();

		Vector3 pos = Vector3.zero;
		Vector3 dir = Vector3.up;

		foreach (char c in _CurrentString)
			switch (c)
			{
			case 'F':
				pos += dir;
				break;

			case '+':
				dir = Quaternion.Euler(0f, 0f, Angle) * dir;
				break;

			case '-':
				dir = Quaternion.Euler(0f, 0f, -Angle) * dir;
				break;

			case '[':
				stack.Push(new TurtleState(pos, dir));
				break;

			case ']':
				branchEnds.Add(Vector3Int.RoundToInt(pos));

				TurtleState restore = stack.Pop();
				pos = restore.Position;
				dir = restore.Direction;
				break;
			}

		return branchEnds;
	}


	private void SpawnVoxels()
	{
		if (!_VoxelPrefab)
		{
			Debug.LogWarning("No voxel prefab assigned!");
			return;
		}

		// Spawn trunk voxels.
		GameObject obj;
		foreach (Vector3Int voxelPos in _Voxels)
		{
			obj = Instantiate(_VoxelPrefab, voxelPos, Quaternion.identity,
							  transform);
			obj.GetComponent<MeshRenderer>().material.color = _BarkColour;
		}

		// Spawn branch voxels.
		foreach (Vector3Int voxelPos in _BranchVoxels)
		{
			obj = Instantiate(_VoxelPrefab, voxelPos, Quaternion.identity,
							  transform);
			obj.GetComponent<MeshRenderer>().material.color = _BarkColour;
		}
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
