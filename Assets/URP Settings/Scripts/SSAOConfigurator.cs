using System.Collections.Generic;
using System.Reflection;
using System;
using UnityEngine.Rendering.Universal;

public class SSAOConfigurator
{
	private readonly object _ssaoSettings;
	private readonly FieldInfo _fRadius;
	private ScriptableRendererFeature _ssaoFeature;

	public SSAOConfigurator()
	{
		static ScriptableRendererFeature findRenderFeature(Type type)
		{
			FieldInfo field = reflectField(typeof(ScriptableRenderer), "m_RendererFeatures");
			ScriptableRenderer renderer = UniversalRenderPipeline.asset.scriptableRenderer;
			var list = (List<ScriptableRendererFeature>)field.GetValue(renderer);
			foreach (ScriptableRendererFeature feature in list)
				if (feature.GetType() == type)
					return feature;
			throw new Exception($"Could not find instance of {type.AssemblyQualifiedName} in the renderer features list");
		}

		static FieldInfo reflectField(Type type, string name) =>
			type.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic) ??
			throw new Exception($"Could not reflect field [{type.AssemblyQualifiedName}].{name}");

		Type tSsaoFeature = Type.GetType("UnityEngine.Rendering.Universal.ScreenSpaceAmbientOcclusion, Unity.RenderPipelines.Universal.Runtime", true);
		FieldInfo fSettings = reflectField(tSsaoFeature, "m_Settings");
		_ssaoFeature = findRenderFeature(tSsaoFeature);
		_ssaoSettings = fSettings.GetValue(_ssaoFeature) ?? throw new Exception("ssaoFeature.m_Settings was null");
		_fRadius = reflectField(_ssaoSettings.GetType(), "Radius");
	}

	public float radius
	{
		get => (float)_fRadius.GetValue(_ssaoSettings);
		set => _fRadius.SetValue(_ssaoSettings, value);
	}

	public void SetActive(bool on) => _ssaoFeature.SetActive(on);
}