using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static SettingData;

public class SettingUI : MonoBehaviour
{
	[SerializeField] 
	private Button _resetButton;

	private TextMeshProUGUI _label;
	private Setting _setting;

	private void Awake()
	{
		_label = GetComponentInChildren<TextMeshProUGUI>();
	}

	public void Reset()
	{
		_setting?.SetToDefault();
	}

	public void Setup(Setting setting)
	{
		foreach (Transform child in transform)
		{
			child.gameObject.SetActive(false);
		}

		_setting = setting;
		_label.gameObject.SetActive(true);
		_label.SetText(setting.name);
		transform.name = $"Setting ({setting.name})";
		setting.forceUpdate = () => Setup(setting);

		switch (setting)
		{
			case OptionSetting s: Setup(s); break;
			case ToggleSetting s: Setup(s); break;
			case SliderSetting s: Setup(s); break;
			case ButtonSetting s: Setup(s); break;
			case IntSliderSetting s: Setup(s); break;
			default: Debug.LogError($"Doesn't support {setting.GetType()}"); return;
		}
	}

	private void Setup(OptionSetting setting)
	{
		var dropdown = transform.GetComponentInChildren<TMP_Dropdown>(true);
		dropdown.gameObject.SetActive(true);
		dropdown.ClearOptions();
		dropdown.AddOptions(setting.options.ToList());
		dropdown.value = setting.Get();
		dropdown.onValueChanged.AddListener(v => setting.Set(v));
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

		var label = slider.GetComponentInChildren<TextMeshProUGUI>();
		slider.onValueChanged.AddListener(v => label.SetText(v.ToString("F1")));
		slider.onValueChanged.AddListener(_ => _resetButton.gameObject.SetActive(true));
		label.SetText(setting.value.ToString("F1"));
	}

	private void Setup(IntSliderSetting setting)
	{
		var slider = GetComponentInChildren<Slider>(true);
		slider.gameObject.SetActive(true);
		slider.minValue = setting.min;
		slider.maxValue = setting.max;
		slider.value = setting.Get();
		slider.wholeNumbers = true;
		slider.onValueChanged.AddListener(v => setting.Set((int)v));

		var label = slider.GetComponentInChildren<TextMeshProUGUI>();
		slider.onValueChanged.AddListener(v => label.SetText(v.ToString()));
		slider.onValueChanged.AddListener(_ => _resetButton.gameObject.SetActive(true));
		label.SetText(setting.value.ToString());
	}

	private void Setup(ButtonSetting setting)
	{
		var button = GetComponentInChildren<Button>(true);
		button.gameObject.SetActive(true);
		button.onClick.AddListener(() => setting.Get().Invoke());
		button.GetComponentInChildren<TextMeshProUGUI>().SetText(setting.name);
	}
}