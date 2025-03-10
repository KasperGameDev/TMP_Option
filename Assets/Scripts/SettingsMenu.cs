using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class SettingsMenu : MonoBehaviour
{
	public Volume volume;

	private void Start()
	{
		volume.profile.TryGet(out Bloom bloom);
		bloom.active = true;

		RenderSettings.fogMode = FogMode.Linear;
		RenderSettings.fogStartDistance = 10f;
		RenderSettings.fogEndDistance = 100f;
	}
}
