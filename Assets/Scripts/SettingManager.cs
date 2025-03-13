using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using static SettingData;

public static class SettingManager
{
	public static Setting[] DisplaySettings { get; private set; }
	public static Setting[] GraphicsSettings { get; private set; }
	public static Setting[] PostProcessingSettings { get; private set; }
	public static Setting[] AudioSettings { get; private set; }

	// Display 
	private static FullScreenMode fullScreenMode = FullScreenMode.FullScreenWindow;
	private static List<Resolution> resolutions = new();
	private static Dictionary<string, List<RefreshRate>> legalRefreshRates = new();

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
	private static void Initialize()
	{
		DisplaySettings = new Setting[0];
		GraphicsSettings = new Setting[0];
		PostProcessingSettings = new Setting[0];
		AudioSettings = new Setting[0];
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

	static void Test()
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
	}
}
