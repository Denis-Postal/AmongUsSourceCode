using UnityEngine;
using UnityEngine.Events;

public class ShadowsSettingsManager : MonoBehaviour
{
	private const int LowQuality = 0;

	private const int MediumQuality = 1;

	private const int MaxQuality = 2;

	public SpriteRenderer LowButton;

	public SpriteRenderer MediumButton;

	public SpriteRenderer MaxButton;

	public GameObject LowButtonObject;

	public GameObject MediumButtonObject;

	public GameObject MaxButtonObject;

	public Color SelectedColor = new Color(0f, 1f, 0f, 1f);

	public Color UnselectedColor = Color.white;

	private int lastQuality = -1;

	private void Awake()
	{
		FindButtons(forceFromObjects: true);
		WireButtons();
		ApplySavedQuality();
	}

	private void OnEnable()
	{
		FindButtons(forceFromObjects: true);
		WireButtons();
		ApplySavedQuality();
	}

	private void Update()
	{
		int quality = SaveManager.ShadowQuality;
		if (quality != lastQuality)
		{
			ApplyQuality(quality, save: false);
		}
	}

	public void SetLow()
	{
		ApplyQuality(LowQuality, save: true);
	}

	public void SetMedium()
	{
		ApplyQuality(MediumQuality, save: true);
	}

	public void SetMax()
	{
		ApplyQuality(MaxQuality, save: true);
	}

	public void ChooseLow()
	{
		SetLow();
	}

	public void ChooseMedium()
	{
		SetMedium();
	}

	public void ChooseMax()
	{
		SetMax();
	}

	public void SelectLow()
	{
		SetLow();
	}

	public void SelectMedium()
	{
		SetMedium();
	}

	public void SelectMax()
	{
		SetMax();
	}

	public void ApplySavedQuality()
	{
		ApplyQuality(SaveManager.ShadowQuality, save: false);
	}

	private void ApplyQuality(int quality, bool save)
	{
		quality = Mathf.Clamp(quality, LowQuality, MaxQuality);
		SaveManager.ShadowQuality = quality;
		lastQuality = quality;
		UpdateButtonColors(quality);
		ShadowCamera.ApplySavedQualityToAll();
	}

	private void UpdateButtonColors(int quality)
	{
		FindButtons(forceFromObjects: true);
		SetButtonColor(LowButton, quality == LowQuality);
		SetButtonColor(MediumButton, quality == MediumQuality);
		SetButtonColor(MaxButton, quality == MaxQuality);
	}

	private void FindButtons(bool forceFromObjects)
	{
		if (forceFromObjects || !LowButton)
		{
			LowButton = FindButtonRenderer(LowButtonObject, "Low");
		}
		if (forceFromObjects || !MediumButton)
		{
			MediumButton = FindButtonRenderer(MediumButtonObject, "Medium");
		}
		if (forceFromObjects || !MaxButton)
		{
			MaxButton = FindButtonRenderer(MaxButtonObject, "Max");
		}
	}

	private void WireButtons()
	{
		WireButton(LowButtonObject, SetLow);
		WireButton(MediumButtonObject, SetMedium);
		WireButton(MaxButtonObject, SetMax);
	}

	private void WireButton(GameObject buttonObject, UnityAction action)
	{
		if (!buttonObject)
		{
			return;
		}
		PassiveButton passiveButton = buttonObject.GetComponent<PassiveButton>();
		if (!passiveButton)
		{
			passiveButton = buttonObject.GetComponentInChildren<PassiveButton>(true);
		}
		if (!passiveButton)
		{
			return;
		}
		passiveButton.OnClick.RemoveListener(action);
		passiveButton.OnClick.AddListener(action);
	}

	private SpriteRenderer FindButtonRenderer(GameObject buttonObject, string namePart)
	{
		if ((bool)buttonObject)
		{
			SpriteRenderer renderer = FindBackgroundRenderer(buttonObject.transform);
			if ((bool)renderer)
			{
				return renderer;
			}
			renderer = buttonObject.GetComponent<SpriteRenderer>();
			if ((bool)renderer)
			{
				return renderer;
			}
			return buttonObject.GetComponentInChildren<SpriteRenderer>(true);
		}
		return FindButton(namePart);
	}

	private SpriteRenderer FindBackgroundRenderer(Transform root)
	{
		Transform[] children = root.GetComponentsInChildren<Transform>(true);
		for (int i = 0; i < children.Length; i++)
		{
			if (children[i].name.ToLowerInvariant() == "background")
			{
				SpriteRenderer renderer = children[i].GetComponent<SpriteRenderer>();
				if ((bool)renderer)
				{
					return renderer;
				}
			}
		}
		return null;
	}

	private SpriteRenderer FindButton(string namePart)
	{
		Transform[] children = GetComponentsInChildren<Transform>(true);
		for (int i = 0; i < children.Length; i++)
		{
			if (children[i].name.ToLowerInvariant().Contains(namePart.ToLowerInvariant()))
			{
				SpriteRenderer renderer = FindBackgroundRenderer(children[i]);
				if ((bool)renderer)
				{
					return renderer;
				}
				renderer = children[i].GetComponent<SpriteRenderer>();
				if ((bool)renderer)
				{
					return renderer;
				}
				renderer = children[i].GetComponentInChildren<SpriteRenderer>(true);
				if ((bool)renderer)
				{
					return renderer;
				}
			}
		}
		return null;
	}

	private void SetButtonColor(SpriteRenderer button, bool selected)
	{
		if ((bool)button)
		{
			Color color = selected ? SelectedColor : UnselectedColor;
			button.color = color;
			ButtonRolloverHandler rollover = button.GetComponentInParent<ButtonRolloverHandler>();
			if ((bool)rollover)
			{
				rollover.Target = button;
				rollover.OutColor = color;
			}
		}
	}
}
