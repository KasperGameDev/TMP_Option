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
using System.Text.RegularExpressions;

public static class SettingManager
{
	public static Dictionary<string, List<Setting>> Settings { get; private set; } = new();

	// State
	private static Resolution _currentResolution;
	private static RefreshRate _currentRefreshRate;
	private static FullScreenMode _fullScreenMode = FullScreenMode.FullScreenWindow;
	private static VolumeProfile _volumeProfile;

	// Data
	private static List<Resolution> _resolutions = new();
	private static Dictionary<string, List<RefreshRate>> _legalRefreshRates = new();
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
		_volumeProfile = null;
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
		var languageSetting = new OptionSetting("Language")
		{
			options = new[] { "English", "Danish" },
			defaultValue = 0,
			onSave = i => { },
			//options = LocalizationSettings.AvailableLocales.Locales.Select(l => Regex.Replace(l.LocaleName, @"\(.*\)", "").Trim()).ToArray(),
			//onSave = i => { LocalizationSettings.SelectedLocale = LocalizationSettings.AvailableLocales.Locales[i]; },
			//defaultValue = LocalizationSettings.AvailableLocales.Locales.IndexOf(LocalizationSettings.ProjectLocale),
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
			languageSetting,
			resetSetting,
		};
	}

	private static void CreateDisplaySettings()
	{
		QualitySettings.vSyncCount = 0;
		UpdateResolutionData();

		// Get current resolution
		var idx = PlayerPrefs.HasKey("Resolution") ? PlayerPrefs.GetInt("Resolution") : _resolutions.Count - 1;
		idx = Mathf.Clamp(idx, 0, _resolutions.Count - 1);
		_currentResolution = _resolutions[idx];

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
				var key = ResolutionToKey(_currentResolution);
				_currentRefreshRate = _legalRefreshRates[key][i];
				SetResolution();
			},
			defaultValue = GetCurrentRefreshRates().Count - 1,
		};

		var resolutionSetting = new OptionSetting("Resolution")
		{
			options = _resolutions.Select(r => $"{r.width}x{r.height}").ToArray(),
			onSave = i =>
			{
				if (i < 0)
				{
					i = _resolutions.Count - 1;
				}
				i = Mathf.Clamp(i, 0, _resolutions.Count - 1);

				_currentResolution = _resolutions[i];
				_currentRefreshRate = GetCurrentRefreshRates().Last();
				SetResolution();
				UpdateRefreshRateSetting();
			},
			defaultValue = _resolutions.Count - 1,
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
			options = 
				Enum.GetNames(typeof(FullScreenMode))
				.Select(n => Regex.Replace(n, "[A-Z]", " $0").Trim())
				.ToArray(),
			onSave = v =>
			{
				_fullScreenMode = (FullScreenMode)v;
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
				//vSyncSetting.Set(false);
				//vSyncSetting.forceUpdate?.Invoke();
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

		if (_volumeProfile.TryGet(out LiftGammaGain lgg))
		{
			var brightnessSetting = new SliderSetting("Gamma")
			{
				min = -1f,
				max = 1f,
				onSave = v =>
				{
					var vector = lgg.gamma.value;
					vector.w = v;
					lgg.gamma.Override(vector);
				},
				defaultValue = 0.0f,
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
			resolutionSetting.options = _resolutions.Select(r => $"{r.width}x{r.height}").ToArray();
			resolutionSetting.Set(_resolutions.Count - 1);
			resolutionSetting.forceUpdate?.Invoke();
		}
		List<RefreshRate> GetCurrentRefreshRates()
		{
			var key = ResolutionToKey(_currentResolution);
			return _legalRefreshRates[key];
		}
		void SetResolution()
		{
			Screen.SetResolution(
				_currentResolution.width,
				_currentResolution.height,
				_fullScreenMode,
				_currentRefreshRate);
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

		var textureResolutionSetting = new OptionSetting("Texture Resolution")
		{
			options = new[] { "Very High", "High", "Medium", "Low", "Very Low" },
			defaultValue = QualitySettings.globalTextureMipmapLimit,
			onSave = v => QualitySettings.globalTextureMipmapLimit = v,
		};

		var shadowResolutionSetting = new OptionSetting("Shadow Resolution")
		{
			options = new[] { "Very Low", "Low", "Medium", "High", "Very High" }, //Enum.GetNames(typeof(ShadowResolution)).Select(r => $"{r}".Substring(1)).ToArray(),
			onSave = v =>
			{
				UnityGraphicsHack.MainLightShadowResolution = shadowRes[v];
				UnityGraphicsHack.AdditionalLightShadowResolution = shadowRes[v];
			},
			defaultValue = shadowRes.IndexOf(ShadowResolution._4096),
		};

		var shadowDistanceSetting = new SliderSetting("Shadow Distance")
		{
			min = 150f,
			max = 450f,
			onSave = v => data.shadowDistance = v,
			defaultValue = 450f,
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
			max = 500f,
			onSave = v => RenderSettings.fogEndDistance = v,
			defaultValue = 300f,
		};

		Settings["Graphics"] = new()
		{
			textureResolutionSetting,
			shadowResolutionSetting,
			shadowDistanceSetting,
			softShadowsSetting,
			msaaSetting,
			//fogModeSetting, // this doesn't really make sense to include tbh
			fogSliderSetting,
		};
	}

	private static void CreatePostProcessingSettings()
	{
		if (_volumeProfile == null)
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

		if (_volumeProfile == null)
		{
			return null;
		}

		if (!_volumeProfile.TryGet(out component))
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
			{
				break;
			}
		}

		if (volume != null)
		{
			_volumeProfile = volume.profile;

			if (_volumeProfile != null && volume.profile != volume)
			{
				Debug.LogWarning("Volume profile changed!");
				_volumeProfile = volume.profile;
			}
		}
		else
		{
			Debug.LogError("Volume could not be found!");
		}
	}

	private static void UpdateResolutionData()
	{
		_resolutions.Clear();
		_legalRefreshRates.Clear();
		foreach (var r in Screen.resolutions)
		{
			var k = $"{r.width}x{r.height}";

			if (!_legalRefreshRates.ContainsKey(k))
			{
				_resolutions.Add(r);
				_legalRefreshRates.Add(k, new());
			}

			_legalRefreshRates[k].Add(r.refreshRateRatio);
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

			_currentResolution = new Resolution();
			_currentRefreshRate = display.refreshRate;
			_currentResolution.width = display.width;
			_currentResolution.height = display.height;
			_currentResolution.refreshRateRatio = display.refreshRate;

			Screen.SetResolution(display.width, display.width, _fullScreenMode, display.refreshRate);
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


//https://docs.unity3d.com/ScriptReference/QualitySettings-globalTextureMipmapLimit.html