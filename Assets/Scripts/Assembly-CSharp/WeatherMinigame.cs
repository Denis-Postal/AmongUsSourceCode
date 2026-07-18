using System.Collections;
using UnityEngine;

public class WeatherMinigame : Minigame
{
	public float Duration = 5f;

	public HorizontalGauge destGauge1;

	public HorizontalGauge destGauge2;

	public HorizontalGauge destGauge3;

	public PassiveButton StartButton;

	public TextRenderer EtaText;

	public AudioClip StartSound;

	public void StartStopFill()
	{
		StartButton.enabled = false;
		StartCoroutine(CoDoAnimation());
	}

	private IEnumerator CoDoAnimation()
	{
		if (Constants.ShouldPlaySfx())
		{
			SoundManager.Instance.PlaySound(StartSound, loop: false);
		}
		yield return Effects.ScaleIn(StartButton.transform, 1f, 0f, 0.15f);
		EtaText.gameObject.SetActive(value: true);
		yield return Effects.ScaleIn(EtaText.transform, 0f, 1f, 0.15f);
		for (float timer = 0f; timer < Duration; timer += Time.deltaTime)
		{
			int num = Mathf.CeilToInt(Duration - timer);
			EtaText.Text = DestroyableSingleton<TranslationController>.Instance.GetString(StringNames.WeatherEta, num);
			destGauge1.Value = Mathf.Lerp(0f, 1f, timer / Duration * 5f);
			destGauge2.Value = Mathf.Lerp(0f, 1f, timer / Duration * 3f);
			destGauge3.Value = Mathf.Lerp(0f, 1f, timer / Duration);
			yield return null;
		}
		EtaText.Text = DestroyableSingleton<TranslationController>.Instance.GetString(StringNames.WeatherComplete);
		MyNormTask.NextStep();
		yield return CoStartClose();
	}
}
