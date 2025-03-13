using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using ShadowResolution = UnityEngine.Rendering.Universal.ShadowResolution;
using static SettingData;

public static class SettingManager
{

	public static List<Setting> GeneralSettings { get; private set; }
	public static List<Setting> DisplaySettings { get; private set; }
	public static List<Setting> GraphicSettings { get; private set; }
	public static List<Setting> PostProcessingSettings { get; private set; }
	public static List<Setting> AudioSettings { get; private set; }

	private static VolumeProfile volumeProfile;
	private static FullScreenMode fullScreenMode = FullScreenMode.FullScreenWindow;
	private static List<Resolution> resolutions = new();
	private static Dictionary<string, List<RefreshRate>> legalRefreshRates = new();

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
	private static void Initialize()
	{
		Reset();

		GeneralSettings = CreateGeneralSettings();
		DisplaySettings = CreateDisplaySettings();
		GraphicSettings = CreateGraphicsSettings();
		AudioSettings	= CreateAudioSettings();
		PostProcessingSettings = new ();

		SceneManager.sceneLoaded += (_,_) => FindVolume();
	}

	private static void Reset()
	{
		GeneralSettings = null;
		DisplaySettings = null;
		GraphicSettings = null;
		AudioSettings = null;
		PostProcessingSettings = null;
		volumeProfile = null;
	}

	private static void FindVolume()
	{
		var volumes = SceneManager.GetActiveScene()
			.GetRootGameObjects()
			.Select(g => g.GetComponent<Volume>())
			.Where(v => v != null);

		if (volumes.Count() > 1)
		{
			Debug.LogError("Multiple volumes found!");
			return;
		}

		var volume = volumes.FirstOrDefault();

		if (volume != null)
		{
			volumeProfile = volume.profile;
			
			if (volumeProfile != null && volume.profile != volume)
			{
				Debug.LogWarning("Volume profile changed!");
				volumeProfile = volume.profile;
			}
		}
		else
		{
			Debug.LogError("Volume could not be found!");
		}

		if (volumeProfile != null)
			PostProcessingSettings = CreatePostProcessingSettings();
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

	private static List<Setting> CreateGeneralSettings()
	{
		var audioLanguageSetting = new OptionSetting("Audio Language")
		{
			options = new string[] { "English", "Danish", "Whatever Dany is working on" },
			onSave = i => { },
		};
		var subtitleLanguageSetting = new OptionSetting("Subtitle Language")
		{
			options = new string[] { "English", "Danish", "Whatever Dany is working on" },
			onSave = i => { },
		};
		var resetSetting = new ButtonSetting("Reset")
		{
			text = "Reset",
			defaultValue = PlayerPrefs.DeleteAll,
		};
		return new()
		{
			resetSetting,
			audioLanguageSetting,
			subtitleLanguageSetting
		};
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

		List<DisplayInfo> displayLayout = new List<DisplayInfo>();
		Screen.GetDisplayLayout(displayLayout);
		int displayCount = displayLayout.Count;
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
			onSave = v =>
			{
				fullScreenMode = (FullScreenMode)v;
				// do this with cur res + cur hz
				//Screen.SetResolution(res.width, res.height, fullScreenMode, res.refreshRateRatio);
			},
			defaultValue = (int)FullScreenMode.FullScreenWindow,
		};

		var fpsMaxTargets = new[] { 30, 60, 90, 120, 144, 165, 180, 240, -1 };
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
			defaultValue = true,
		};

		return new () 
		{ 
			fullscreenSetting,
			resolutionSetting, 
			refreshRateSetting,
			displaySetting, 
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

		var fogModeSetting = new OptionSetting("Fog Mode")
		{
			options = Enum.GetNames(typeof(FogMode)).Select(r => $"{r}").ToArray(),
			onSave = v => RenderSettings.fogMode = (FogMode)(v+1),
			defaultValue = (int)FogMode.Linear,
		};

		var fogSliderSetting = new SliderSetting("Fog Distance")
		{
			min = 100f,
			max = 1000f,
			onSave = v => RenderSettings.fogEndDistance = v,
			defaultValue = 1000f,
		};

		return new ()
		{
			shadowResolutionSetting,
			shadowDistanceSetting,
			softShadowsSetting,
			msaaSetting,
			fogModeSetting,
			fogSliderSetting
		};
	}
	
