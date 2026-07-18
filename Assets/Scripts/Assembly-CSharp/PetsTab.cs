using System.Collections.Generic;
using UnityEngine;

public class PetsTab : MonoBehaviour
{
	public ColorChip ColorTabPrefab;

	public SpriteRenderer DemoImage;

	public SpriteRenderer HatImage;

	public SpriteRenderer SkinImage;

	public SpriteRenderer PetImage;

	public FloatRange XRange = new FloatRange(1.5f, 3f);

	public float YStart = 0.8f;

	public float YOffset = 0.8f;

	public int NumPerRow = 4;

	public Scroller scroller;

	private List<ColorChip> ColorChips = new List<ColorChip>();

	public void OnEnable()
	{
		PlayerControl.SetPlayerMaterialColors(PlayerControl.LocalPlayer.Data.ColorId, DemoImage);
		PlayerControl.SetHatImage(SaveManager.LastHat, HatImage);
		PlayerControl.SetSkinImage(SaveManager.LastSkin, SkinImage);
		PlayerControl.SetPetImage(SaveManager.LastPet, PlayerControl.LocalPlayer.Data.ColorId, PetImage);
		PetBehaviour[] unlockedPets = DestroyableSingleton<HatManager>.Instance.GetUnlockedPets();
		for (int i = 0; i < unlockedPets.Length; i++)
		{
			PetBehaviour pet = unlockedPets[i];
			float x = XRange.Lerp((float)(i % NumPerRow) / ((float)NumPerRow - 1f));
			float y = YStart - (float)(i / NumPerRow) * YOffset;
			ColorChip chip = Object.Instantiate(ColorTabPrefab, scroller.Inner);
			chip.transform.localPosition = new Vector3(x, y, -1f);
			chip.InUseForeground.SetActive(DestroyableSingleton<HatManager>.Instance.GetIdFromPet(pet) == SaveManager.LastPet);
			chip.Button.OnClick.AddListener(delegate
			{
				SelectPet(chip, pet);
			});
			PlayerControl.SetPetImage(pet, PlayerControl.LocalPlayer.Data.ColorId, chip.Inner);
			ColorChips.Add(chip);
		}
		scroller.YBounds.max = 0f - (YStart - (float)(unlockedPets.Length / NumPerRow) * YOffset) - 3f;
	}

	public void OnDisable()
	{
		for (int i = 0; i < ColorChips.Count; i++)
		{
			Object.Destroy(ColorChips[i].gameObject);
		}
		ColorChips.Clear();
	}

	private void SelectPet(ColorChip sender, PetBehaviour pet)
	{
		uint petId = (SaveManager.LastPet = DestroyableSingleton<HatManager>.Instance.GetIdFromPet(pet));
		PlayerControl.SetPetImage(pet, PlayerControl.LocalPlayer.Data.ColorId, PetImage);
		if ((bool)PlayerControl.LocalPlayer)
		{
			PlayerControl.LocalPlayer.RpcSetPet(petId);
		}
		for (int i = 0; i < ColorChips.Count; i++)
		{
			ColorChip colorChip = ColorChips[i];
			colorChip.InUseForeground.SetActive(colorChip == sender);
		}
	}
}
