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

	private Section general, display, graphics, postProcessing, audio;
	private List<Section> sections = new();

	private void Awake()
	{
		scrollRect = GetComponentInChildren<ScrollRect>();
	}

	private void Start()
	{
		if (!initialized)
			Initialize();

		List<DisplayInfo> displayLayout = new List<DisplayInfo>();
		Screen.GetDisplayLayout(displayLayout);

		debugText.text = "DISPLAYS: " + Display.displays.Length;
		debugText.text += "\nDISPLAYS_LAYOUT: " + displayLayout.Count;
		foreach (var display in displayLayout)
			debugText.text += "\nDISPLAYS_LAYOUT: " + display.name;

	}

	private void Initialize()
	{
		Destroy(scrollRect.content.gameObject);
		initialized = true;

		general = CreateSection("General", SettingManager.GeneralSettings);
		display = CreateSection("Display", SettingManager.DisplaySettings);
		graphics = CreateSection("Graphics", SettingManager.GraphicSettings);
		postProcessing = CreateSection("Post Processing", SettingManager.PostProcessingSettings);
		audio = CreateSection("Audio", SettingManager.AudioSettings);

		OpenSection(general);

		Section CreateSection(string title, IEnumerable<Setting> settings)
		{
			var section = Instantiate(sectionPrefab, scrollRect.viewport.transform);
			section.name = $"Section ({title})";
			section.Title = title;
			section.Populate(settings);
			section.gameObject.SetActive(false);
			sections.Add(section);
			return section;
		}
	}

	public void OpenSection(string title)
	{
		var section = sections.FirstOrDefault(s => s.Title.ToLower() == title.ToLower());
		if (section)
			OpenSection(section);
		else
			Debug.LogError($"Couldn't find section: {title}");
	}

	private void OpenSection(Section section)
	{
		foreach (var s in sections)
		{
			s.gameObject.SetActive(s == section);
		}

		titleText.text = section.Title;
		scrollRect.content = section.transform as RectTransform;
	}
}
