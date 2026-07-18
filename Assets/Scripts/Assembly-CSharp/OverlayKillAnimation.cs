using System.Collections;
using PowerTools;
using UnityEngine;

public class OverlayKillAnimation : MonoBehaviour
{
	public KillAnimType KillType;

	public PoolablePlayer killerParts;

	public PoolablePlayer victimParts;

	private uint victimHat;

	public AudioClip Stinger;

	public AudioClip Sfx;

	public float StingerVolume = 0.6f;

	public void Begin(PlayerControl killer, GameData.PlayerInfo vInfo)
	{
		if ((bool)killerParts)
		{
			GameData.PlayerInfo kInfo = killer.Data;
			PlayerControl.SetPlayerMaterialColors(kInfo.ColorId, killerParts.Body);
			killerParts.Hands.ForEach(delegate(SpriteRenderer b)
			{
				PlayerControl.SetPlayerMaterialColors(kInfo.ColorId, b);
			});
			PlayerControl.SetHatImage(kInfo.HatId, killerParts.HatSlot);
			switch (KillType)
			{
			case KillAnimType.Tongue:
			{
				SkinData skinById2 = DestroyableSingleton<HatManager>.Instance.GetSkinById(kInfo.SkinId);
				killerParts.SkinSlot.GetComponent<SpriteAnim>().Play(skinById2.KillTongueImpostor);
				break;
			}
			case KillAnimType.Shoot:
			{
				SkinData skinById = DestroyableSingleton<HatManager>.Instance.GetSkinById(kInfo.SkinId);
				killerParts.SkinSlot.GetComponent<SpriteAnim>().Play(skinById.KillShootImpostor);
				break;
			}
			case KillAnimType.Stab:
			case KillAnimType.Neck:
				PlayerControl.SetSkinImage(kInfo.SkinId, killerParts.SkinSlot);
				break;
			}
			if ((bool)killerParts.PetSlot)
			{
				PetBehaviour petById = DestroyableSingleton<HatManager>.Instance.GetPetById(kInfo.PetId);
				if ((bool)petById && (bool)petById.scaredClip)
				{
					killerParts.PetSlot.GetComponent<SpriteAnim>().Play(petById.idleClip);
					killerParts.PetSlot.sharedMaterial = petById.rend.sharedMaterial;
					PlayerControl.SetPlayerMaterialColors(kInfo.ColorId, killerParts.PetSlot);
				}
				else
				{
					killerParts.PetSlot.enabled = false;
				}
			}
		}
		if (vInfo == null || !victimParts)
		{
			return;
		}
		victimHat = vInfo.HatId;
		PlayerControl.SetPlayerMaterialColors(vInfo.ColorId, victimParts.Body);
		PlayerControl.SetHatImage(vInfo.HatId, victimParts.HatSlot);
		SkinData skinById3 = DestroyableSingleton<HatManager>.Instance.GetSkinById(vInfo.SkinId);
		switch (KillType)
		{
		case KillAnimType.Tongue:
			victimParts.SkinSlot.GetComponent<SpriteAnim>().Play(skinById3.KillTongueVictim);
			break;
		case KillAnimType.Stab:
			victimParts.SkinSlot.GetComponent<SpriteAnim>().Play(skinById3.KillStabVictim);
			break;
		case KillAnimType.Neck:
			victimParts.SkinSlot.GetComponent<SpriteAnim>().Play(skinById3.KillNeckVictim);
			break;
		case KillAnimType.Shoot:
			victimParts.SkinSlot.GetComponent<SpriteAnim>().Play(skinById3.KillShootVictim);
			break;
		}
		if ((bool)victimParts.PetSlot)
		{
			PetBehaviour petById2 = DestroyableSingleton<HatManager>.Instance.GetPetById(vInfo.PetId);
			if ((bool)petById2 && (bool)petById2.scaredClip)
			{
				victimParts.PetSlot.GetComponent<SpriteAnim>().Play(petById2.scaredClip);
				victimParts.PetSlot.sharedMaterial = petById2.rend.sharedMaterial;
				PlayerControl.SetPlayerMaterialColors(vInfo.ColorId, victimParts.PetSlot);
			}
			else
			{
				victimParts.PetSlot.enabled = false;
			}
		}
	}

	public void SetHatFloor()
	{
		HatBehaviour hatById = DestroyableSingleton<HatManager>.Instance.GetHatById(victimHat);
		if ((bool)hatById)
		{
			victimParts.HatSlot.sprite = hatById.FloorImage;
		}
	}

	public void PlayKillSound()
	{
		if (Constants.ShouldPlaySfx())
		{
			SoundManager.Instance.PlaySound(Sfx, loop: false).volume = 0.8f;
		}
	}

	public IEnumerator WaitForFinish()
	{
		SpriteAnim[] anims = GetComponentsInChildren<SpriteAnim>();
		if (anims.Length == 0)
		{
			yield return new WaitForSeconds(1f);
			yield break;
		}
		while (true)
		{
			bool flag = false;
			for (int i = 0; i < anims.Length; i++)
			{
				if (anims[i].IsPlaying())
				{
					flag = true;
					break;
				}
			}
			if (flag)
			{
				yield return null;
				continue;
			}
			break;
		}
	}
}
