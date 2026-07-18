using UnityEngine;

public class OptionsMenuBehaviour : MonoBehaviour, ITranslatedText
{
	public SpriteRenderer Background;

	public SpriteRenderer JoystickButton;

	public SpriteRenderer TouchButton;

	public SlideBar JoystickSizeSlider;

	public FloatRange JoystickSizes = new FloatRange(0.5f, 1.5f);

	public SlideBar SoundSlider;

	public SlideBar MusicSlider;

	public ToggleButtonBehaviour PersonalizedAdsButton;

	public ToggleButtonBehaviour CensorChatButton;

	public bool Toggle = true;

	public TabGroup[] Tabs;

	public bool IsOpen => base.isActiveAndEnabled;

	public void OpenTabGroup(TabGroup selected)
	{
		selected.Open();
		for (int i = 0; i < Tabs.Length; i++)
		{
			TabGroup tabGroup = Tabs[i];
			if (!(tabGroup == selected))
			{
				tabGroup.Close();
			}
		}
	}

	private void Update()
	{
		if (Input.GetKeyUp(KeyCode.Escape))
		{
			Close();
		}
	}

	public void Start()
	{
		DestroyableSingleton<TranslationController>.Instance.ActiveTexts.Add(this);
	}

	public void OnDestroy()
	{
		DestroyableSingleton<TranslationController>.Instance.ActiveTexts.Remove(this);
	}

	public void ResetText()
	{
		JoystickButton.transform.parent.GetComponentInChildren<TextRenderer>().Text = DestroyableSingleton<TranslationController>.Instance.GetString(StringNames.SettingsMouseMode);
		TouchButton.transform.parent.GetComponentInChildren<TextRenderer>().Text = DestroyableSingleton<TranslationController>.Instance.GetString(StringNames.SettingsKeyboardMode);
		JoystickSizeSlider.gameObject.SetActive(value: false);
	}

	public void Open()
	{
		ResetText();
		if (base.gameObject.activeSelf)
		{
			if (Toggle)
			{
				GetComponent<TransitionOpen>().Close();
			}
		}
		else
		{
			OpenTabGroup(Tabs[0]);
			UpdateButtons();
			base.gameObject.SetActive(value: true);
		}
	}

	public void SetControlType(int i)
	{
		SaveManager.TouchConfig = i;
		UpdateButtons();
		if (DestroyableSingleton<HudManager>.InstanceExists)
		{
			DestroyableSingleton<HudManager>.Instance.SetTouchType(i);
		}
	}

	public void UpdateJoystickSize(SlideBar slider)
	{
		SaveManager.JoystickSize = JoystickSizes.Lerp(slider.Value);
		if (DestroyableSingleton<HudManager>.InstanceExists)
		{
			DestroyableSingleton<HudManager>.Instance.SetJoystickSize(SaveManager.JoystickSize);
		}
	}

	public void UpdateSfxVolume(SlideBar button)
	{
		SaveManager.SfxVolume = button.Value;
		SoundManager.Instance.ChangeSfxVolume(button.Value);
	}

	public void UpdateMusicVolume(SlideBar button)
	{
		SaveManager.MusicVolume = button.Value;
		SoundManager.Instance.ChangeMusicVolume(button.Value);
	}

	public void TogglePersonalizedAd()
	{
		switch (SaveManager.ShowAdsScreen & (ShowAdsState)127)
		{
		case ShowAdsState.NonPersonalized:
			SaveManager.ShowAdsScreen = ShowAdsState.Accepted;
			break;
		default:
			SaveManager.ShowAdsScreen = (ShowAdsState)129;
			break;
		case ShowAdsState.Purchased:
			break;
		}
		UpdateButtons();
	}

	public void ToggleCensorChat()
	{
		SaveManager.CensorChat = !SaveManager.CensorChat;
		UpdateButtons();
	}

	public void UpdateButtons()
	{
		if (SaveManager.TouchConfig == 0)
		{
			JoystickButton.color = new Color32(0, byte.MaxValue, 42, byte.MaxValue);
			TouchButton.color = Color.white;
			JoystickSizeSlider.enabled = true;
			JoystickSizeSlider.OnEnable();
		}
		else
		{
			JoystickButton.color = Color.white;
			TouchButton.color = new Color32(0, byte.MaxValue, 42, byte.MaxValue);
			JoystickSizeSlider.enabled = false;
			JoystickSizeSlider.OnDisable();
		}
		JoystickSizeSlider.Value = JoystickSizes.ReverseLerp(SaveManager.JoystickSize);
		SoundSlider.Value = SaveManager.SfxVolume;
		MusicSlider.Value = SaveManager.MusicVolume;
		CensorChatButton.UpdateText(SaveManager.CensorChat);
		if ((bool)PersonalizedAdsButton)
		{
			if (SaveManager.ShowAdsScreen.HasFlag(ShowAdsState.Purchased) || SaveManager.BoughtNoAds)
			{
				PersonalizedAdsButton.transform.parent.gameObject.SetActive(value: false);
			}
			else
			{
				PersonalizedAdsButton.UpdateText(!SaveManager.ShowAdsScreen.HasFlag(ShowAdsState.NonPersonalized));
			}
		}
	}

	public void Close()
	{
		base.gameObject.SetActive(value: false);
	}
}
