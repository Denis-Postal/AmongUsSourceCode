using System.Collections;
using System.Linq;
using UnityEngine;

public class ExileController : MonoBehaviour
{
	public static ExileController Instance;

	public TextRenderer ImpostorText;

	public TextRenderer Text;

	public SpriteRenderer Player;

	public SpriteRenderer PlayerHat;

	public SpriteRenderer PlayerSkin;

	public AnimationCurve LerpCurve;

	public float Duration = 7f;

	public AudioClip TextSound;

	protected string completeString = "TestPlayer was not The Impostor";

	protected GameData.PlayerInfo exiled;

	public void Begin(GameData.PlayerInfo exiled, bool tie)
	{
		Instance = this;
		this.exiled = exiled;
		Text.gameObject.SetActive(value: false);
		Text.Text = string.Empty;
		int num = GameData.Instance.AllPlayers.Count((GameData.PlayerInfo p) => p.IsImpostor && !p.IsDead && !p.Disconnected);
		if (exiled != null)
		{
			int num2 = GameData.Instance.AllPlayers.Count((GameData.PlayerInfo p) => p.IsImpostor);
			if (exiled.IsImpostor)
			{
				if (num2 > 1)
				{
					completeString = DestroyableSingleton<TranslationController>.Instance.GetString(StringNames.ExileTextPP, exiled.PlayerName);
				}
				else
				{
					completeString = DestroyableSingleton<TranslationController>.Instance.GetString(StringNames.ExileTextSP, exiled.PlayerName);
				}
			}
			else if (num2 > 1)
			{
				completeString = DestroyableSingleton<TranslationController>.Instance.GetString(StringNames.ExileTextPN, exiled.PlayerName);
			}
			else
			{
				completeString = DestroyableSingleton<TranslationController>.Instance.GetString(StringNames.ExileTextSN, exiled.PlayerName);
			}
			PlayerControl.SetPlayerMaterialColors(exiled.ColorId, Player);
			PlayerControl.SetHatImage(exiled.HatId, PlayerHat);
			PlayerSkin.sprite = DestroyableSingleton<HatManager>.Instance.GetSkinById(exiled.SkinId).EjectFrame;
			if (exiled.IsImpostor)
			{
				num--;
			}
		}
		else
		{
			if (tie)
			{
				completeString = DestroyableSingleton<TranslationController>.Instance.GetString(StringNames.NoExileTie);
			}
			else
			{
				completeString = DestroyableSingleton<TranslationController>.Instance.GetString(StringNames.NoExileSkip);
			}
			Player.gameObject.SetActive(value: false);
		}
		if (num == 1)
		{
			ImpostorText.Text = DestroyableSingleton<TranslationController>.Instance.GetString(StringNames.ImpostorsRemainS, num);
		}
		else
		{
			ImpostorText.Text = DestroyableSingleton<TranslationController>.Instance.GetString(StringNames.ImpostorsRemainP, num);
		}
		StartCoroutine(Animate());
	}

	protected virtual IEnumerator Animate()
	{
		yield return DestroyableSingleton<HudManager>.Instance.CoFadeFullScreen(Color.black, Color.clear);
		yield return new WaitForSeconds(1f);
		float num = Camera.main.orthographicSize * Camera.main.aspect + 1f;
		Vector2 left = Vector2.left * num;
		Vector2 right = Vector2.right * num;
		for (float t = 0f; t <= Duration; t += Time.deltaTime)
		{
			float num2 = t / Duration;
			Player.transform.localPosition = Vector2.Lerp(left, right, LerpCurve.Evaluate(num2));
			float num3 = (t + 0.75f) * 25f / Mathf.Exp(t * 0.75f + 1f);
			Player.transform.Rotate(new Vector3(0f, 0f, num3 * Time.deltaTime * 30f));
			if (num2 >= 0.3f)
			{
				int num4 = (int)(Mathf.Min(1f, (num2 - 0.3f) / 0.3f) * (float)completeString.Length);
				if (num4 > Text.Text.Length)
				{
					Text.Text = completeString.Substring(0, num4);
					Text.gameObject.SetActive(value: true);
					if (completeString[num4 - 1] != ' ')
					{
						SoundManager.Instance.PlaySoundImmediate(TextSound, loop: false, 0.8f);
					}
				}
			}
			yield return null;
		}
		Text.Text = completeString;
		ImpostorText.gameObject.SetActive(value: true);
		yield return Effects.Bloop(0f, ImpostorText.transform);
		yield return new WaitForSeconds(0.5f);
		yield return DestroyableSingleton<HudManager>.Instance.CoFadeFullScreen(Color.clear, Color.black);
		WrapUp();
	}

	protected void WrapUp()
	{
		if (exiled != null)
		{
			exiled.Object?.Exiled();
		}
		if (DestroyableSingleton<TutorialManager>.InstanceExists || !ShipStatus.Instance.IsGameOverDueToDeath())
		{
			DestroyableSingleton<HudManager>.Instance.StartCoroutine(DestroyableSingleton<HudManager>.Instance.CoFadeFullScreen(Color.black, Color.clear));
			PlayerControl.LocalPlayer.SetKillTimer(PlayerControl.GameOptions.KillCooldown);
			ShipStatus.Instance.EmergencyCooldown = PlayerControl.GameOptions.EmergencyCooldown;
			Camera.main.GetComponent<FollowerCamera>().Locked = false;
			DestroyableSingleton<HudManager>.Instance.SetHudActive(isActive: true);
		}
		Object.Destroy(base.gameObject);
	}
}
