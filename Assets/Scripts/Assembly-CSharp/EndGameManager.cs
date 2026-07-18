using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Assets.CoreScripts;
using UnityEngine;

public class EndGameManager : DestroyableSingleton<EndGameManager>
{
	public TextRenderer WinText;

	public MeshRenderer BackgroundBar;

	public MeshRenderer Foreground;

	public FloatRange ForegroundRadius;

	public SpriteRenderer FrontMost;

	public PoolablePlayer PlayerPrefab;

	public Sprite GhostSprite;

	public SpriteRenderer PlayAgainButton;

	public SpriteRenderer ExitButton;

	public AudioClip DisconnectStinger;

	public AudioClip CrewStinger;

	public AudioClip ImpostorStinger;

	public float BaseY = -0.25f;

	private float stingerTime;

	public void Start()
	{
		if (TempData.showAd && !SaveManager.BoughtNoAds)
		{
			AdPlayer.RequestInterstitial();
		}
		SetEverythingUp();
		StartCoroutine(CoBegin());
		Invoke("ShowButtons", 1.1f);
	}

	private void ShowButtons()
	{
		FrontMost.gameObject.SetActive(value: false);
		PlayAgainButton.gameObject.SetActive(value: true);
		ExitButton.gameObject.SetActive(value: true);
	}

	private void SetEverythingUp()
	{
		StatsManager.Instance.GamesFinished++;
		bool flag = TempData.DidHumansWin(TempData.EndReason);
		if (TempData.EndReason == GameOverReason.ImpostorDisconnect)
		{
			StatsManager.Instance.AddDrawReason(TempData.EndReason);
			WinText.Text = DestroyableSingleton<TranslationController>.Instance.GetString(StringNames.ImpostorDisconnected);
			SoundManager.Instance.PlaySound(DisconnectStinger, loop: false);
		}
		else if (TempData.EndReason == GameOverReason.HumansDisconnect)
		{
			StatsManager.Instance.AddDrawReason(TempData.EndReason);
			WinText.Text = DestroyableSingleton<TranslationController>.Instance.GetString(StringNames.CrewmatesDisconnected);
			SoundManager.Instance.PlaySound(DisconnectStinger, loop: false);
		}
		else
		{
			if (TempData.winners.Any((WinningPlayerData h) => h.IsYou))
			{
				StatsManager.Instance.AddWinReason(TempData.EndReason);
				WinText.Text = DestroyableSingleton<TranslationController>.Instance.GetString(StringNames.Victory);
				BackgroundBar.material.SetColor("_Color", Palette.CrewmateBlue);
				WinningPlayerData winningPlayerData = TempData.winners.FirstOrDefault((WinningPlayerData h) => h.IsYou);
				if (winningPlayerData != null)
				{
					DestroyableSingleton<Telemetry>.Instance.WonGame(winningPlayerData.ColorId, winningPlayerData.HatId);
				}
			}
			else
			{
				StatsManager.Instance.AddLoseReason(TempData.EndReason);
				WinText.Text = DestroyableSingleton<TranslationController>.Instance.GetString(StringNames.Defeat);
				WinText.Color = Color.red;
			}
			if (flag)
			{
				SoundManager.Instance.PlayDynamicSound("Stinger", CrewStinger, loop: false, GetStingerVol);
			}
			else
			{
				SoundManager.Instance.PlayDynamicSound("Stinger", ImpostorStinger, loop: false, GetStingerVol);
			}
		}
		List<WinningPlayerData> list = TempData.winners.OrderBy((WinningPlayerData b) => b.IsYou ? (-1) : 0).ToList();
		for (int num = 0; num < list.Count; num++)
		{
			WinningPlayerData winningPlayerData2 = list[num];
			int num2 = ((num % 2 != 0) ? 1 : (-1));
			int num3 = (num + 1) / 2;
			float num4 = 1f - (float)num3 * 0.075f;
			float num5 = 1f - (float)num3 * 0.035f;
			float num6 = ((num == 0) ? (-8) : (-1));
			PoolablePlayer poolablePlayer = UnityEngine.Object.Instantiate(PlayerPrefab, base.transform);
			poolablePlayer.transform.localPosition = new Vector3(0.8f * (float)num2 * (float)num3 * num5, BaseY - 0.25f + (float)num3 * 0.1f, num6 + (float)num3 * 0.01f) * 1.25f;
			Vector3 vector = new Vector3(num4, num4, num4) * 1.25f;
			poolablePlayer.transform.localScale = vector;
			if (winningPlayerData2.IsDead)
			{
				poolablePlayer.Body.sprite = GhostSprite;
				poolablePlayer.SetDeadFlipX(num % 2 != 0);
			}
			else
			{
				poolablePlayer.SetFlipX(num % 2 == 0);
			}
			if (!winningPlayerData2.IsDead)
			{
				DestroyableSingleton<HatManager>.Instance.SetSkin(poolablePlayer.SkinSlot, winningPlayerData2.SkinId);
			}
			else
			{
				poolablePlayer.HatSlot.color = new Color(1f, 1f, 1f, 0.5f);
			}
			PlayerControl.SetPlayerMaterialColors(winningPlayerData2.ColorId, poolablePlayer.Body);
			PlayerControl.SetHatImage(winningPlayerData2.HatId, poolablePlayer.HatSlot);
			PlayerControl.SetPetImage(winningPlayerData2.PetId, winningPlayerData2.ColorId, poolablePlayer.PetSlot);
			if (flag)
			{
				poolablePlayer.NameText.gameObject.SetActive(value: false);
				continue;
			}
			poolablePlayer.NameText.Text = winningPlayerData2.Name;
			if (winningPlayerData2.IsImpostor)
			{
				poolablePlayer.NameText.Color = Palette.ImpostorRed;
			}
			poolablePlayer.NameText.transform.localScale = vector.Inv();
		}
	}

