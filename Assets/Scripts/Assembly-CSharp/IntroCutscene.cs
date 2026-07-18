using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IntroCutscene : MonoBehaviour
{
	public static IntroCutscene Instance;

	public TextRenderer Title;

	public TextRenderer ImpostorText;

	public PoolablePlayer PlayerPrefab;

	public MeshRenderer BackgroundBar;

	public MeshRenderer Foreground;

	public FloatRange ForegroundRadius;

	public SpriteRenderer FrontMost;

	public AudioClip IntroStinger;

	public float BaseY = -0.25f;

	private static readonly Vector3 PlayerIntroAnchor = new Vector3(0f, -0.75f, -13.81f);

	public IEnumerator CoBegin(List<PlayerControl> yourTeam, bool isImpostor)
	{
		SoundManager.Instance.PlaySound(IntroStinger, loop: false);
		if (!isImpostor)
		{
			BeginCrewmate(yourTeam);
		}
		else
		{
			BeginImpostor(yourTeam);
		}
		Color c = Title.Color;
		Color fade = Color.black;
		Color impColor = Color.white;
		Vector3 titlePos = Title.transform.localPosition;
		float timer = 0f;
		while (timer < 3f)
		{
			timer += Time.deltaTime;
			float num = Mathf.Min(1f, timer / 3f);
			Foreground.material.SetFloat("_Rad", ForegroundRadius.ExpOutLerp(num * 2f));
			fade.a = Mathf.Lerp(1f, 0f, num * 3f);
			FrontMost.color = fade;
			c.a = Mathf.Clamp(FloatRange.ExpOutLerp(num, 0f, 1f), 0f, 1f);
			Title.Color = c;
			impColor.a = Mathf.Lerp(0f, 1f, (num - 0.3f) * 3f);
			ImpostorText.Color = impColor;
			titlePos.y = 2.7f - num * 0.3f;
			Title.transform.localPosition = titlePos;
			yield return null;
		}
		timer = 0f;
		while (timer < 1f)
		{
			timer += Time.deltaTime;
			float num2 = timer / 1f;
			fade.a = Mathf.Lerp(0f, 1f, num2 * 3f);
			FrontMost.color = fade;
			yield return null;
		}
		Object.Destroy(base.gameObject);
	}

	private void BeginCrewmate(List<PlayerControl> yourTeam)
	{
		Vector3 position = BackgroundBar.transform.position;
		position.y -= 0.25f;
		BackgroundBar.transform.position = position;
		int adjustedNumImpostors = PlayerControl.GameOptions.GetAdjustedNumImpostors(GameData.Instance.PlayerCount);
		if (adjustedNumImpostors == 1)
		{
			ImpostorText.Text = DestroyableSingleton<TranslationController>.Instance.GetString(StringNames.NumImpostorsS);
		}
		else
		{
			ImpostorText.Text = DestroyableSingleton<TranslationController>.Instance.GetString(StringNames.NumImpostorsP, adjustedNumImpostors);
		}
		BackgroundBar.material.SetColor("_Color", Palette.CrewmateBlue);
		Title.Text = DestroyableSingleton<TranslationController>.Instance.GetString(StringNames.Crewmate);
		Title.Color = Palette.CrewmateBlue;
		for (int i = 0; i < yourTeam.Count; i++)
		{
			PlayerControl playerControl = yourTeam[i];
			if ((bool)playerControl)
			{
				GameData.PlayerInfo data = playerControl.Data;
				if (data != null)
				{
					int num = ((i % 2 != 0) ? 1 : (-1));
					int num2 = (i + 1) / 2;
					float num3 = ((i == 0) ? 1.2f : 1f) - (float)num2 * 0.12f;
					float num4 = 1f - (float)num2 * 0.08f;
					float num5 = PlayerIntroAnchor.z + (float)num2 * 0.015f;
					PoolablePlayer poolablePlayer = Object.Instantiate(PlayerPrefab, base.transform);
					poolablePlayer.name = data.PlayerName + "Dummy";
					poolablePlayer.SetFlipX(i % 2 == 0);
					poolablePlayer.transform.localPosition = PlayerIntroAnchor + new Vector3(0.8f * (float)num * (float)num2 * num4 * 1.5f, (float)num2 * 0.15f, num5 - PlayerIntroAnchor.z);
					Vector3 localScale = new Vector3(num3, num3, num3) * 1.5f;
					poolablePlayer.transform.localScale = localScale;
					PlayerControl.SetPlayerMaterialColors(data.ColorId, poolablePlayer.Body);
					DestroyableSingleton<HatManager>.Instance.SetSkin(poolablePlayer.SkinSlot, data.SkinId);
					PlayerControl.SetHatImage(data.HatId, poolablePlayer.HatSlot);
					PlayerControl.SetPetImage(data.PetId, data.ColorId, poolablePlayer.PetSlot);
					poolablePlayer.NameText.gameObject.SetActive(value: false);
				}
			}
		}
	}

	private void BeginImpostor(List<PlayerControl> yourTeam)
	{
		ImpostorText.gameObject.SetActive(value: false);
		Title.Text = DestroyableSingleton<TranslationController>.Instance.GetString(StringNames.Impostor);
		Title.Color = Palette.ImpostorRed;
		for (int i = 0; i < yourTeam.Count; i++)
		{
			PlayerControl playerControl = yourTeam[i];
			if ((bool)playerControl)
			{
				GameData.PlayerInfo data = playerControl.Data;
				if (data != null)
				{
					int num = ((i % 2 != 0) ? 1 : (-1));
					int num2 = (i + 1) / 2;
					float num3 = 1f - (float)num2 * 0.075f;
					float num4 = 1f - (float)num2 * 0.035f;
					float num5 = PlayerIntroAnchor.z + (float)num2 * 0.015f;
					PoolablePlayer poolablePlayer = Object.Instantiate(PlayerPrefab, base.transform);
					poolablePlayer.transform.localPosition = PlayerIntroAnchor + new Vector3((float)(num * num2) * num4 * 1.5f, (float)num2 * 0.225f, num5 - PlayerIntroAnchor.z);
					Vector3 vector = new Vector3(num3, num3, num3) * 1.5f;
					poolablePlayer.transform.localScale = vector;
					poolablePlayer.SetFlipX(i % 2 == 1);
					PlayerControl.SetPlayerMaterialColors(data.ColorId, poolablePlayer.Body);
					DestroyableSingleton<HatManager>.Instance.SetSkin(poolablePlayer.SkinSlot, data.SkinId);
					PlayerControl.SetHatImage(data.HatId, poolablePlayer.HatSlot);
					PlayerControl.SetPetImage(data.PetId, data.ColorId, poolablePlayer.PetSlot);
					TextRenderer nameText = poolablePlayer.NameText;
					nameText.Text = data.PlayerName;
					nameText.transform.localScale = vector.Inv();
				}
			}
		}
	}
}
