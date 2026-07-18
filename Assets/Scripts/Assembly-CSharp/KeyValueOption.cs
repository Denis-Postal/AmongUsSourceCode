using System.Collections.Generic;
using UnityEngine;

public class KeyValueOption : OptionBehaviour
{
	public TextRenderer TitleText;

	public TextRenderer ValueText;

	public List<KeyValuePair<string, int>> Values;

	private int Selected;

	private int oldValue = -1;

	public void OnEnable()
	{
		GameOptionsData gameOptions = PlayerControl.GameOptions;
		StringNames title = Title;
		if (title == StringNames.GameMapName)
		{
			Selected = gameOptions.MapId;
		}
		else
		{
			Debug.Log("Ono, unrecognized setting: " + Title);
		}
		TitleText.Text = DestroyableSingleton<TranslationController>.Instance.GetString(Title);
		ValueText.Text = Values[Selected].Key;
	}

	private void FixedUpdate()
	{
		if (oldValue != Selected)
		{
			oldValue = Selected;
			ValueText.Text = Values[Selected].Key;
		}
	}

	public void Increase()
	{
		Selected = Mathf.Clamp(Selected + 1, 0, Values.Count - 1);
		OnValueChanged(this);
	}

	public void Decrease()
	{
		Selected = Mathf.Clamp(Selected - 1, 0, Values.Count - 1);
		OnValueChanged(this);
	}

	public override int GetInt()
	{
		return Values[Selected].Value;
	}
}
