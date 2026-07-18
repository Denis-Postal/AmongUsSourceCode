using UnityEngine;

public class ToggleButtonBehaviour : MonoBehaviour, ITranslatedText
{
	public StringNames BaseText;

	public TextRenderer Text;

	public SpriteRenderer Background;

	public ButtonRolloverHandler Rollover;

	private bool onState;

	public void Start()
	{
		DestroyableSingleton<TranslationController>.Instance.ActiveTexts.Add(this);
	}

	public void OnDestroy()
	{
		if (DestroyableSingleton<TranslationController>.InstanceExists)
		{
			DestroyableSingleton<TranslationController>.Instance.ActiveTexts.Remove(this);
		}
	}

	public void ResetText()
	{
		Text.Text = DestroyableSingleton<TranslationController>.Instance.GetString(BaseText) + ": " + DestroyableSingleton<TranslationController>.Instance.GetString(onState ? StringNames.SettingsOn : StringNames.SettingsOff);
	}

	public void UpdateText(bool on)
	{
		onState = on;
		Color color = (on ? new Color(0f, 1f, 14f / 85f, 1f) : Color.white);
		Background.color = color;
		ResetText();
		if ((bool)Rollover)
		{
			Rollover.OutColor = color;
		}
	}
}
