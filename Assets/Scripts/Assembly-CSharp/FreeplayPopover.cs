using UnityEngine;

public class FreeplayPopover : MonoBehaviour
{
	public GameObject Content;

	public SpriteRenderer[] MapButtons;

	public HostGameButton HostGame;

	public void Show()
	{
		int num = 0;
		for (int i = 0; i < AmongUsClient.Instance.ShipPrefabs.Count; i++)
		{
			if (SaveManager.GetMapPurchased(i))
			{
				num++;
			}
			else if (i < MapButtons.Length)
			{
				MapButtons[i].gameObject.SetActive(value: false);
			}
		}
		if (num == 1)
		{
			HostGame.OnClick();
		}
		else
		{
			Content.SetActive(value: true);
		}
	}

	public void PlayMap(int i)
	{
		AmongUsClient.Instance.TutorialMapId = i;
		HostGame.OnClick();
	}
}
