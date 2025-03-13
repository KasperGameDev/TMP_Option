using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.Collections.Generic;
using ShadowResolution = UnityEngine.Rendering.Universal.ShadowResolution;
using static SettingData;
using TMPro;
using System.Linq;

public class SettingsMenu : MonoBehaviour
{
	public Volume volume;
	public UniversalRenderPipelineAsset urpAsset;
	
	[SerializeField] private Section sectionPrefab;
	[SerializeField] private TextMeshProUGUI debugText;
	[SerializeField] private TextMeshProUGUI titleText;

	private bool initialized = false;
	private ScrollRect scrollRect;

	private Section display, graphics, postProcessing, audio;
	private Section[] sections;

	private void Awake()
	{
		scrollRect = GetComponentInChildren<ScrollRect>();
	}

	private void Start()
	{
		if (!initialized)
			Initialize();
	}

	private void Initialize()
	{
		Destroy(scrollRect.content.gameObject);
		initialized = true;

		display = CreateSection("Display", SettingManager.DisplaySettings);
		graphics = CreateSection("Graphics", SettingManager.GraphicsSettings);
		postProcessing = CreateSection("Post Processing", SettingManager.PostProcessingSettings);
		audio = CreateSection("Audio", SettingManager.AudioSettings);
		sections = new Section[] { display, graphics, postProcessing, audio };

		OpenSection(display);

		Section CreateSection(string title, Setting[] settings)
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
		var section = sections.FirstOrDefault(s => s.Title.ToLower() == title.ToLower());
		if (section)
			OpenSection(section);
		else
			Debug.LogError($"Couldn't find section: {name}");
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

	private void Test()
	{
		volume.profile.TryGet(out Bloom bloom);
		bloom.active = true;

		Application.targetFrameRate = 60;
		int displayCount = Display.displays.Length;
		if (displayCount > 1)
		{
			// Set the game to the second monitor (index 1)
			Display.displays[1].Activate();
		}

		RenderSettings.fogMode = FogMode.Linear;
		RenderSettings.fogStartDistance = 1f;
		RenderSettings.fogEndDistance = 1000f;

		UniversalRenderPipelineAsset data = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;

		if (data)
		{
			data.shadowCascadeCount = 2;
			data.shadowDistance = 150f;
			UnityGraphicsHack.SoftShadowsEnabled = true;
			UnityGraphicsHack.MainLightShadowResolution = ShadowResolution._256;
			UnityGraphicsHack.AdditionalLightShadowResolution = ShadowResolution._256;
			data.msaaSampleCount = 4;
		}

		if (volume.profile.TryGet(out ColorAdjustments colorAdjustments))
		{
			// Adjust the brightness (Exposure)
			colorAdjustments.postExposure.value = 1.5f;
		}
		/*
		 https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@14.0/manual/quality/quality-settings-through-code.html
		 */
	}
}
