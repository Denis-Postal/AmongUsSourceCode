using System.Collections;
using UnityEngine;

public class VendingSlot : MonoBehaviour
{
	public SpriteRenderer DrinkImage;

	public SpriteRenderer GlassImage;

	private const float SlideDuration = 0.75f;

	public IEnumerator CoBuy(AudioClip sliderOpen, AudioClip drinkShake, AudioClip drinkLand)
	{
		if (Constants.ShouldPlaySfx())
		{
			SoundManager.Instance.PlaySound(sliderOpen, loop: false);
		}
		yield return new WaitForLerp(0.75f, delegate(float v)
		{
			GlassImage.size = new Vector2(1f, Mathf.Lerp(1.7f, 0f, v));
			GlassImage.transform.localPosition = new Vector3(0f, Mathf.Lerp(0f, 0.85f, v), -1f);
		});
		if (Constants.ShouldPlaySfx())
		{
			SoundManager.Instance.PlaySound(drinkShake, loop: false);
		}
		yield return Effects.SwayX(DrinkImage.transform, 0.75f, 0.075f);
		Vector3 localPosition = DrinkImage.transform.localPosition;
		localPosition.z = -5f;
		DrinkImage.transform.localPosition = localPosition;
		Vector3 vector = localPosition;
		vector.y = -8f - localPosition.y;
		yield return Effects.All(Effects.Slide2D(DrinkImage.transform, localPosition, vector), Effects.Rotate2D(DrinkImage.transform, 0f, 0f - FloatRange.Next(-45f, 45f)), Effects.Sequence(Effects.Wait(0.25f), PlayLand(drinkLand)));
		DrinkImage.enabled = false;
	}

	public IEnumerator CloseSlider(AudioClip sliderOpen)
	{
		if (Constants.ShouldPlaySfx())
		{
			SoundManager.Instance.PlaySound(sliderOpen, loop: false);
		}
		yield return new WaitForLerp(0.75f, delegate(float v)
		{
			GlassImage.size = new Vector2(1f, Mathf.Lerp(0f, 1.7f, v));
			GlassImage.transform.localPosition = new Vector3(0f, Mathf.Lerp(0.85f, 0f, v), -1f);
		});
	}

	private IEnumerator PlayLand(AudioClip drinkLand)
	{
		if (Constants.ShouldPlaySfx())
		{
			SoundManager.Instance.PlaySound(drinkLand, loop: false);
		}
		yield break;
	}
}
