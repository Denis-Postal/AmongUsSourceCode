using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TranslationController : DestroyableSingleton<TranslationController>
{
	private static readonly StringNames[] SystemTypesToStringNames;

	private static readonly StringNames[] TaskTypesToStringNames;

	public TextAsset[] Languages;

	public TranslatedImageSet[] Images;

	public LanguageUnit CurrentLanguage;

	public List<ITranslatedText> ActiveTexts = new List<ITranslatedText>();

	static TranslationController()
	{
		SystemTypesToStringNames = SystemTypeHelpers.AllTypes.Select(delegate(SystemTypes t)
		{
			Enum.TryParse<StringNames>(t.ToString(), out var result);
			return result;
		}).ToArray();
		TaskTypesToStringNames = TaskTypesHelpers.AllTypes.Select(delegate(TaskTypes t)
		{
			Enum.TryParse<StringNames>(t.ToString(), out var result);
			return result;
		}).ToArray();
	}

	public override void Awake()
	{
		base.Awake();
		if (DestroyableSingleton<TranslationController>.Instance == this)
		{
			CurrentLanguage = new LanguageUnit(Languages[SaveManager.LastLanguage], Images[SaveManager.LastLanguage].Images);
		}
	}

	public void SetLanguage(TextAsset lang)
	{
		int num = Languages.IndexOf(lang);
		Debug.Log("Set language to " + num);
		SaveManager.LastLanguage = (uint)num;
		CurrentLanguage = new LanguageUnit(Languages[num], Images[num].Images);
		for (int i = 0; i < ActiveTexts.Count; i++)
		{
			ActiveTexts[i].ResetText();
		}
	}

	public Sprite GetImage(ImageNames id)
	{
		return CurrentLanguage.GetImage(id);
	}

	public string GetString(StringNames id, params object[] parts)
	{
		return CurrentLanguage.GetString(id, parts);
	}

	public string GetString(SystemTypes room)
	{
		return GetString(SystemTypesToStringNames[(uint)room]);
	}

	public string GetString(TaskTypes task)
	{
		return GetString(TaskTypesToStringNames[(byte)task]);
	}

	internal static uint SelectDefaultLanguage()
	{
		try
		{
			switch (Application.systemLanguage)
			{
			case SystemLanguage.Portuguese:
				return 2u;
			case SystemLanguage.Spanish:
				return 1u;
			}
		}
		catch
		{
		}
		return 0u;
	}
}
