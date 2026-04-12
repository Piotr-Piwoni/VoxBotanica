using System;
using System.Collections.Generic;
using Sirenix.Utilities;
using UnityEngine;
using UnityEngine.UIElements;

namespace VoxBotanica.UIElements
{
[UxmlElement]
public partial class IntListFoldout : VisualElement
{
	[UxmlAttribute]
	public bool Collapsed
	{
		get => !_Foldout.value;
		set => _Foldout.value = !value;
	}
	[UxmlAttribute]
	public string Title
	{
		get => _Foldout.text;
		set => _Foldout.text = value;
	}

	private readonly Foldout _Foldout;
	private readonly ListView _ListView;
	private readonly Length _MaxRowHeight = new(50, LengthUnit.Pixel);
	private List<int> _Data = new();
	private Action<int, int> _OnChanged;

	[UxmlAttribute]
	public string LabelTitle = string.Empty;


	public IntListFoldout()
	{
		_Foldout = new Foldout { value = false, };
		_ListView = new ListView
		{
				virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight,
				selectionType = SelectionType.None,

				makeItem = MakeItem,
				bindItem = BindItem,

				style =
				{
						maxHeight = _MaxRowHeight.value * 10,
				},
		};

		_Foldout.Add(_ListView);
		hierarchy.Add(_Foldout);
	}


	// Call this from your generator/editor.
	public void SetData(List<int> data, Action<int, int> onChanged)
	{
		_Data = data;
		_OnChanged = onChanged;

		_ListView.itemsSource = _Data;
		_ListView.Rebuild();
	}

	private VisualElement MakeItem()
	{
		var row = new VisualElement
		{
				style =
				{
						flexDirection = FlexDirection.Row,
						maxHeight = _MaxRowHeight,
						alignItems = Align.Stretch,
				},
		};

		var field = new UnsignedIntegerField
		{
				label = LabelTitle,
				style =
				{
						flexGrow = new StyleFloat(1),
						flexShrink = new StyleFloat(1),
				},
		};

		row.Add(field);

		return row;
	}

	private void BindItem(VisualElement element, int index)
	{
		var field = element.ElementAt(0) as UnsignedIntegerField;

		field!.label = $"{LabelTitle} {index + 1}";
		field.SetValueWithoutNotify((uint)_Data[index]);
		field.userData = index;

		field.UnregisterValueChangedCallback(OnFieldChanged);
		field.RegisterValueChangedCallback(OnFieldChanged);
	}

	private void OnFieldChanged(ChangeEvent<uint> evt)
	{
		var field = evt.target as UnsignedIntegerField;
		var index = (int)field!.userData;

		var value = (int)evt.newValue;
		_Data[index] = value;
		_OnChanged?.Invoke(index, value);
	}

	// Visuals only.
	public void SetAllValues(int value)
	{
		if (_Data.IsNullOrEmpty()) return;

		value = Mathf.Clamp(value, 0, int.MaxValue);
		// Update backing data.
		for (var i = 0; i < _Data.Count; i++)
			_Data[i] = value;

		_ListView.Rebuild();
	}
}
}
