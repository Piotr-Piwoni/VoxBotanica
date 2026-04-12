using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace VoxBotanica.UIElements
{
[UxmlElement]
public partial class LSystemRulesFoldout : VisualElement
{
	[UxmlAttribute]
	public string Title
	{
		get => _Foldout.text;
		set => _Foldout.text = value;
	}

	private readonly Foldout _Foldout;

	// We cache keys because Dictionary cannot be indexed safely.
	private readonly List<string> _Keys = new();
	private readonly ListView _ListView;

	private readonly Length _MaxRowHeight = new(50, LengthUnit.Pixel);
	private Dictionary<string, string> _Data;
	private Action<string, string> _OnChanged;

	[UxmlAttribute]
	public string KeyLabel = "Symbol";
	[UxmlAttribute]
	public string ValueLabel = "Rule";

	public LSystemRulesFoldout()
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

	// Bind directly to your L-system dictionary.
	public void SetData(Dictionary<string, string> rules,
			Action<string, string> onChanged)
	{
		_Data = rules;
		_OnChanged = onChanged;

		RebuildKeyCache();
	}

	private void RebuildKeyCache()
	{
		_Keys.Clear();

		if (_Data == null) return;

		foreach (KeyValuePair<string, string> kvp in _Data)
			_Keys.Add(kvp.Key);

		_ListView.itemsSource = _Keys;
		_ListView.Rebuild();
	}

	// Row creation.
	private VisualElement MakeItem()
	{
		var row = new VisualElement
		{
				style =
				{
						flexDirection = FlexDirection.Row,
						alignItems = Align.Stretch,
						maxHeight = _MaxRowHeight,
				},
		};

		var keyField = new TextField
		{
				label = KeyLabel,
				maxLength = 1,
				style = { flexGrow = 1, },
		};

		var valueField = new TextField
		{
				label = ValueLabel,
				style = { flexGrow = 3, },
		};

		row.Add(keyField);
		row.Add(valueField);

		return row;
	}

	private void BindItem(VisualElement element, int index)
	{
		var keyField = element.ElementAt(0) as TextField;
		var valueField = element.ElementAt(1) as TextField;

		string key = _Keys[index];
		string value = _Data[key];

		keyField!.SetValueWithoutNotify(key);
		valueField!.SetValueWithoutNotify(value);

		keyField.userData = index;
		valueField.userData = index;

		keyField.UnregisterValueChangedCallback(OnKeyChanged);
		valueField.UnregisterValueChangedCallback(OnValueChanged);

		keyField.RegisterValueChangedCallback(OnKeyChanged);
		valueField.RegisterValueChangedCallback(OnValueChanged);
	}

	private void OnKeyChanged(ChangeEvent<string> evt)
	{
		var field = evt.target as TextField;
		var index = (int)field!.userData;

		string oldKey = _Keys[index];
		string newKey = evt.newValue;

		if (string.IsNullOrEmpty(newKey))
			return;

		newKey = newKey.Substring(0, 1);

		if (oldKey == newKey)
			return;

		// Preserve value.
		string value = _Data[oldKey];

		_Data.Remove(oldKey);

		// Prevent accidental overwrite.
		if (!_Data.ContainsKey(newKey)) _Data[newKey] = value;

		RebuildKeyCache();

		_OnChanged?.Invoke(newKey, value);
	}

	private void OnValueChanged(ChangeEvent<string> evt)
	{
		var field = evt.target as TextField;
		var index = (int)field!.userData;

		string key = _Keys[index];

		_Data[key] = evt.newValue;

		_OnChanged?.Invoke(key, evt.newValue);
	}
}
}
