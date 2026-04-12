using Game.UI;
using UnityEngine;
using UnityEngine.UIElements;

namespace VoxBotanica.Systems
{
public class UIManager : MonoBehaviour
{
	[SerializeField]
	private TreeGenerator _Generator;
	[SerializeField]
	private ModelExporter _Exporter;

	private UIDocument _Document;

	private UnsignedIntegerField _IterationField;
	private UnsignedIntegerField _TrunkHeight;
	private VisualElement _Root;


	private void Awake()
	{
		_Document = GetComponent<UIDocument>();
		_Root = _Document?.rootVisualElement;
	}

	private void Start()
	{
		BasePanelSetup();


		_IterationField = _Root.Q<UnsignedIntegerField>("IterationField");
		_IterationField.value = (uint)_Generator._LSystem.Iterations;

		_TrunkHeight = _Root.Q<UnsignedIntegerField>("TrunkHeightField");
		_TrunkHeight.value = (uint)_Generator.TrunkHeight;

		ColourFieldsSetup();

		var exportBtn = _Root.Q<Button>("ExportBtn");
		exportBtn.clicked += () => _Exporter.Export();
	}

	private void LateUpdate()
	{
		if (_IterationField.value != (uint)_Generator._LSystem.Iterations)
			_Generator._LSystem.Iterations = (int)_IterationField.value;
		if (_TrunkHeight.value != (uint)_Generator.TrunkHeight)
			_Generator.TrunkHeight = _TrunkHeight.value;
	}


	private void BasePanelSetup()
	{
		var panel = _Root.Q<GroupBox>();
		var panelBtn = _Root.Q<Button>("PanelViewBtn");
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
	}

	private void ColourFieldsSetup()
	{
		var colourPopup = _Root.Q<ColorPopup>();
		var trunkColourFiled = _Root.Q<ColorField>("TrunkColour");
		var canopyColourFiled = _Root.Q<ColorField>("CanopyColour");

		trunkColourFiled.ColorPopup = colourPopup;
		trunkColourFiled.value = Color.saddleBrown;
		trunkColourFiled.ResetButtonPressed +=
				() => trunkColourFiled.value = Color.saddleBrown;

		canopyColourFiled.ColorPopup = colourPopup;
		canopyColourFiled.value = Color.oliveDrab;
		canopyColourFiled.ResetButtonPressed +=
				() => canopyColourFiled.value = Color.oliveDrab;
	}
}
}
