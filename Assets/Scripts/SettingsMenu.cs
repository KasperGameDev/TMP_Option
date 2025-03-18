using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;
using static SettingData;

public class SettingsMenu : MonoBehaviour
{
	[SerializeField] private Section _sectionPrefab;
	[SerializeField] private TextMeshProUGUI _debugText;
	[SerializeField] private TextMeshProUGUI _titleText;

	private bool _initialized = false;
	private ScrollRect _scrollRect;
	private Dictionary<string, Section> _sections = new();

	private void Awake()
	{
		_scrollRect = GetComponentInChildren<ScrollRect>();
	}

	private void Start()
	{
		if (!_initialized)
		{
			Initialize();
		}

		if (_debugText != null)
		{
			List<DisplayInfo> displayLayout = new List<DisplayInfo>();
			Screen.GetDisplayLayout(displayLayout);

			_debugText.text = "DISPLAYS: " + Display.displays.Length;
			_debugText.text += "\nDISPLAYS_LAYOUT: " + displayLayout.Count;
			foreach (var display in displayLayout)
			{
				_debugText.text += "\nDISPLAYS_LAYOUT: " + display.name;
			}
		}
	}

	private void Initialize()
	{
		Destroy(_scrollRect.content.gameObject);
		_initialized = true;

		foreach (var key in SettingManager.Settings.Keys)
		{
			_sections[key] = CreateSection(key, SettingManager.Settings[key]);
		}

		OpenSection("General");

		Section CreateSection(string title, IEnumerable<Setting> settings)
		{
			var section = Instantiate(_sectionPrefab, _scrollRect.viewport.transform);
			section.name = $"Section ({title})";
			section.Title = title;
			section.Populate(settings);
			section.gameObject.SetActive(false);
			return section;
		}
	}

	public void OpenSection(string title)
	{
		var section = _sections[title];
		if (section)
		{
			OpenSection(section);
		}
		else
		{
			Debug.LogError($"Couldn't find section: {title}");
		}
	}

	private void OpenSection(Section section)
	{
		foreach (var s in _sections.Values)
		{
			s.gameObject.SetActive(s == section);
		}

		_titleText.text = section.Title;
		_scrollRect.content = section.transform as RectTransform;
	}
}
