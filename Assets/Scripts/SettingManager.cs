using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using ShadowResolution = UnityEngine.Rendering.Universal.ShadowResolution;
using static SettingData;

public static class SettingManager
{
	[SerializeField]
	private static VolumeProfile volumeProfile;

	public static List<Setting> DisplaySettings { get; private set; }
	public static List<Setting> GraphicSettings { get; private set; }
	public static List<Setting> PostProcessingSettings { get; private set; }
	public static List<Setting> AudioSettings { get; private set; }

	// Display 
	private static FullScreenMode fullScreenMode = FullScreenMode.FullScreenWindow;
	private static List<Resolution> resolutions = new();
	private static Dictionary<string, List<RefreshRate>> legalRefreshRates = new();

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
	private static void Initialize()
	{
		DisplaySettings = CreateDisplaySettings();
		GraphicSettings = CreateGraphicsSettings();
		PostProcessingSettings = new ();
		AudioSettings = new ();
	}

	private static void UpdateResolutionData()
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
			//debugText.text += $"\n{r.width}x{r.height}@{r.refreshRateRatio}";
		}
	}

	static async Task MoveWindowTask(int index)
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
			//debugText.text = $"Display {index}";
		}
		else
		{
			await Task.CompletedTask;
		}
	}

	private static List<Setting> CreateDisplaySettings()
	{
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
	
		var fullscreenSetting = new OptionSetting("Fullscreen Mode")
		{
			options = Enum.GetNames(typeof(FullScreenMode)),
			onSave = v => fullScreenMode = (FullScreenMode)v,
			defaultValue = (int)FullScreenMode.FullScreenWindow,
		};

		var fpsMaxTargets = new[] { 4, 30, 60, 90, 120, 144, 165, 180, 240, -1 };
		var fpsMaxOptions = fpsMaxTargets.Select(i => $"{i}").ToArray();
		fpsMaxOptions[fpsMaxOptions.Length - 1] = "Unlimited";

		var fpsMaxSetting = new OptionSetting("FPS Max")
		{
			options = fpsMaxOptions,
			onSave = i => Application.targetFrameRate = fpsMaxTargets[i],
			defaultValue = fpsMaxOptions.Length - 1,
		};

		var vSyncSetting = new ToggleSetting("VSync")
		{
			onSave = v => QualitySettings.vSyncCount = v ? 1 : 0,
			defaultValue = false,
		};

		return new () 
		{ 
			displaySetting, 
			resolutionSetting, 
			refreshRateSetting,
			fpsMaxSetting,
			vSyncSetting
		};
	}

	private static List<Setting> CreateGraphicsSettings()
	{
		UniversalRenderPipelineAsset data = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;

		if (!data)
		{
			Debug.LogError("URP Asset couldn't be found!");
			return new List<Setting>();
		}

		data.shadowCascadeCount = 2;
		data.shadowDistance = 150f;
		UnityGraphicsHack.SoftShadowsEnabled = true;
		UnityGraphicsHack.MainLightShadowResolution = ShadowResolution._256;
		UnityGraphicsHack.AdditionalLightShadowResolution = ShadowResolution._256;
		data.msaaSampleCount = 4;

		//var msaa = new[] { 1, 2, 4, 8 };
		//var msaaOptions = msaa.Select(i => $"{i}x").ToArray();
		//msaaOptions[0] = "Disabled";

		var shadowRes = Enum.GetValues(typeof(ShadowResolution)).Cast<ShadowResolution>().ToList();
		var msaa = Enum.GetValues(typeof(MsaaQuality)).Cast<MsaaQuality>().ToList();

		var shadowResolutionSetting = new OptionSetting("Shadow Resolution")
		{
			options = Enum.GetNames(typeof(ShadowResolution)).Select(r => $"{r}".Substring(1)).ToArray(),
			onSave = v =>
			{
				UnityGraphicsHack.MainLightShadowResolution = shadowRes[v];
				UnityGraphicsHack.AdditionalLightShadowResolution = shadowRes[v];
			},
			defaultValue = shadowRes.IndexOf(ShadowResolution._1024),
		};

		var shadowDistanceSetting = new SliderSetting("Shadow Distance")
		{
			min = 1f,
			max = 500f,
			onSave = v => data.shadowDistance = v,
			defaultValue = 150f,
		};

		var softShadowsSetting = new ToggleSetting("Soft Shadows")
		{
			onSave = v => UnityGraphicsHack.SoftShadowsEnabled = v,
			defaultValue = true,
		};

		var msaaSetting = new OptionSetting("MSAA")
		{
			options = msaa.Select(i => $"{i}".Replace("_", "")).ToArray(),
			onSave = v => data.msaaSampleCount = (int)msaa[v],
			defaultValue = (int)MsaaQuality._2x,
		};

		return new ()
		{
			shadowResolutionSetting,
			shadowDistanceSetting,
			softShadowsSetting,
			msaaSetting,
		};
	}
}