	private static List<Setting> CreatePostProcessingSettings()
	{

		var brightnessSetting = new SliderSetting("Brightness")
		{
			min = -5f,
			max = 5f,
			onSave = v =>
			{
				if (volumeProfile.TryGet(out ColorAdjustments colorAdjustments))
				{
					colorAdjustments.postExposure.value = v;
				}
			},
			defaultValue = 0f,
		};
		var bloomSetting = new ToggleSetting("Bloom")
		{
			onSave = v =>
			{
				if (volumeProfile.TryGet(out Bloom bloom))
				{
					bloom.active = v;
				}
			},
			defaultValue = true,
		};
		var chromeSetting = new ToggleSetting("Chromatic Aberration")
		{
			onSave = v =>
			{
				if (volumeProfile.TryGet(out ChromaticAberration chromaticAberration))
				{
					chromaticAberration.active = v;
				}
			},
			defaultValue = true,
		};
		var depthOfFieldSetting = new ToggleSetting("Depth of Field")
		{
			onSave = v =>
			{
				if (volumeProfile.TryGet(out DepthOfField depthOfField))
				{
					depthOfField.active = v;
				}
			},
			defaultValue = true,
		};
		var filmGrainSetting = new ToggleSetting("Film Grain")
		{
			onSave = v =>
			{
				if (volumeProfile.TryGet(out FilmGrain filmGrain))
				{
					filmGrain.active = v;
				}
			},
			defaultValue = true,
		};
		var lensDistortionSetting = new ToggleSetting("Lens Distortion")
		{
			onSave = v =>
			{
				if (volumeProfile.TryGet(out LensDistortion lensDistortion))
				{
					lensDistortion.active = v;
				}
			},
			defaultValue = true,
		};
		var motionBlurSetting = new ToggleSetting("Motion Blur")
		{
			onSave = v =>
			{
				if (volumeProfile.TryGet(out MotionBlur motionBlur))
				{
					motionBlur.active = v;
				}
			},
			defaultValue = true,
		};
		var paniniProjectionSetting = new ToggleSetting("Panini Projection")
		{
			onSave = v =>
			{
				if (volumeProfile.TryGet(out PaniniProjection paniniProjection))
				{
					paniniProjection.active = v;
				}
			},
			defaultValue = true,
		};
		var vignetteSetting = new ToggleSetting("Vignette")
		{
			onSave = v =>
			{
				if (volumeProfile.TryGet(out Vignette vignette))
				{
					vignette.active = v;
				}
			},
			defaultValue = true,
		};

		DisplaySettings.Add(brightnessSetting);
		return new () 
		{
			bloomSetting,
			chromeSetting,
			depthOfFieldSetting,
			filmGrainSetting,
			lensDistortionSetting,
			motionBlurSetting,
			paniniProjectionSetting,
			vignetteSetting
		};
	}

	private static List<Setting> CreateAudioSettings()
	{
		var masterSetting = new SliderSetting("Master")
		{
			min = 0f,
			max = 1f,
			onSave = v => AudioListener.volume = v,
			defaultValue = 1f,
		};
		var sfxSetting = new SliderSetting("SFX")
		{
			min = 0f,
			max = 1f,
			onSave = v => AudioListener.volume = v,
			defaultValue = 1f,
		};
		var musicSetting = new SliderSetting("Music")
		{
			min = 0f,
			max = 1f,
			onSave = v => AudioListener.volume = v,
			defaultValue = 1f,
		};
		var dialogueSetting = new SliderSetting("Dialogue")
		{
			min = 0f,
			max = 1f,
			onSave = v => AudioListener.volume = v,
			defaultValue = 1f,
		};
		var muteSetting = new ToggleSetting("Mute")
		{
			onSave = v => AudioListener.pause = v,
			defaultValue = false,
		};
		return new()
		{
			masterSetting,
			sfxSetting,
			musicSetting,
			dialogueSetting,
			muteSetting,
		};
	}
}