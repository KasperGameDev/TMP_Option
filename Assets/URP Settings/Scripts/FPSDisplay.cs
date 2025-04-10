using System;
using TMPro;
using UnityEngine;

public class FPSDisplay : MonoBehaviour
{
	[SerializeField] private Color syncedColor = Color.grey;
	[SerializeField] private Color normalColor = Color.white;

	private TextMeshProUGUI _fpsText;
	private int _frameCount;
	private int _totalFPS;

	private void Awake()
	{
		_fpsText = GetComponent<TextMeshProUGUI>();
	}

	private void Update()
	{
		_frameCount++;
		_totalFPS += (int)Math.Round(1f / Time.unscaledDeltaTime);

		if (_frameCount % 60 == 0)
		{
			_fpsText.text = $"{_totalFPS / _frameCount} FPS";
			_totalFPS = 0;
			_frameCount = 0;
		}

		if (QualitySettings.vSyncCount > 0)
		{
			_fpsText.color = syncedColor;
		}
		else
		{
			_fpsText.color = normalColor;
		}
	}
}