using Game.UI;
using UnityEngine;
using UnityEngine.UIElements;

namespace VoxBotanica.Systems
{
using TrunkThicknessShape = TreeGenerator.TrunkThicknessShape;

public class UIManager : MonoBehaviour
{
	[SerializeField]
	private TreeGenerator _Generator;
	[SerializeField]
	private ModelExporter _Exporter;
	private bool _ReGenerate;

	private ColorField _CanopyColourField;
	private ColorField _TrunkColourField;
	private EnumField _TrunkShapeField;
	private Slider _BranchRadialField;
	private Slider _BranchTiltField;
	private UIDocument _Document;
	private UnsignedIntegerField _IterationField;
	private UnsignedIntegerField _MaxHeightField;
	private UnsignedIntegerField _TopCanopyThicknessField;
	private UnsignedIntegerField _TotalCanopyThicknessField;
	private UnsignedIntegerField _TrunkHeightField;
	private UnsignedIntegerField _TrunkThicknessField;
	private VisualElement _Root;

	private void Awake()
	{
		_Document = GetComponent<UIDocument>();
		_Root = _Document?.rootVisualElement;
	}

	private void Start()
	{
		BasePanelSetup();

		// Unsigned int fields.
		_IterationField = _Root.Q<UnsignedIntegerField>("IterationField");
		_IterationField.value = (uint)_Generator.LSystem.Iterations;
		_MaxHeightField = _Root.Q<UnsignedIntegerField>("MaxHeightField");
		_MaxHeightField.value = (uint)_Generator.MaxHeight;
		_TopCanopyThicknessField = _Root.Q<UnsignedIntegerField>("TopCanopyRadiusField");
		_TopCanopyThicknessField.value = (uint)_Generator.TrunkLeafRadius;
		_TotalCanopyThicknessField = _Root.Q<UnsignedIntegerField>("CanopyRadiusField");
		_TotalCanopyThicknessField.value = (uint)_Generator.LeafRadius;
		_TrunkHeightField = _Root.Q<UnsignedIntegerField>("TrunkHeightField");
		_TrunkHeightField.value = (uint)_Generator.TrunkHeight;
		_TrunkThicknessField = _Root.Q<UnsignedIntegerField>("TrunkThicknessField");
		_TrunkThicknessField.value = (uint)_Generator.TrunkThickness;

		// Slider fields.
		_BranchRadialField = _Root.Q<Slider>("BranchRadialField");
		_BranchRadialField.value = _Generator.BranchRadialPosition;
		_BranchTiltField = _Root.Q<Slider>("BranchTiltSlider");
		_BranchTiltField.value = _Generator.BranchTilt;

		// Enum field.
		_TrunkShapeField = _Root.Q<EnumField>("TrunkShapeField");
		_TrunkShapeField.value = _Generator.TrunkShape;

		ColourFieldsSetup();

		var exportBtn = _Root.Q<Button>("ExportBtn");
		exportBtn.clicked += () => _Exporter.Export();
	}

	private void Update()
	{
		if (_MaxHeightField.value < 1)
			_MaxHeightField.value = 1;
		if (_TrunkThicknessField.value < 1)
			_TrunkThicknessField.value = 1;
	}

	private void LateUpdate()
	{
		if (_IterationField.value != (uint)_Generator.LSystem.Iterations)
		{
			_Generator.LSystem.Iterations = (int)_IterationField.value;
			_ReGenerate = true;
		}
		else if (_MaxHeightField.value != (uint)_Generator.MaxHeight)
		{
			_Generator.MaxHeight = _MaxHeightField.value;
			_ReGenerate = true;
		}
		else if (_TrunkHeightField.value != (uint)_Generator.TrunkHeight)
		{
			_Generator.TrunkHeight = _TrunkHeightField.value;
			_ReGenerate = true;
		}
		else if (_TrunkThicknessField.value != (uint)_Generator.TrunkThickness)
		{
			_Generator.TrunkThickness = (int)_TrunkThicknessField.value;
			_ReGenerate = true;
		}
		else if (!Mathf.Approximately(_BranchRadialField.value,
									  _Generator.BranchRadialPosition))
		{
			_Generator.BranchRadialPosition = _BranchRadialField.value;
			_ReGenerate = true;
		}
		else if (!Mathf.Approximately(_BranchTiltField.value, _Generator.BranchTilt))
		{
			_Generator.BranchTilt = _BranchTiltField.value;
			_ReGenerate = true;
		}
		else if ((TrunkThicknessShape)_TrunkShapeField.value != _Generator.TrunkShape)
		{
			_Generator.TrunkShape = (TrunkThicknessShape)_TrunkShapeField.value;
			_ReGenerate = true;
		}

		// Canopy Settings.
		if (_TopCanopyThicknessField.value != (uint)_Generator.TrunkLeafRadius)
			_Generator.TrunkLeafRadius = (int)_TopCanopyThicknessField.value;
		if (_TotalCanopyThicknessField.value != (uint)_Generator.LeafRadius)
			_Generator.LeafRadius = (int)_TotalCanopyThicknessField.value;

		// ReSharper disable once RedundantCheckBeforeAssignment
		if (_TrunkColourField.value != _Generator.TrunkColour)
			_Generator.TrunkColour = _TrunkColourField.value;
		// ReSharper disable once RedundantCheckBeforeAssignment
		if (_CanopyColourField.value != _Generator.LeafColour)
			_Generator.LeafColour = _CanopyColourField.value;


		if (!_ReGenerate) return;
		_Generator.Generate();
		_ReGenerate = false;
	}

	private void OnEnable()
	{
		_Generator.OnTotalCanopyValueChanged += UpdateLeafSettingsFields;
	}

	private void OnDisable()
	{
		_Generator.OnTotalCanopyValueChanged -= UpdateLeafSettingsFields;
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
		_TrunkColourField = _Root.Q<ColorField>("TrunkColour");
		_CanopyColourField = _Root.Q<ColorField>("CanopyColour");

		_TrunkColourField.ColorPopup = colourPopup;
		_TrunkColourField.value = _Generator.TrunkColour;
		_TrunkColourField.ResetButtonPressed +=
				() => _TrunkColourField.value = Color.saddleBrown;

		_CanopyColourField.ColorPopup = colourPopup;
		_CanopyColourField.value = _Generator.LeafColour;
		_CanopyColourField.ResetButtonPressed +=
				() => _CanopyColourField.value = Color.oliveDrab;
	}

	private void UpdateLeafSettingsFields()
	{
		_TopCanopyThicknessField.value = _TotalCanopyThicknessField.value;
	}
}
}
