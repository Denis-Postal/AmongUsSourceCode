using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class ImageTranslator : MonoBehaviour, ITranslatedText
{
	public ImageNames TargetImage;

	public void ResetText()
	{
		GetComponent<SpriteRenderer>().sprite = DestroyableSingleton<TranslationController>.Instance.GetImage(TargetImage);
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
			DestroyableSingleton<TranslationController>.Instance.ActiveTexts.Remove(this);
		}
	}
}
