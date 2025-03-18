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
	public static Dictionary<string, List<Setting>> Settings { get; private set; } = new();

	// State
	private static Resolution curResolution;
	private static RefreshRate curRefreshRate;
	private static FullScreenMode fullScreenMode = FullScreenMode.FullScreenWindow;
	private static VolumeProfile volumeProfile;

	// Data
	private static List<Resolution> resolutions = new();
	private static Dictionary<string, List<RefreshRate>> legalRefreshRates = new();
	private static int[] fpsMaxTargets = new[] { 30, 60, 90, 120, 144, 165, 180, 240, -1 };

	#region Loading
	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
	private static void Initialize()
	{
		SceneManager.sceneLoaded += (_,_) => CreateSettings();
	}

	private static void Reset()
	{
		Settings.Clear();
		volumeProfile = null;
	}

	public static void CreateSettings()
	{
		Reset();
		FindVolume();
		CreateGeneralSettings();
		CreateDisplaySettings();
		CreateGraphicsSettings();
		CreatePostProcessingSettings();
		CreateAudioSettings();
		LoadSave();
	}

	private static void LoadSave()
	{
		foreach (var settingList in Settings.Values)
		{
			foreach (var setting in settingList)
			{
				setting.forceUpdate?.Invoke();
				setting.Apply();
			}
		}
	}
	#endregion

	#region Create Settings
	private static void CreateGeneralSettings()
	{
		var audioLanguageSetting = new OptionSetting("Audio Language")
		{
			options = new string[] { "English", "Danish", "Corr" },
			onSave = i => { },
		};
		var subtitleLanguageSetting = new OptionSetting("Subtitle Language")
		{
			options = new string[] { "English", "Danish", "Corr" },
			onSave = i => { },
		};
		var resetSetting = new ButtonSetting("Reset")
		{
			text = "Reset",
			defaultValue = () =>
			{
				PlayerPrefs.DeleteAll();
				LoadSave();
			},
		};

		Settings["General"] = new()
		{
			audioLanguageSetting,
			subtitleLanguageSetting,
			resetSetting,
		};
	}

	private static void CreateDisplaySettings()
	{
		QualitySettings.vSyncCount = 0;
		UpdateResolutionData();

		// Get current resolution
		var idx = PlayerPrefs.HasKey("Resolution") ? PlayerPrefs.GetInt("Resolution") : resolutions.Count - 1;
		idx = Mathf.Clamp(idx, 0, resolutions.Count - 1);
		curResolution = resolutions[idx];

		// Get current display layout
		List<DisplayInfo> displayLayout = new List<DisplayInfo>();
		Screen.GetDisplayLayout(displayLayout);
		int displayCount = displayLayout.Count;
		int displayIndex = Display.displays.ToList().FindIndex(d => d.active);

		// Create 'FPS Max' options
		var fpsMaxOptions = fpsMaxTargets.Select(i => $"{i}").ToArray();
		fpsMaxOptions[fpsMaxOptions.Length - 1] = "Unlimited";

		var refreshRateSetting = new OptionSetting("Refresh Rate")
		{
			options = GetCurrentRefreshRates().Select(r => r.value.ToString("F2")).ToArray(),
			onSave = i =>
			{
				var key = ResolutionToKey(curResolution);
				curRefreshRate = legalRefreshRates[key][i];
				SetResolution();
			},
			defaultValue = GetCurrentRefreshRates().Count - 1,
		};

		var resolutionSetting = new OptionSetting("Resolution")
		{
			options = resolutions.Select(r => $"{r.width}x{r.height}").ToArray(),
			onSave = i =>
			{
				if (i < 0)
				{
					i = resolutions.Count - 1;
				}
				i = Mathf.Clamp(i, 0, resolutions.Count - 1);

				curResolution = resolutions[i];
				curRefreshRate = GetCurrentRefreshRates().Last();
				SetResolution();
				UpdateRefreshRateSetting();
			},
			defaultValue = resolutions.Count - 1,
		};

		var displaySetting = new OptionSetting("Monitor")
		{
			options = Enumerable.Range(1, displayCount).Select(i => $"{i}").ToArray(),
			onSave = async i =>
			{
				await MoveWindowTask(i);
				UpdateResolutionData();
				UpdateResolutionSetting();	
				UpdateRefreshRateSetting();
			},
			defaultValue = displayIndex,
		};

		var fullscreenSetting = new OptionSetting("Fullscreen Mode")
		{
			options = Enum.GetNames(typeof(FullScreenMode)),
			onSave = v =>
			{
				fullScreenMode = (FullScreenMode)v;
				SetResolution();
			},
			defaultValue = (int)FullScreenMode.FullScreenWindow,
		};

		var vSyncSetting = new ToggleSetting("VSync")
		{
			onSave = v => QualitySettings.vSyncCount = v ? 1 : 0,
			defaultValue = true,
		};

		var fpsMaxSetting = new OptionSetting("FPS Max")
		{
			options = fpsMaxOptions,
			onSave = i =>
			{
				var targetFPS = fpsMaxTargets[i];
				Application.targetFrameRate = targetFPS;
				vSyncSetting.Set(false);
				vSyncSetting.forceUpdate?.Invoke();
			},
			defaultValue = fpsMaxOptions.Length - 1,
		};

		Settings["Display"] = new()
		{
			fullscreenSetting,
			resolutionSetting,
			refreshRateSetting,
			displaySetting,
			fpsMaxSetting,
			vSyncSetting,
		};

		if (volumeProfile.TryGet(out ColorAdjustments colorAdjustments))
		{
			var brightnessSetting = new SliderSetting("Brightness")
			{
				min = -5f,
				max = 5f,
				onSave = v =>
				{
					colorAdjustments.postExposure.value = v;
				},
				defaultValue = colorAdjustments.postExposure.value,
			};

			Settings["Display"].Add(brightnessSetting);
		}
	
		void UpdateRefreshRateSetting()
		{
			refreshRateSetting.options = GetCurrentRefreshRates().Select(r => r.value.ToString("F2")).ToArray();
			refreshRateSetting.Set(GetCurrentRefreshRates().Count - 1);
			refreshRateSetting.forceUpdate?.Invoke();
		}
		void UpdateResolutionSetting()
		{
			resolutionSetting.options = resolutions.Select(r => $"{r.width}x{r.height}").ToArray();
			resolutionSetting.Set(resolutions.Count - 1);
			resolutionSetting.forceUpdate?.Invoke();
		}
		List<RefreshRate> GetCurrentRefreshRates()
		{
			var key = ResolutionToKey(curResolution);
			return legalRefreshRates[key];
		}
		void SetResolution()
		{
			Screen.SetResolution(
				curResolution.width,
				curResolution.height,
				fullScreenMode,
				curRefreshRate);
		}
	}

	private static void CreateGraphicsSettings()
	{
		UniversalRenderPipelineAsset data = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;

		if (!data)
		{
			Debug.LogError("URP Asset couldn't be found!");
			return;
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
			onSave = v => RenderSettings.fogMode = (FogMode)(v + 1),
			defaultValue = (int)FogMode.Linear,
		};

		var fogSliderSetting = new SliderSetting("Fog Distance")
		{
			min = 100f,
			max = 1000f,
			onSave = v => RenderSettings.fogEndDistance = v,
			defaultValue = 1000f,
		};

		Settings["Graphics"] = new()
		{
			shadowResolutionSetting,
			shadowDistanceSetting,
			softShadowsSetting,
			msaaSetting,
			fogModeSetting,
			fogSliderSetting,
		};
	}

	private static void CreatePostProcessingSettings()
	{
		if (volumeProfile == null)
		{
			Debug.LogError("Couldn't create post processing settings, because there is no available volume.");
			return;
		}

		var bloomSetting = CreatePostProcessingSetting<Bloom>();
		var chromeSetting = CreatePostProcessingSetting<ChromaticAberration>();
		var depthOfFieldSetting = CreatePostProcessingSetting<DepthOfField>();
		var filmGrainSetting = CreatePostProcessingSetting<FilmGrain>();
		var lensDistortionSetting = CreatePostProcessingSetting<LensDistortion>();
		var motionBlurSetting = CreatePostProcessingSetting<MotionBlur>();
		var paniniProjectionSetting = CreatePostProcessingSetting<PaniniProjection>();
		var vignetteSetting = CreatePostProcessingSetting<Vignette>();

		Settings["Post Processing"] = new List<Setting>()
		{
			bloomSetting,
			chromeSetting,
			depthOfFieldSetting,
			filmGrainSetting,
			lensDistortionSetting,
			motionBlurSetting,
			paniniProjectionSetting,
			vignetteSetting
		}.Where(s => s != null).ToList();
	}

	private static void CreateAudioSettings()
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

		Settings["Audio"] = new()
		{
			masterSetting,
			sfxSetting,
			musicSetting,
			dialogueSetting,
			muteSetting,
		};
	}

	public static Setting CreatePostProcessingSetting<T>(string name = "") where T : VolumeComponent
	{
		T component;

		if (volumeProfile == null)
		{
			return null;
		}

		if (!volumeProfile.TryGet(out component))
		{
			return null;
		}

		if (string.IsNullOrEmpty(name))
		{
			name = typeof(T).Name;
		}

		return new ToggleSetting(name)
		{
			onSave = v => component.active = v,
			defaultValue = component.active,
		};
	}
	#endregion

	#region Helpers
	private static void FindVolume()
	{
		Volume volume = null;

		for (int i = 0; i < SceneManager.sceneCount; i++)
		{
			volume = (Volume)GameObject.FindFirstObjectByType(typeof(Volume));
			if (volume != null)
				break;
		}

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

	private static async Task MoveWindowTask(int index)
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

			curResolution = new Resolution();
			curRefreshRate = display.refreshRate;
			curResolution.width = display.width;
			curResolution.height = display.height;
			curResolution.refreshRateRatio = display.refreshRate;

			Screen.SetResolution(display.width, display.width, fullScreenMode, display.refreshRate);
		}
		else
		{
			await Task.CompletedTask;
		}
	}
	
	private static string ResolutionToKey(Resolution res)
	{
		return $"{res.width}x{res.height}";
	}
	#endregion
}