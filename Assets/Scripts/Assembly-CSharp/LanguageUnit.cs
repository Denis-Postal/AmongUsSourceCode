using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class LanguageUnit
{
	public bool IsEnglish;

	private Dictionary<StringNames, string> AllStrings = new Dictionary<StringNames, string>();

	private Dictionary<ImageNames, Sprite> AllImages = new Dictionary<ImageNames, Sprite>();

	public LanguageUnit(TextAsset data, ImageData[] images)
	{
		for (int i = 0; i < images.Length; i++)
		{
			ImageData imageData = images[i];
			AllImages.Add(imageData.Name, imageData.Sprite);
		}
		using (StringReader stringReader = new StringReader(data.text))
		{
			for (string text = stringReader.ReadLine(); text != null; text = stringReader.ReadLine())
			{
				if (text.Length != 0)
				{
					int num = text.IndexOf(',');
					if (num < 0)
					{
						Debug.LogWarning("Couldn't parse: " + text);
					}
					else
					{
						string text2 = text.Substring(0, num);
						if (!Enum.TryParse<StringNames>(text2, out var result))
						{
							Debug.LogWarning("Couldn't parse: " + text2);
						}
						else
						{
							string value = UnescapeCodes(text.Substring(num + 1));
							AllStrings.Add(result, value);
						}
					}
				}
			}
		}
	}

	public static string UnescapeCodes(string src)
	{
		if (src.IndexOf("\\n") < 0)
		{
			return src;
		}
		return src.Replace("\\n", "\n");
	}

	public string GetString(StringNames stringId, params object[] parts)
	{
		if (AllStrings.TryGetValue(stringId, out var value))
		{
			if (parts.Length != 0)
			{
				return string.Format(value, parts);
			}
			return value;
		}
		return "STRMISS";
	}

	public Sprite GetImage(ImageNames id)
	{
		AllImages.TryGetValue(id, out var value);
		return value;
	}
}
