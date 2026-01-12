using System.Linq;
using AYellowpaper.SerializedCollections;
using Sirenix.OdinInspector;
using UnityEngine;
using VoxBotanica.Utilities;

namespace VoxBotanica
{
public class CubeGen : MonoBehaviour
{
	[SerializeField]
	private Vector3 _Position;
	[SerializeField]
	private MeshFilter _MeshFilter;
	[SerializeField]
	private MeshRenderer _Renderer;
	[SerializeField, ReadOnly,]
	private SerializedDictionary<Vector3, Block> _Blocks = new();

	private void Awake()
	{
		_MeshFilter = GetComponent<MeshFilter>();
		_Renderer = GetComponent<MeshRenderer>();

		CreateCube();
	}

	[Button]
	private void Clear()
	{
		_MeshFilter.sharedMesh.Clear();
		_Blocks.Clear();
	}

	[Button]
	private void CreateCube()
	{
		// Create a cube at different position.
		if (_Blocks.ContainsKey(_Position)) return;
		var cube = new Block(_Position, BlockType.Dirt);
		_Blocks.Add(_Position, cube);

		// Combine all cube meshes.
		var meshes = new Mesh[_Blocks.Count];
		for (var i = 0; i < _Blocks.Count; i++)
			meshes[i] = _Blocks.ElementAt(i).Value.Mesh;

		Mesh finalMesh = MeshUtils.MergeMeshes(meshes);
		_MeshFilter.sharedMesh = finalMesh;
	}
}
}
