using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.Collections.Generic;
using ShadowResolution = UnityEngine.Rendering.Universal.ShadowResolution;
using static SettingData;
using TMPro;
using System.Linq;
using System;
using System.Threading.Tasks;

public class SettingsMenu : MonoBehaviour
{
	public Volume volume;
	public UniversalRenderPipelineAsset urpAsset;
	[SerializeField] private Section sectionPrefab;

	private bool initialized = false;
	private ScrollRect scrollRect;
	private TextMeshProUGUI debugText;

	// Display 
	private FullScreenMode fullScreenMode = FullScreenMode.FullScreenWindow;
	private List<Resolution> resolutions = new();
	private Dictionary<string, List<RefreshRate>> legalRefreshRates = new();

	private void Awake()
	{
		scrollRect = GetComponentInChildren<ScrollRect>();
		debugText = transform.parent.Find("Debug").GetComponent<TextMeshProUGUI>();
	}

	private void Start()
	{
		if (!initialized)
			Initialize();
	}

	private void UpdateResolutionData()
	{
		resolutions.Clear();
		legalRefreshRates.Clear();
		foreach (var r in Screen.resolutions)
		{
			var k = $"{r.width}x{r.height}";

			if (!legalRefreshRates.ContainsKey(k))
			{
				resolutions.Add(r);
				legalRefreshRates.Add(k, new());
			}

			legalRefreshRates[k].Add(r.refreshRateRatio);
			debugText.text += $"\n{r.width}x{r.height}@{r.refreshRateRatio}";
		}
	}

	async Task MoveWindowTask(int index)
	{
		List<DisplayInfo> displayLayout = new List<DisplayInfo>();
		Screen.GetDisplayLayout(displayLayout);
		if (index < displayLayout.Count)
		{
			DisplayInfo display = displayLayout[index];
			Vector2Int position = new Vector2Int(0, 0);
			if (Screen.fullScreenMode != FullScreenMode.Windowed)
			{
				position.x += display.width / 2;
				position.y += display.height / 2;
			}
			AsyncOperation asyncOperation = Screen.MoveMainWindowTo(display, position);
			while (asyncOperation.progress < 1f)
			{
				await Task.Yield();
			}
			Screen.SetResolution(display.width, display.width, fullScreenMode, display.refreshRate);
			debugText.text = $"Display {index}";
		}
		else
		{
			await Task.CompletedTask;
		}
	}

	async void MoveWindowAsync(int index)
	{
		await MoveWindowTask(index);
	}

	private void Initialize()
	{
		Destroy(scrollRect.content.gameObject);
		initialized = true;

		QualitySettings.vSyncCount = 0;
		UpdateResolutionData();

		var idx = PlayerPrefs.HasKey("Resolution") ? PlayerPrefs.GetInt("Resolution") : resolutions.Count - 1;
		idx = Mathf.Clamp(idx, 0, resolutions.Count - 1);
		var res = resolutions[idx];
		var key = $"{res.width}x{res.height}";

		var refreshRateSetting = new OptionSetting("Refresh Rate")
		{
			options = legalRefreshRates[key].Select(r => r.value.ToString("F2")).ToArray(),
			onSave = i =>
			{
				var idx = PlayerPrefs.HasKey("Resolution") ? PlayerPrefs.GetInt("Resolution") : resolutions.Count - 1;
				var res = resolutions[idx];
				var key = $"{res.width}x{res.height}";
				var hz = legalRefreshRates[key][i];
				Screen.SetResolution(res.width, res.height, fullScreenMode, hz);
				//debugText.text = $"{res.width}x{res.height}@{hz}";
			},
			defaultValue = legalRefreshRates[key].Count - 1,
		};

		var resolutionSetting = new OptionSetting("Resolution")
		{
			options = resolutions.Select(r => $"{r.width}x{r.height}").ToArray(),
			onSave = i =>
			{
				var res = resolutions[i];
				var key = $"{res.width}x{res.height}";
				var hz = legalRefreshRates[key].Last();

				Screen.SetResolution(res.width, res.height, fullScreenMode, hz);

				refreshRateSetting.options = legalRefreshRates[key].Select(r => r.value.ToString("F2")).ToArray();
				refreshRateSetting.Set(legalRefreshRates[key].Count - 1);
				refreshRateSetting.forceUpdate?.Invoke();
			},
			defaultValue = resolutions.Count - 1,
		};


		int displayCount = Display.displays.Length;
		int displayIndex = Display.displays.ToList().FindIndex(d => d.active);

		var displaySetting = new OptionSetting("Monitor")
		{
			options = Enumerable.Range(1, displayCount).Select(i => $"{i}").ToArray(),
			onSave = async i =>
			{
				await MoveWindowTask(i);
				UpdateResolutionData();

				var res = resolutions[resolutions.Count - 1];
				var key = $"{res.width}x{res.height}";

				resolutionSetting.options = resolutions.Select(r => $"{r.width}x{r.height}").ToArray();
				resolutionSetting.Set(resolutions.Count - 1);
				resolutionSetting.forceUpdate?.Invoke();

				refreshRateSetting.options = legalRefreshRates[key].Select(r => r.value.ToString("F2")).ToArray();
				refreshRateSetting.Set(legalRefreshRates[key].Count - 1);
				refreshRateSetting.forceUpdate?.Invoke();
			},
			defaultValue = displayIndex,
		};

		var test = new List<Setting>()
		{
			
			new ButtonSetting("Reset")
			{
				text = "Reset",
				defaultValue = () =>
				{
					print("Reset");
					PlayerPrefs.DeleteAll();
				}
			},
			resolutionSetting,
			refreshRateSetting,
			displaySetting,
			//new OptionSetting("Dropdown")
			//{
			//	options = new string[] { "Test 1", "Test 2", "Test 3" },
			//	onSave = i => print($"Dropdown {i}"),
			//	defaultValue = 1,
			//},
			//new ToggleSetting("Toggle")
			//{
			//	onSave = b => print($"Toggle {b}"),
			//	defaultValue = true,
			//},
			//new SliderSetting("Slider")
			//{
			//	min = 0f,
			//	max = 100f,
			//	onSave = f => print($"Slider {f}"),
			//	defaultValue = 50f,
			//},
			//new ButtonSetting("Button")
			//{
			//	defaultValue = () =>
			//	{
			//		print("Button pressed");
			//		PlayerPrefs.DeleteAll();
			//	}
			//}
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
