using System;
using System.Collections;
using System.Globalization;
using UnityEngine;
using UnityEngine.Networking;

public class AnnouncementPopUp : MonoBehaviour
{
	private const string DefaultGithubRawUrl = "https://raw.githubusercontent.com/Denis-Postal/Classic-Among-Us/refs/heads/main/Announcement.txt";

	public string GithubRawUrl = DefaultGithubRawUrl;

	public TextRenderer AnnounceText;

	public int MaxCharsPerLine = 72;

	private Coroutine fetchRoutine;

	public IEnumerator Init()
	{
		yield break;
	}

	private void OnEnable()
	{
		StartFetch();
	}

	public IEnumerator Show()
	{
		base.gameObject.SetActive(value: true);
		StartFetch();
		while (base.gameObject.activeSelf)
		{
			yield return null;
		}
	}

	public void Close()
	{
		base.gameObject.SetActive(value: false);
	}

	private void StartFetch()
	{
		if (fetchRoutine != null)
		{
			StopCoroutine(fetchRoutine);
		}
		fetchRoutine = StartCoroutine(FetchNews());
	}

	private IEnumerator FetchNews()
	{
		SetAnnouncementText("Fetching...");
		string sourceUrl = string.IsNullOrEmpty(GithubRawUrl) ? DefaultGithubRawUrl : GithubRawUrl;
		string url = BuildNoCacheUrl(sourceUrl);
		if (string.IsNullOrEmpty(url))
		{
			SetAnnouncementText("News temporarily unavailable");
			fetchRoutine = null;
			yield break;
		}
		Debug.Log("Requesting announcement from GitHub raw: " + url);
		using (UnityWebRequest request = UnityWebRequest.Get(url))
		{
			request.timeout = 10;
			request.SetRequestHeader("Cache-Control", "no-cache, no-store, must-revalidate");
			request.SetRequestHeader("Pragma", "no-cache");
			request.SetRequestHeader("Expires", "0");
			UnityWebRequestAsyncOperation operation = request.SendWebRequest();
			float deadline = Time.realtimeSinceStartup + 12f;
			while (!operation.isDone && Time.realtimeSinceStartup < deadline)
			{
				yield return null;
			}
			if (!operation.isDone)
			{
				request.Abort();
				Debug.LogWarning("Announcement request timed out url=" + sourceUrl);
				SetAnnouncementText("News temporarily unavailable");
				fetchRoutine = null;
				yield break;
			}
			if (request.isNetworkError || request.isHttpError)
			{
				Debug.LogWarning("Announcement request failed: " + request.error + " url=" + sourceUrl);
				SetAnnouncementText("News temporarily unavailable");
				fetchRoutine = null;
				yield break;
			}
			string rawText = request.downloadHandler.text;
			rawText = rawText.Replace("\\n", "\n");
			SetAnnouncementText(WrapText(rawText, MaxCharsPerLine));
		}
		fetchRoutine = null;
	}

	private void SetAnnouncementText(string text)
	{
		if (!AnnounceText)
		{
			return;
		}
		AnnounceText.Text = text;
		AnnounceText.RefreshMesh();
	}

	private static string BuildNoCacheUrl(string baseUrl)
	{
		if (string.IsNullOrEmpty(baseUrl))
		{
			return string.Empty;
		}
		string separator = baseUrl.Contains("?") ? "&" : "?";
		return baseUrl + separator + "t=" + DateTime.UtcNow.Ticks.ToString(CultureInfo.InvariantCulture);
	}

	private static string WrapText(string text, int lineLength)
	{
		if (string.IsNullOrEmpty(text))
		{
			return string.Empty;
		}
		string[] lines = text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
		string result = string.Empty;
		for (int i = 0; i < lines.Length; i++)
		{
			string[] words = lines[i].Split(' ');
			string line = string.Empty;
			for (int j = 0; j < words.Length; j++)
			{
				string word = words[j];
				if ((line + word).Length > lineLength)
				{
					result += line.TrimEnd() + "\r\n";
					line = string.Empty;
				}
				line += word + " ";
			}
			result += line.TrimEnd() + "\r\n";
		}
		return result.TrimEnd('\r', '\n');
	}
}
