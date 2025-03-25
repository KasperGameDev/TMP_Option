using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MainMenu : MonoBehaviour
{
	[SerializeField]
	private GameObject _settingsMenu;

	public void OpenMainMenu()
	{
		_settingsMenu.SetActive(false);
		gameObject.SetActive(true);
	}

	public void OpenSettings()
	{
		_settingsMenu.SetActive(true);
		gameObject.SetActive(false);
	}

	public void Quit()
	{
		Application.Quit();
	}
}
