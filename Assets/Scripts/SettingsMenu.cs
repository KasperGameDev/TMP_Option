using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using ShadowResolution = UnityEngine.Rendering.Universal.ShadowResolution;
using static SettingData;
using TMPro;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class SettingsMenu : MonoBehaviour
{
	public Volume volume;
	public UniversalRenderPipelineAsset urpAsset;
	
	[SerializeField] private Section sectionPrefab;
	[SerializeField] private TextMeshProUGUI debugText;
	[SerializeField] private TextMeshProUGUI titleText;

	private bool initialized = false;
	private ScrollRect scrollRect;

	private Dictionary<string, Section> sections = new();

	private void Awake()
	{
		scrollRect = GetComponentInChildren<ScrollRect>();
	}

	private void Start()
	{
		if (!initialized)
			Initialize();

		if (debugText != null)
		{
			List<DisplayInfo> displayLayout = new List<DisplayInfo>();
			Screen.GetDisplayLayout(displayLayout);

			debugText.text = "DISPLAYS: " + Display.displays.Length;
			debugText.text += "\nDISPLAYS_LAYOUT: " + displayLayout.Count;
			foreach (var display in displayLayout)
				debugText.text += "\nDISPLAYS_LAYOUT: " + display.name;
		}
	}

	private void Initialize()
	{
		Destroy(scrollRect.content.gameObject);
		initialized = true;

		foreach (var key in SettingManager.Settings.Keys)
		{
			sections[key] = CreateSection(key, SettingManager.Settings[key]);
		}

		OpenSection("General");

		Section CreateSection(string title, IEnumerable<Setting> settings)
		{
			var section = Instantiate(sectionPrefab, scrollRect.viewport.transform);
			section.name = $"Section ({title})";
			section.Title = title;
			section.Populate(settings);
			section.gameObject.SetActive(false);
			return section;
		}
	}

	public void OpenSection(string title)
	{
		var section = sections[title];
		if (section)
			OpenSection(section);
		else
			Debug.LogError($"Couldn't find section: {title}");
	}

	private void OpenSection(Section section)
	{
		foreach (var s in sections.Values)
		{
			s.gameObject.SetActive(s == section);
		}

		titleText.text = section.Title;
		scrollRect.content = section.transform as RectTransform;
	}
}
