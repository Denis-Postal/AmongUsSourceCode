using UnityEngine;

public class LanguageSetter : MonoBehaviour
{
	public LanguageButton ButtonPrefab;

	public Scroller ButtonParent;

	public float ButtonStart = 0.5f;

	public float ButtonHeight = 0.5f;

	private LanguageButton[] AllButtons;

	public void Start()
	{
		TextAsset[] languages = DestroyableSingleton<TranslationController>.Instance.Languages;
		Vector3 localPosition = new Vector3(0f, ButtonStart, -1f);
		AllButtons = new LanguageButton[languages.Length];
		for (int i = 0; i < languages.Length; i++)
		{
			LanguageButton button = Object.Instantiate(ButtonPrefab, ButtonParent.Inner);
			AllButtons[i] = button;
			button.Language = languages[i];
			button.Title.Text = languages[i].name;
			if (i == SaveManager.LastLanguage)
			{
				button.Title.Color = Color.green;
			}
			button.Button.OnClick.AddListener(delegate
			{
				SetLanguage(button);
			});
			button.transform.localPosition = localPosition;
			localPosition.y -= ButtonHeight;
		}
		ButtonParent.YBounds.max = Mathf.Max(0f, 0f - localPosition.y - ButtonStart * 2f);
	}

	public void SetLanguage(LanguageButton selected)
	{
		for (int i = 0; i < AllButtons.Length; i++)
		{
			AllButtons[i].Title.Color = Color.white;
		}
		selected.Title.Color = Color.green;
		DestroyableSingleton<TranslationController>.Instance.SetLanguage(selected.Language);
	}
}
