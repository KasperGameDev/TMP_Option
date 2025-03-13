using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using static SettingData;

public class SettingUI : MonoBehaviour
{
	private TextMeshProUGUI text;

	private void Awake()
	{
		text = GetComponentInChildren<TextMeshProUGUI>();
	}

	public void Setup(Setting setting)
	{
		text.SetText(setting.name);

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
		//var selector = GetComponentInChildren<Selector>();
		//selector.SetOptions(setting.options);
		//selector.SetValue(setting.Get());
		//selector.onSelectionChanged.AddListener(v => setting.Set(v));
	}

	private void Setup(ToggleSetting setting)
	{
		//var toggle = GetComponentInChildren<Toggle>();
		//toggle.SetValue(setting.Get());
		//toggle.onValueChanged.AddListener(v => setting.Set(v));
	}

	private void Setup(SliderSetting setting)
	{
		//var slider = GetComponentInChildren<Slider>();
		//slider.SetValue(setting.Get());
		//slider.onValueChanged.AddListener(v => setting.Set(v));
	}

	private void Setup(ButtonSetting setting)
	{
		//var button = GetComponentInChildren<Button>();
		//button.SetRed(setting.isRed);
		//button.SetValue(setting.Get(), setting.text);
	}
}