using System.Collections;
using UnityEngine;

public class MiraExileController : ExileController
{
	public Transform BackgroundClouds;

	public Transform ForegroundClouds;

	protected override IEnumerator Animate()
	{
		yield return DestroyableSingleton<HudManager>.Instance.CoFadeFullScreen(Color.black, Color.clear);
		yield return Effects.All(PlayerSpin(), HandleText(), Effects.Slide2D(BackgroundClouds, new Vector2(0f, -3f), new Vector2(0f, 0.5f), Duration), Effects.Sequence(Effects.Wait(2f), Effects.Slide2D(ForegroundClouds, new Vector2(0f, -7f), new Vector2(0f, 2f))));
		ImpostorText.gameObject.SetActive(value: true);
		yield return Effects.Bloop(0f, ImpostorText.transform);
		yield return new WaitForSeconds(0.5f);
		yield return DestroyableSingleton<HudManager>.Instance.CoFadeFullScreen(Color.clear, Color.black);
		WrapUp();
	}

	private IEnumerator HandleText()
	{
		yield return Effects.Wait(Duration * 0.5f);
		float newDur = Duration * 0.5f;
		for (float t = 0f; t <= newDur; t += Time.deltaTime)
		{
			int num = (int)(t / newDur * (float)completeString.Length);
			if (num > Text.Text.Length)
			{
				Text.Text = completeString.Substring(0, num);
				Text.gameObject.SetActive(value: true);
				if (completeString[num - 1] != ' ')
				{
					SoundManager.Instance.PlaySoundImmediate(TextSound, loop: false, 0.8f);
				}
			}
			yield return null;
		}
		Text.Text = completeString;
	}

	private IEnumerator PlayerSpin()
	{
		float num = Camera.main.orthographicSize + 1f;
		Vector2 top = Vector2.up * num;
		Vector2 bottom = Vector2.down * num;
		for (float t = 0f; t <= Duration; t += Time.deltaTime)
		{
			float t2 = t / Duration;
			Player.transform.localPosition = Vector2.Lerp(top, bottom, t2);
			float num2 = (t + 0.75f) * 25f / Mathf.Exp(t * 0.75f + 1f);
			Player.transform.Rotate(new Vector3(0f, 0f, num2 * Time.deltaTime * 5f));
			yield return null;
		}
		for (float num3 = 0f; num3 <= 1f; num3 += Time.deltaTime)
		{
			_ = num3 / 1f;
		}
	}
}
