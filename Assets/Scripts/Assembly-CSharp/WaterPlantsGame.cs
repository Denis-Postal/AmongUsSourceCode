using System.Collections;
using System.Linq;
using UnityEngine;

public class WaterPlantsGame : Minigame
{
	public GameObject stage1;

	public GameObject stage2;

	public AudioClip CanGrabSound;

	public PassiveButton WaterCan;

	public SpriteRenderer[] Plants;

	public AudioClip WaterPlantSound;

	public AudioClip[] PlantGrowSounds;

	public AudioClip[] PlantFinishedSounds;

	public TextRenderer FloatText;

	public Transform[] Locations;

	private bool Watered(int x)
	{
		return MyNormTask.Data[x] != 0;
	}

	private void Watered(int x, bool b)
	{
		MyNormTask.Data[x] = (byte)(b ? 1u : 0u);
	}

	public override void Begin(PlayerTask task)
	{
		base.Begin(task);
		if (MyNormTask.taskStep == 0)
		{
			WaterCan.transform.localPosition = Locations.Random().localPosition;
			WaterCan.GetComponent<SpriteRenderer>().flipX = BoolRange.Next();
			stage1.gameObject.SetActive(value: true);
			stage2.gameObject.SetActive(value: false);
		}
		else
		{
			if (MyNormTask.taskStep != 1)
			{
				return;
			}
			stage1.gameObject.SetActive(value: false);
			stage2.gameObject.SetActive(value: true);
			for (int i = 0; i < Plants.Length; i++)
			{
				if (Watered(i))
				{
					SpriteRenderer obj = Plants[i];
					obj.material.SetFloat("_Desat", 0f);
					obj.transform.localScale = Vector3.one;
				}
			}
		}
	}

	public void PickWaterCan()
	{
		WaterCan.enabled = false;
		if (Constants.ShouldPlaySfx())
		{
			SoundManager.Instance.PlaySound(CanGrabSound, loop: false);
		}
		MyNormTask.NextStep();
		StartCoroutine(CoPickWaterCan());
	}

	private IEnumerator CoPickWaterCan()
	{
		FloatText.Text = DestroyableSingleton<TranslationController>.Instance.GetString(StringNames.WaterPlantsGetCan);
		FloatText.gameObject.SetActive(value: true);
		yield return Effects.All(Effects.ColorFade(WaterCan.GetComponent<SpriteRenderer>(), Color.white, Palette.ClearWhite, 0.25f), Effects.Slide2D(FloatText.transform, WaterCan.transform.localPosition + new Vector3(0f, 0.1f, 0f), WaterCan.transform.localPosition + new Vector3(0f, 0.5f, 0f)), Effects.ColorFade(FloatText, Color.white, Palette.ClearWhite, 0.75f));
		yield return CoStartClose();
	}

	public void WaterPlant(int num)
	{
		if (!Watered(num))
		{
			Watered(num, b: true);
			if (Enumerable.Range(0, 4).All(Watered))
			{
				MyNormTask.NextStep();
				StartCoroutine(CoStartClose());
			}
			if (Constants.ShouldPlaySfx())
			{
				SoundManager.Instance.PlaySound(WaterPlantSound, loop: false);
			}
			StartCoroutine(CoGrowPlant(num));
		}
	}

	private IEnumerator CoGrowPlant(int num)
	{
		SpriteRenderer plant = Plants[num];
		if (Constants.ShouldPlaySfx())
		{
			SoundManager.Instance.PlaySound(PlantGrowSounds.Random(), loop: false).pitch = FloatRange.Next(0.9f, 1.1f);
		}
		for (float timer = 0f; timer < 1f; timer += Time.deltaTime)
		{
			float num2 = timer / 1f;
			plant.material.SetFloat("_Desat", 1f - num2);
			plant.transform.localScale = new Vector3(0.8f, Mathf.Lerp(0.8f, 1.1f, num2), 1f);
			yield return null;
		}
		plant.material.SetFloat("_Desat", 0f);
		if (Constants.ShouldPlaySfx())
		{
			SoundManager.Instance.PlaySound(PlantFinishedSounds.Random(), loop: false).pitch = FloatRange.Next(0.9f, 1.1f);
		}
		for (float timer = 0f; timer < 0.1f; timer += Time.deltaTime)
		{
			float t = timer / 0.1f;
			plant.transform.localScale = new Vector3(Mathf.Lerp(0.8f, 1.1f, t), Mathf.Lerp(1.1f, 0.95f, t), 1f);
			yield return null;
		}
		for (float timer = 0f; timer < 0.1f; timer += Time.deltaTime)
		{
			float t2 = timer / 0.1f;
			plant.transform.localScale = new Vector3(Mathf.Lerp(1.1f, 1f, t2), Mathf.Lerp(0.95f, 1f, t2), 1f);
			yield return null;
		}
	}
}
