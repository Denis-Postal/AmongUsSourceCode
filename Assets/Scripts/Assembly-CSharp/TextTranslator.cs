using UnityEngine;

[RequireComponent(typeof(TextRenderer))]
public class TextTranslator : MonoBehaviour, ITranslatedText
{
	public StringNames TargetText;

	public void ResetText()
	{
		GetComponent<TextRenderer>().Text = DestroyableSingleton<TranslationController>.Instance.GetString(TargetText);
	}

	public void Start()
	{
		DestroyableSingleton<TranslationController>.Instance.ActiveTexts.Add(this);
		ResetText();
	}

	public void OnDestroy()
	{
		if (DestroyableSingleton<TranslationController>.InstanceExists)
		{
			try
			{
				DestroyableSingleton<TranslationController>.Instance.ActiveTexts.Remove(this);
			}
			catch
			{
			}
		}
	}
}
