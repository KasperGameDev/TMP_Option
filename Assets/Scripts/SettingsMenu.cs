using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.Collections.Generic;
using ShadowResolution = UnityEngine.Rendering.Universal.ShadowResolution;
using static SettingData;

public class SettingsMenu : MonoBehaviour
{
	public Volume volume;
	public UniversalRenderPipelineAsset urpAsset;
	[SerializeField] private Section sectionPrefab;

	private bool initialized = false;
	private ScrollRect scrollRect;

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
		initialized = true;

		var test = new List<Setting>()
		{
			new OptionSetting("Dropdown")
			{
				options = new string[] { "Test 1", "Test 2", "Test 3" },
				onSave = i => print($"Dropdown {i}"),
				defaultValue = 1,
			},
			new ToggleSetting("Toggle")
			{
				onSave = b => print($"Toggle {b}"),
				defaultValue = true,
			},
			new SliderSetting("Slider")
			{
				min = 0f,
				max = 100f,
				onSave = f => print($"Slider {f}"),
				defaultValue = 50f,
			},
			new ButtonSetting("Button")
			{
				defaultValue = () => print("Button pressed")
			}
		};

		var sectionTest = CreateSection("Test", test);
		scrollRect.content = sectionTest.transform as RectTransform;

		Section CreateSection(string title, List<Setting> settings)
		{
			var section = Instantiate(sectionPrefab, scrollRect.viewport.transform);
			section.name = $"Section ({title})";
			section.Populate(settings);
			return section;
		}
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
