using Game.UI;
using UnityEngine;
using UnityEngine.UIElements;

namespace VoxBotanica.Systems
{
public class UIManager : MonoBehaviour
{
	private UIDocument _Document;
	private VisualElement _Root;


	private void Awake()
	{
		_Document = GetComponent<UIDocument>();
		_Root = _Document?.rootVisualElement;
	}

	private void Start()
	{
		var panel = _Root.Q<GroupBox>();
		var panelBtn = _Root.Q<Button>("PanelViewBtn");
		var exportBtn = _Root.Q<Button>("ExportBtn");

		exportBtn.clicked += () => { Debug.Log("EXPORT!!!"); };

		panelBtn.clicked += () =>
		{
			float panelWidth = panel.resolvedStyle.width + 10f;

			bool isClosed = _Root.ClassListContains("closed");

			if (isClosed)
			{
				_Root.RemoveFromClassList("closed");
				panelBtn.text = "◀";
				_Root.style.translate = new Translate(0, 0, 0);
			}
			else
			{
				_Root.AddToClassList("closed");
				panelBtn.text = "▶";
				_Root.style.translate = new Translate(-panelWidth, 0, 0);
			}
		};

		var colourPopup = _Root.Q<ColorPopup>();
		var trunkColourFiled = _Root.Q<ColorField>("TrunkColour");
		trunkColourFiled.ColorPopup = colourPopup;
		trunkColourFiled.ResetButtonPressed +=
				() => trunkColourFiled.value = Color.saddleBrown;
	}
}
}
