using UnityEngine;

public class StringOption : OptionBehaviour
{
	public TextRenderer TitleText;

	public TextRenderer ValueText;

	public string[] Values;

	public int Value;

	private int oldValue = -1;

	public void OnEnable()
	{
		GameOptionsData gameOptions = PlayerControl.GameOptions;
		StringNames title = Title;
		if (title == StringNames.GameKillDistance)
		{
			Value = gameOptions.KillDistance;
		}
		else
		{
			Debug.Log("Ono, unrecognized setting: " + Title);
		}
		TitleText.Text = DestroyableSingleton<TranslationController>.Instance.GetString(Title);
		ValueText.Text = Values[Value];
	}

	private void FixedUpdate()
	{
		if (oldValue != Value)
		{
			oldValue = Value;
			ValueText.Text = Values[Value];
		}
	}

	public void Increase()
	{
		Value = Mathf.Clamp(Value + 1, 0, Values.Length - 1);
		OnValueChanged(this);
	}

	public void Decrease()
	{
		Value = Mathf.Clamp(Value - 1, 0, Values.Length - 1);
		OnValueChanged(this);
	}

	public override int GetInt()
	{
		return Value;
	}
}
