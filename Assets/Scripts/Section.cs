using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static SettingData;

public class Section : MonoBehaviour
{
	public string Title { get; set; }

	[SerializeField] 
	private SettingUI settingPrefab;

	public void Populate(IEnumerable<Setting> settings)
	{
		ClearSettings();
		CreateSettings(settings);
	}

	private void ClearSettings()
	{
		foreach (Transform child in transform)
			Destroy(child.gameObject);
	}

	private void CreateSettings(IEnumerable<Setting> settings)
	{
		foreach (var setting in settings)
		{
			var ui = Instantiate(settingPrefab, transform); 
			ui.Setup(setting);
		}
	}
}
