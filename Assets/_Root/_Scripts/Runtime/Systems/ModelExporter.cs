using System.IO;
using Autodesk.Fbx;
using Sirenix.OdinInspector;
using Sirenix.Utilities;
using UnityEngine;

namespace VoxBotanica
{
public class ModelExporter : MonoBehaviour
{
	private static readonly int _BaseColor = Shader.PropertyToID("_BaseColor");

	public string ExportFolderPath =
			@"W:\Projects\Engines\Unity\Abertay\[CMP400] - Honours Project\VoxBotanica\Assets\_Root\Exports";
	[SerializeField]
	private GameObject _ExportObject;

	private FbxManager _Manager;


	private void Awake()
	{
		_Manager = FbxManager.Create();
		_Manager.SetIOSettings(FbxIOSettings.Create(_Manager, Globals.IOSROOT));
	}

	private void OnDestroy()
	{
		_Manager.Dispose();
	}


	[Button]
	private void Export()
	{
		var fileName = $"{_ExportObject.name.ToTitleCase()}.fbx";
		string filePath = Path.Combine(ExportFolderPath, fileName);

		var meshFilter = _ExportObject.GetComponent<MeshFilter>();
		var meshRenderer = _ExportObject.GetComponent<MeshRenderer>();

		if (!meshFilter || !meshRenderer)
		{
			Debug.LogError("Missing MeshFilter or MeshRenderer.");
			return;
		}

		Mesh mesh = meshFilter.sharedMesh;

		if (_Manager == null)
		{
			_Manager = FbxManager.Create();
			_Manager.SetIOSettings(FbxIOSettings.Create(_Manager, Globals.IOSROOT));
		}

		using (var exporter = FbxExporter.Create(_Manager, "Exporter"))
		{
			if (!exporter.Initialize(filePath, -1, _Manager.GetIOSettings()))
			{
				Debug.LogError("Failed to initialize exporter.");
				return;
			}

			var scene = FbxScene.Create(_Manager, "Scene");
			var node = FbxNode.Create(scene, fileName);
			var fbxMesh = FbxMesh.Create(scene, "Mesh");

			Vector3[] vertices = mesh.vertices;
			Vector3[] normals = mesh.normals;
			Vector2[] uvs = mesh.uv;

			// Vertices.
			fbxMesh.InitControlPoints(vertices.Length);
			for (var i = 0; i < vertices.Length; i++)
				fbxMesh.SetControlPointAt(new FbxVector4(vertices[i].x,
														 vertices[i].y,
														 vertices[i].z), i);

			// Normals.
			FbxLayerElementNormal normalLayer = fbxMesh.CreateElementNormal();
			normalLayer.SetMappingMode(FbxLayerElement.EMappingMode.eByControlPoint);
			normalLayer.SetReferenceMode(FbxLayerElement.EReferenceMode.eDirect);

			foreach (Vector3 n in normals)
				normalLayer.GetDirectArray().Add(new FbxVector4(n.x, n.y, n.z));

			// UVs.
			FbxLayerElementUV uvLayer = fbxMesh.CreateElementUV("UVSet");
			uvLayer.SetMappingMode(FbxLayerElement.EMappingMode.eByControlPoint);
			uvLayer.SetReferenceMode(FbxLayerElement.EReferenceMode.eDirect);

			foreach (Vector2 uv in uvs)
				uvLayer.GetDirectArray().Add(new FbxVector2(uv.x, uv.y));

			// Material layer.
			FbxLayerElementMaterial materialLayer = fbxMesh.CreateElementMaterial();
			materialLayer.SetMappingMode(FbxLayerElement.EMappingMode.eByPolygon);
			materialLayer.SetReferenceMode(FbxLayerElement.EReferenceMode.eIndexToDirect);

			// Materials.
			Material[] materials = meshRenderer.sharedMaterials;
			foreach (Material material in materials)
			{
				var fbxMaterial = FbxSurfacePhong.Create(scene, material.name);

				Color colour = material.HasProperty(_BaseColor) ?
									   material.GetColor(_BaseColor) :
									   material.color;

				// Convert to gamma space.
				colour = colour.linear;

				fbxMaterial.Diffuse.Set(new FbxDouble3(colour.r, colour.g, colour.b));
				fbxMaterial.Ambient.Set(new FbxDouble3(colour.r, colour.g, colour.b));
				fbxMaterial.Specular.Set(new FbxDouble3(0.0, 0.0, 0.0));
				fbxMaterial.Shininess.Set(0.0);

				node.AddMaterial(fbxMaterial);
			}

			// Submeshes.
			for (var subMeshIndex = 0; subMeshIndex < mesh.subMeshCount; subMeshIndex++)
			{
				int[] triangles = mesh.GetTriangles(subMeshIndex);

				for (var i = 0; i < triangles.Length; i += 3)
				{
					fbxMesh.BeginPolygon(subMeshIndex);
					fbxMesh.AddPolygon(triangles[i]);
					fbxMesh.AddPolygon(triangles[i + 1]);
					fbxMesh.AddPolygon(triangles[i + 2]);
					fbxMesh.EndPolygon();

					materialLayer.GetIndexArray().Add(subMeshIndex);
				}
			}

			node.SetNodeAttribute(fbxMesh);
			scene.GetRootNode().AddChild(node);

			exporter.Export(scene);
		}

		Debug.Log("Exported FBX to: " + filePath);
	}
}
}
