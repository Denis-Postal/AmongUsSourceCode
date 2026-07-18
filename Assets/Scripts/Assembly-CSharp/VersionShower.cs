using UnityEngine;

public class VersionShower : MonoBehaviour
{
	public TextRenderer text;

	public void Start()
	{
		this.text.Text = "v" + StripTrailingLetters(Application.version);
		Screen.sleepTimeout = -1;
	}

	private static string StripTrailingLetters(string version)
	{
		if (string.IsNullOrEmpty(version))
		{
			return string.Empty;
		}
		int length = version.Length;
		while (length > 0 && char.IsLetter(version[length - 1]))
		{
			length--;
		}
		return version.Substring(0, length);
	}
}