	private void GetStingerVol(AudioSource source, float dt)
	{
		stingerTime += dt * 0.75f;
		source.volume = Mathf.Clamp(1f / stingerTime, 0f, 1f);
	}

	public IEnumerator CoBegin()
	{
		Color c = WinText.Color;
		Color fade = Color.black;
		_ = Color.white;
		Vector3 titlePos = WinText.transform.localPosition;
		float timer = 0f;
		while (timer < 3f)
		{
			timer += Time.deltaTime;
			float num = Mathf.Min(1f, timer / 3f);
			Foreground.material.SetFloat("_Rad", ForegroundRadius.ExpOutLerp(num * 2f));
			fade.a = Mathf.Lerp(1f, 0f, num * 3f);
			FrontMost.color = fade;
			c.a = Mathf.Clamp(FloatRange.ExpOutLerp(num, 0f, 1f), 0f, 1f);
			WinText.Color = c;
			titlePos.y = 2.7f - num * 0.3f;
			WinText.transform.localPosition = titlePos;
			yield return null;
		}
		FrontMost.gameObject.SetActive(value: false);
	}

	public void NextGame()
	{
		PlayAgainButton.gameObject.SetActive(value: false);
		ExitButton.gameObject.SetActive(value: false);
		if (TempData.showAd && !SaveManager.BoughtNoAds)
		{
			TempData.showAd = false;
			AdPlayer.ShowInterstitial(this, playAgain: true);
		}
		else
		{
			StartCoroutine(CoJoinGame());
		}
	}

	public IEnumerator CoJoinGame()
	{
		AmongUsClient.Instance.JoinGame();
		yield return WaitWithTimeout(() => AmongUsClient.Instance.ClientId >= 0);
		if (AmongUsClient.Instance.ClientId < 0)
		{
			AmongUsClient.Instance.ExitGame(AmongUsClient.Instance.LastDisconnectReason);
		}
	}

	public void Exit()
	{
		PlayAgainButton.gameObject.SetActive(value: false);
		ExitButton.gameObject.SetActive(value: false);
		if (TempData.showAd && !SaveManager.BoughtNoAds)
		{
			TempData.showAd = false;
			AdPlayer.ShowInterstitial(this, playAgain: false);
		}
		else
		{
			AmongUsClient.Instance.ExitGame();
		}
	}

	public static IEnumerator WaitWithTimeout(Func<bool> success)
	{
		for (float timer = 0f; timer < 5f; timer += Time.deltaTime)
		{
			if (success())
			{
				break;
			}
			yield return null;
		}
	}
}
