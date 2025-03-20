using System;
using UnityEngine;

public class SettingData : MonoBehaviour
{
	public abstract class Setting
	{
		public string name;
		public Action forceUpdate;

		public Setting(string name)
		{
			this.name = name;
		}

		public abstract void Apply();
	}

	public class Setting<T> : Setting
	{
		public Action<T> onSave;
		public T value;
		public T defaultValue;

		public Setting(string name) : base(name) { }

		public T Get()
		{
			if (!PlayerPrefs.HasKey(name))
			{
				SetToDefault();
			}

			return value;
		}
		public virtual void Set(T value)
		{
			this.value = value;
			onSave?.Invoke(value);
		}
		public void SetToDefault()
		{
			Set(defaultValue);
		}
		public override void Apply()
		{
			value = Get();
			Set(value);
		}
	}

	public class OptionSetting : Setting<int>
	{
		public string[] options;

		public OptionSetting(string name) : base(name)
		{
			value = PlayerPrefs.GetInt(name);
		}
		public override void Set(int value)
		{
			PlayerPrefs.SetInt(name, value);
			base.Set(value);
		}
	}

	public class ToggleSetting : Setting<bool>
	{
		public ToggleSetting(string name) : base(name)
		{
			value = PlayerPrefs.GetInt(name) == 1;
		}
		public override void Set(bool value)
		{
			PlayerPrefs.SetInt(name, value ? 1 : 0);
			base.Set(value);
		}
	}

	public class SliderSetting : Setting<float>
	{
		public float min = 0.0f, max = 1.0f;

		public SliderSetting(string name) : base(name)
		{
			value = PlayerPrefs.GetFloat(name);
		}
		public override void Set(float value)
		{
			value = Mathf.Clamp(value, min, max);
			PlayerPrefs.SetFloat(name, value);
			base.Set(value);
		}
	}

	public class IntSliderSetting : Setting<int>
	{
		public int min = 0, max = 10;

		public IntSliderSetting(string name) : base(name)
		{
			value = PlayerPrefs.GetInt(name);
		}
		public override void Set(int value)
		{
			value = Mathf.Clamp(value, min, max);
			PlayerPrefs.SetFloat(name, value);
			base.Set(value);
		}
	}

	public class ButtonSetting : Setting<Action>
	{
		public string text;

		public ButtonSetting(string name) : base(name)
		{
			value = defaultValue;
		}
		public override void Set(Action value)
		{
			base.Set(value);
			onSave = null;
		}
		public override void Apply() { }
	}
}
