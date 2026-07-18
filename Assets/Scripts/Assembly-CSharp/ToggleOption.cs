using UnityEngine;

public class ToggleOption : OptionBehaviour
{
	public TextRenderer TitleText;

	public SpriteRenderer CheckMark;

	private bool oldValue;

	public void OnEnable()
	{
		TitleText.Text = DestroyableSingleton<TranslationController>.Instance.GetString(Title);
		GameOptionsData gameOptions = PlayerControl.GameOptions;
		StringNames title = Title;
		if (title == StringNames.GameRecommendedSettings)
		{
			CheckMark.enabled = gameOptions.isDefaults;
		}
		else
		{
			Debug.Log("Ono, unrecognized setting: " + Title);
		}
	}

	private void FixedUpdate()
	{
		bool flag = GetBool();
		if (oldValue != flag)
		{
			oldValue = flag;
			CheckMark.enabled = flag;
		}
	}

	public void Toggle()
	{
		CheckMark.enabled = !CheckMark.enabled;
		OnValueChanged(this);
	}

	public override bool GetBool()
	{
		return CheckMark.enabled;
	}
}
