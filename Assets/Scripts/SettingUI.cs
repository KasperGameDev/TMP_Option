using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static SettingData;

public class SettingUI : MonoBehaviour
{
	private TextMeshProUGUI label;

	private void Awake()
	{
		label = GetComponentInChildren<TextMeshProUGUI>();
	}

	public void Setup(Setting setting)
	{
		foreach (Transform child in transform)
			child.gameObject.SetActive(false);

		label.gameObject.SetActive(true);
		label.SetText(setting.name);
		transform.name = $"Setting ({setting.name})";

		switch (setting)
		{
			case OptionSetting s: Setup(s); break;
			case ToggleSetting s: Setup(s); break;
			case SliderSetting s: Setup(s); break;
			case ButtonSetting s: Setup(s); break;
			default: Debug.LogError($"Doesn't support {setting.GetType()}"); return;
		}
	}

	private void Setup(OptionSetting setting)
	{
		var dropdown = transform.GetComponentInChildren<TMP_Dropdown>(true);
		setting.forceUpdate = UpdateDropdown;
		UpdateDropdown();

		void UpdateDropdown()
		{
			print(dropdown);
			print(string.Join(',', setting.options.Select(o => o.ToString())));
			dropdown.gameObject.SetActive(true);
			dropdown.ClearOptions();
			dropdown.AddOptions(setting.options.ToList());
			dropdown.value = setting.Get();
			dropdown.onValueChanged.AddListener(v => setting.Set(v));
		}
	}

	private void Setup(ToggleSetting setting)
	{
		var toggle = GetComponentInChildren<Toggle>(true);
		toggle.gameObject.SetActive(true);
		toggle.isOn = setting.Get();
		toggle.onValueChanged.AddListener(v => setting.Set(v));
	}

	private void Setup(SliderSetting setting)
	{
		var slider = GetComponentInChildren<Slider>(true);
		slider.gameObject.SetActive(true);
		slider.minValue = setting.min;
		slider.maxValue = setting.max;
		slider.value = setting.Get();
		slider.onValueChanged.AddListener(v => setting.Set(v));
	}

	private void Setup(ButtonSetting setting)
	{
		var button = GetComponentInChildren<Button>(true);
		button.gameObject.SetActive(true);
		button.onClick.AddListener(() => setting.Get().Invoke());
		button.GetComponentInChildren<TextMeshProUGUI>().SetText(setting.name);
	}
}