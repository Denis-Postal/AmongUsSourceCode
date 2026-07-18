using System.Collections;
using System.Text;
using PowerTools;
using UnityEngine;

public class ProcessDataMinigame : Minigame
{
	private string[] DocTopics = new string[17]
	{
		"important", "amongis", "lifeform", "danger", "mining", "rocks", "minerals", "dirt", "soil", "life",
		"specimen", "lookatthis", "wut", "happy_birthday", "1internet", "cake", "pineapple"
	};

	private string[] DocTypes = new string[7] { "data", "srsbiz", "finances", "report", "growth", "results", "investigation" };

	private string[] DocExtensions = new string[15]
	{
		".png", ".tiff", ".txt", ".csv", ".doc", ".file", ".data", ".jpg", ".raw", ".xsl",
		".dot", ".dat", ".doof", ".mira", ".space"
	};

	public float Duration = 5f;

	public ParallaxController scenery;

	public PassiveButton StartButton;

	public TextRenderer EstimatedText;

	public TextRenderer PercentText;

	public SpriteAnim LeftFolder;

	public SpriteAnim RightFolder;

	public AnimationClip OpenFolderClip;

	public AnimationClip CloseFolderClip;

	public GameObject Status;

	public SpriteRenderer Runner;

	public HorizontalGauge Gauge;

	private bool running = true;

	public FloatRange SceneRange = new FloatRange(0f, 50f);

	public override void Begin(PlayerTask task)
	{
		base.Begin(task);
		PlayerControl.LocalPlayer.SetPlayerMaterialColors(Runner);
	}

	public void StartStopFill()
	{
		StartButton.enabled = false;
		StartCoroutine(CoDoAnimation());
	}

	private IEnumerator CoDoAnimation()
	{
		LeftFolder.Play(OpenFolderClip);
		yield return Transition();
		StartCoroutine(DoText());
		for (float timer = 0f; timer < Duration; timer += Time.deltaTime)
		{
			float num = timer / Duration;
			Gauge.Value = num;
			PercentText.Text = Mathf.RoundToInt(num * 100f) + "%";
			scenery.SetParallax(SceneRange.Lerp(num));
			yield return null;
		}
		running = false;
		EstimatedText.Text = DestroyableSingleton<TranslationController>.Instance.GetString(StringNames.WeatherComplete);
		RightFolder.Play(CloseFolderClip);
		MyNormTask.NextStep();
		yield return CoStartClose();
	}

	private IEnumerator Transition()
	{
		yield return Effects.ScaleIn(StartButton.transform, 1f, 0f, 0.15f);
		Status.SetActive(value: true);
		for (float t = 0f; t < 0.15f; t += Time.deltaTime)
		{
			Gauge.transform.localScale = new Vector3(t / 0.15f, 1f, 1f);
			yield return null;
		}
		Gauge.transform.localScale = new Vector3(1f, 1f, 1f);
	}

	private IEnumerator DoText()
	{
		StringBuilder txt = new StringBuilder("Processing: ");
		int len = txt.Length;
		while (running)
		{
			txt.Append(DocTopics.Random());
			txt.Append("_");
			txt.Append(DocTypes.Random());
			txt.Append(DocExtensions.Random());
			EstimatedText.Text = txt.ToString();
			yield return Effects.Wait(FloatRange.Next(0.025f, 0.15f));
			txt.Length = len;
		}
	}
}
