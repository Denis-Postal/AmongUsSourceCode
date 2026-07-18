using System.Collections.Generic;
using UnityEngine;

public class PlayerTab : MonoBehaviour
{
	public ColorChip ColorTabPrefab;

	public SpriteRenderer DemoImage;

	public SpriteRenderer HatImage;

	public SpriteRenderer SkinImage;

	public SpriteRenderer PetImage;

	public FloatRange XRange = new FloatRange(1.5f, 3f);

	public FloatRange YRange = new FloatRange(-1f, -3f);

	private HashSet<int> AvailableColors = new HashSet<int>();

	private List<ColorChip> ColorChips = new List<ColorChip>();

	private const int Columns = 3;

	public void OnEnable()
	{
		PlayerControl.SetPlayerMaterialColors(PlayerControl.LocalPlayer.Data.ColorId, DemoImage);
		PlayerControl.SetHatImage(SaveManager.LastHat, HatImage);
		PlayerControl.SetSkinImage(SaveManager.LastSkin, SkinImage);
		PlayerControl.SetPetImage(SaveManager.LastPet, PlayerControl.LocalPlayer.Data.ColorId, PetImage);
		float num = (float)Palette.PlayerColors.Length / 3f;
		for (int i = 0; i < Palette.PlayerColors.Length; i++)
		{
			float x = XRange.Lerp((float)(i % 3) / 2f);
			float y = YRange.Lerp(1f - (float)(i / 3) / num);
			ColorChip colorChip = Object.Instantiate(ColorTabPrefab);
			colorChip.transform.SetParent(base.transform);
			colorChip.transform.localPosition = new Vector3(x, y, -1f);
			int j = i;
			colorChip.Button.OnClick.AddListener(delegate
			{
				SelectColor(j);
			});
			colorChip.Inner.color = Palette.PlayerColors[i];
			ColorChips.Add(colorChip);
		}
	}

	public void OnDisable()
	{
		for (int i = 0; i < ColorChips.Count; i++)
		{
			Object.Destroy(ColorChips[i].gameObject);
		}
		ColorChips.Clear();
	}

	public void Update()
	{
		UpdateAvailableColors();
		for (int i = 0; i < ColorChips.Count; i++)
		{
			ColorChips[i].InUseForeground.SetActive(!AvailableColors.Contains(i));
		}
	}

	private void SelectColor(int colorId)
	{
		UpdateAvailableColors();
		if (AvailableColors.Remove(colorId))
		{
			SaveManager.BodyColor = (byte)colorId;
			if ((bool)PlayerControl.LocalPlayer)
			{
				PlayerControl.LocalPlayer.CmdCheckColor((byte)colorId);
			}
		}
	}

	public void UpdateAvailableColors()
	{
		PlayerControl.SetPlayerMaterialColors(PlayerControl.LocalPlayer.Data.ColorId, DemoImage);
		PlayerControl.SetPetImage(SaveManager.LastPet, PlayerControl.LocalPlayer.Data.ColorId, PetImage);
		for (int i = 0; i < Palette.PlayerColors.Length; i++)
		{
			AvailableColors.Add(i);
		}
		if ((bool)GameData.Instance)
		{
			List<GameData.PlayerInfo> allPlayers = GameData.Instance.AllPlayers;
			for (int j = 0; j < allPlayers.Count; j++)
			{
				GameData.PlayerInfo playerInfo = allPlayers[j];
				AvailableColors.Remove(playerInfo.ColorId);
			}
		}
	}
}
