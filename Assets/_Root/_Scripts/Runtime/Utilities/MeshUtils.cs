using System.Collections.Generic;
using UnityEngine;
using VertexData =
		System.Tuple<UnityEngine.Vector3, UnityEngine.Vector3, UnityEngine.Vector2>;


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

	public static void ExtractArrays(Dictionary<VertexData, int> list,
			Mesh mesh)
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

	// Fractal Brownian Motion.
	public static float fBM(float x,
			float z,
			int octaves = 1,
			float scale = 1f,
			float heightScale = 1f,
			float heightOffset = 0f,
			float frequency = 1f)
	{
		var total = 0f;
		for (var i = 0; i < octaves; i++)
		{
			total += Mathf.PerlinNoise(x * scale * frequency,
									   z * scale * frequency) * heightScale;
			frequency *= 2f;
		}

		return total + heightOffset;
	}
}
}
