using System.IO;
using Autodesk.Fbx;
using Sirenix.OdinInspector;
using UnityEngine;

namespace VoxBotanica
{
public class ModelExporter : MonoBehaviour
{
	[SerializeField]
	private string _ExportFolderPath =
			@"W:\Projects\Engines\Unity\Abertay\[CMP400] - Honours Project\VoxBotanica\Assets\_Root\Exports";
	[SerializeField]
	private GameObject _ExportObject;


	[Button]
	private void Export()
	{
		string filePath = Path.Combine(_ExportFolderPath, $"{_ExportObject.name}.fbx");

		var meshFilter = _ExportObject.GetComponent<MeshFilter>();
		if (!meshFilter)
		{
			Debug.LogError("No MeshFilter Found!");
			return;
		}

		Mesh mesh = meshFilter.sharedMesh;


		using (var manager = FbxManager.Create())
		{
			manager.SetIOSettings(FbxIOSettings.Create(manager, Globals.IOSROOT));

			using (var exporter = FbxExporter.Create(manager, "Exporter"))
			{
				if (!exporter.Initialize(filePath, -1, manager.GetIOSettings()))
				{
					Debug.LogError("Failed to initialize exporter!");
					return;
				}

				var scene = FbxScene.Create(manager, "Scene");
				var node = FbxNode.Create(scene, _ExportObject.name);
				var fbxMesh = FbxMesh.Create(scene, "Mesh");

				Vector3[] vertices = mesh.vertices;
				int[] triangles = mesh.triangles;

				fbxMesh.InitControlPoints(vertices.Length);
				for (var i = 0; i < vertices.Length; i++)
					fbxMesh.SetControlPointAt(new FbxVector4(vertices[i].x,
												  vertices[i].y,
												  vertices[i].z), i);

				for (int i = 0; i < triangles.Length; i += 3)
				{
					fbxMesh.BeginPolygon();
					fbxMesh.AddPolygon(triangles[i]);
					fbxMesh.AddPolygon(triangles[i + 1]);
					fbxMesh.AddPolygon(triangles[i + 2]);
					fbxMesh.EndPolygon();
				}

				node.SetNodeAttribute(fbxMesh);
				scene.GetRootNode().AddChild(node);

				exporter.Export(scene);
			}
		}

		Debug.Log("Exported FBX to: " + filePath);
	}
}
}
