using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VertexData = System.Tuple<UnityEngine.Vector3,
		UnityEngine.Vector3, UnityEngine.Vector2>;

// Copied from my other project VoxelWorld which was based off the web course
// "Create Minecraft-Inspired Voxel Worlds - Unity 6 Compatible" By Penny de Byl.

namespace VoxBotanica.Utilities
{
public static class MeshUtils
{
	public enum BlockSide
	{
		Front = 0,
		Back = 1,
		Left = 2,
		Right = 3,
		Top = 4,
		Bottom = 5,
	}

	public static Mesh MergeMeshes(Mesh[] meshes)
	{
		var mergedMesh = new Mesh();
		mergedMesh.name = "Mesh";

		Dictionary<VertexData, int> pointsOrder = new();
		HashSet<VertexData> pointsHash = new();
		List<int> triangles = new();

		var pointIndex = 0;
		for (var i = 0; i < meshes.Length; i++) //< Loop through each mesh.
		{
			if (!meshes[i]) continue;
			// Loop through each vertex in the current mesh.
			for (var j = 0; j < meshes[i].vertices.Length; j++)
			{
				Vector3 vertex = meshes[i].vertices[j];
				Vector3 normal = meshes[i].normals[j];
				Vector2 uv = meshes[i].uv[j];
				var vertexData = new VertexData(vertex, normal, uv);

				// Check if we came across the data before.
				if (pointsHash.Contains(vertexData)) continue;
				pointsOrder.Add(vertexData, pointIndex);
				pointsHash.Add(vertexData);
				pointIndex++;
			}

			// Loop through each triangle in the current mesh.
			foreach (int trianglePoint in meshes[i].triangles)
			{
				Vector3 vertex = meshes[i].vertices[trianglePoint];
				Vector3 normal = meshes[i].normals[trianglePoint];
				Vector2 uv = meshes[i].uv[trianglePoint];
				var vertexData = new VertexData(vertex, normal, uv);

				// Keep tack of the triangle position.
				pointsOrder.TryGetValue(vertexData, out int index);
				triangles.Add(index);
			}

			// Done with the mesh.
			meshes[i] = null;
		}

		// Contract mesh.
		ExtractArrays(pointsOrder, mergedMesh);
		mergedMesh.triangles = triangles.ToArray();

		mergedMesh.RecalculateBounds();
		mergedMesh.RecalculateTangents();

		return mergedMesh;
	}

	public static void ExtractArrays(Dictionary<VertexData, int> list, Mesh mesh)
	{
		List<Vector3> vertices = new();
		List<Vector3> normals = new();
		List<Vector2> uvs = new();

		// Extract information from the dictionary.
		foreach (VertexData data in list.Keys)
		{
			vertices.Add(data.Item1);
			normals.Add(data.Item2);
			uvs.Add(data.Item3);
		}

		// Construct mesh.
		mesh.vertices = vertices.ToArray();
		mesh.normals = normals.ToArray();
		mesh.uv = uvs.ToArray();
	}

	// Added after the import. This is from project VoxBotanica.
	public static Mesh CreateSubMeshes(List<Mesh> subMeshes)
	{
		Mesh finalMesh = new();

		// Calculate total vertex count.
		int totalVertexCount = subMeshes.Sum(t => t.vertexCount);

		// Allocate combined buffers.
		var vertices = new Vector3[totalVertexCount];
		var normals = new Vector3[totalVertexCount];
		var uvs = new Vector2[totalVertexCount];

		// Copy vertex data with cumulative offset.
		var vertexOffset = 0;
		foreach (Mesh mesh in subMeshes)
		{
			mesh.vertices.CopyTo(vertices, vertexOffset);

			if (mesh.normals != null && mesh.normals.Length == mesh.vertexCount)
				mesh.normals.CopyTo(normals, vertexOffset);

			if (mesh.uv != null && mesh.uv.Length == mesh.vertexCount)
				mesh.uv.CopyTo(uvs, vertexOffset);

			vertexOffset += mesh.vertexCount;
		}

		finalMesh.vertices = vertices;
		finalMesh.normals = normals;
		finalMesh.uv = uvs;

		// Combine triangles per submesh with proper offsets.
		finalMesh.subMeshCount = subMeshes.Count;
		vertexOffset = 0;
		for (var i = 0; i < subMeshes.Count; i++)
		{
			Mesh mesh = subMeshes[i];
			int[] triangles = mesh.triangles;

			var adjusted = new int[triangles.Length];
			for (var t = 0; t < triangles.Length; t++)
				adjusted[t] = triangles[t] + vertexOffset;

			finalMesh.SetTriangles(adjusted, i);

			vertexOffset += mesh.vertexCount;
		}

		// Final recalculation.
		finalMesh.RecalculateBounds();
		finalMesh.RecalculateTangents();

		return finalMesh;
	}
}
}
