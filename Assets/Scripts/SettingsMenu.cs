using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using ShadowResolution = UnityEngine.Rendering.Universal.ShadowResolution;

public class SettingsMenu : MonoBehaviour
{
	public Volume volume;
	public UniversalRenderPipelineAsset urpAsset;

	private void Start()
	{
		volume.profile.TryGet(out Bloom bloom);
		bloom.active = true;

		Application.targetFrameRate = 60;
		int displayCount = Display.displays.Length;
		if (displayCount > 1)
		{
			// Set the game to the second monitor (index 1)
			Display.displays[1].Activate();
		}
				
		RenderSettings.fogMode = FogMode.Linear;
		RenderSettings.fogStartDistance = 1f;
		RenderSettings.fogEndDistance = 1000f;

		UniversalRenderPipelineAsset data = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;

		if (data)
		{
			data.shadowCascadeCount = 2;
			data.shadowDistance = 150f;
			UnityGraphicsHack.SoftShadowsEnabled = false;
			UnityGraphicsHack.MainLightShadowResolution = ShadowResolution._256;
			UnityGraphicsHack.AdditionalLightShadowResolution = ShadowResolution._256;
			data.msaaSampleCount = 4;
		}

		if (volume.profile.TryGet(out ColorAdjustments colorAdjustments))
		{
			// Adjust the brightness (Exposure)
			colorAdjustments.postExposure.value = 1.5f; 
		}
	}
}

/*
 https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@14.0/manual/quality/quality-settings-through-code.html
 */