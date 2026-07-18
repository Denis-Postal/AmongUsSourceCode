using UnityEngine;
using UnityEngine.UI;

public class DeconControl : MonoBehaviour, IUsable
{
	public DeconSystem System;

	public float usableDistance = 1f;

	public SpriteRenderer Image;

	public AudioClip UseSound;

	public Button.ButtonClickedEvent OnUse;

	public float UsableDistance => usableDistance;

	public float PercentCool => 0f;

	public void SetOutline(bool on, bool mainTarget)
	{
		if ((bool)Image)
		{
			Image.material.SetFloat("_Outline", on ? 1 : 0);
			Image.material.SetColor("_OutlineColor", Color.white);
			Image.material.SetColor("_AddColor", mainTarget ? Color.white : Color.clear);
		}
	}

	public float CanUse(GameData.PlayerInfo pc, out bool canUse, out bool couldUse)
	{
		if (System.CurState != DeconSystem.States.Idle)
		{
			canUse = false;
			couldUse = false;
			return 0f;
		}
		float num = float.MaxValue;
		PlayerControl playerControl = pc.Object;
		couldUse = pc.Object.CanMove && !pc.IsDead;
		canUse = couldUse;
		if (canUse)
		{
			num = Vector2.Distance(playerControl.GetTruePosition(), base.transform.position);
			canUse &= num <= UsableDistance;
		}
		return num;
	}

	public void Use()
	{
		CanUse(PlayerControl.LocalPlayer.Data, out var canUse, out var _);
		if (canUse)
		{
			if (Constants.ShouldPlaySfx())
			{
				SoundManager.Instance.PlaySound(UseSound, loop: false);
			}
			OnUse.Invoke();
		}
	}
}
