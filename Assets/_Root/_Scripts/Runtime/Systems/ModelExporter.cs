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
	private Object _ExportObject;


	[Button]
	private void Export()
	{
		var fileName = $"{_ExportObject.name}.fbx";
		string filePath = Path.Combine(_ExportFolderPath, fileName);

		using (var fbxManager = FbxManager.Create())
		{
			fbxManager.SetIOSettings(FbxIOSettings.Create(fbxManager, Globals.IOSROOT));

			using (var exporter = FbxExporter.Create(fbxManager, "myExporter"))
			{
				bool status = exporter.Initialize(fileName, -1,
												  fbxManager.GetIOSettings());

				var scene = FbxScene.Create(fbxManager, "myScene");

				exporter.Export(scene);
			}
		}
	}
}
}
