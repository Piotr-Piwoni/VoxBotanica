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
		var panelButton = _Root.Q<Button>();

		panelButton.clicked += () =>
		{
			float panelWidth = panel.resolvedStyle.width + 10f;

			bool isClosed = _Root.ClassListContains("closed");

			if (isClosed)
			{
				_Root.RemoveFromClassList("closed");
				panelButton.text = "◀";
				_Root.style.translate = new Translate(0, 0, 0);
			}
			else
			{
				_Root.AddToClassList("closed");
				panelButton.text = "▶";
				_Root.style.translate = new Translate(-panelWidth, 0, 0);
			}
		};
	}
}
}
