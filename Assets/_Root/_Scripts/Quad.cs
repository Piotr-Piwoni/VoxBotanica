using UnityEngine;
using VoxBotanica.Utilities;

// Copied from my other project VoxelWorld which was based off the web course
// "Create Minecraft-Inspired Voxel Worlds - Unity 6 Compatible" By Penny de Byl.

namespace VoxBotanica
{
public class Quad
{
	public readonly Mesh Mesh;


	public Quad(MeshUtils.BlockSide side, Vector3 offset, BlockType bType)
	{
		Mesh = new Mesh();
		Mesh.name = "Quad";

		// Decrease the type to account for air.
		if (bType != BlockType.Air)
			bType--;

		var vertices = new Vector3[4];
		var normals = new Vector3[4];
		var uvs = new Vector2[4];
		var triangles = new[] { 3, 1, 0, 3, 2, 1, };

		// Setup default UVs.
		Vector2 uv00 = BlockUtils.BlockUVs[(int)bType, 0];
		Vector2 uv10 = BlockUtils.BlockUVs[(int)bType, 1];
		Vector2 uv01 = BlockUtils.BlockUVs[(int)bType, 2];
		Vector2 uv11 = BlockUtils.BlockUVs[(int)bType, 3];

		// Setup face vertices.
		Vector3 p0 = offset + new Vector3(-0.5f, -0.5f, 0.5f);
		Vector3 p1 = offset + new Vector3(0.5f, -0.5f, 0.5f);
		Vector3 p2 = offset + new Vector3(0.5f, -0.5f, -0.5f);
		Vector3 p3 = offset + new Vector3(-0.5f, -0.5f, -0.5f);
		Vector3 p4 = offset + new Vector3(-0.5f, 0.5f, 0.5f);
		Vector3 p5 = offset + new Vector3(0.5f, 0.5f, 0.5f);
		Vector3 p6 = offset + new Vector3(0.5f, 0.5f, -0.5f);
		Vector3 p7 = offset + new Vector3(-0.5f, 0.5f, -0.5f);

		// Setup vertices, normals, and UVs based on the side to build.
		Vector3 nDir;
		switch (side)
		{
		case MeshUtils.BlockSide.Front:
			nDir = Vector3.forward;
			vertices = new[] { p4, p5, p1, p0, };
			normals = new[] { nDir, nDir, nDir, nDir, };
			uvs = new[] { uv11, uv01, uv00, uv10, };
			break;
		case MeshUtils.BlockSide.Back:
			nDir = Vector3.back;
			vertices = new[] { p6, p7, p3, p2, };
			normals = new[] { nDir, nDir, nDir, nDir, };
			uvs = new[] { uv11, uv01, uv00, uv10, };
			break;
		case MeshUtils.BlockSide.Left:
			nDir = Vector3.left;
			vertices = new[] { p7, p4, p0, p3, };
			normals = new[] { nDir, nDir, nDir, nDir, };
			uvs = new[] { uv11, uv01, uv00, uv10, };
			break;
		case MeshUtils.BlockSide.Right:
			nDir = Vector3.right;
			vertices = new[] { p5, p6, p2, p1, };
			normals = new[] { nDir, nDir, nDir, nDir, };
			uvs = new[] { uv11, uv01, uv00, uv10, };
			break;
		case MeshUtils.BlockSide.Top:
			nDir = Vector3.up;
			vertices = new[] { p7, p6, p5, p4, };
			normals = new[] { nDir, nDir, nDir, nDir, };
			uvs = new[] { uv11, uv01, uv00, uv10, };
			break;
		case MeshUtils.BlockSide.Bottom:
			nDir = Vector3.down;
			vertices = new[] { p2, p3, p0, p1, };
			normals = new[] { nDir, nDir, nDir, nDir, };
			uvs = new[] { uv11, uv01, uv00, uv10, };
			break;
		}

		// Finish mesh.
		Mesh.vertices = vertices;
		Mesh.normals = normals;
		Mesh.triangles = triangles;
		Mesh.uv = uvs;

		Mesh.RecalculateBounds();
		Mesh.RecalculateTangents();
	}
}
}
