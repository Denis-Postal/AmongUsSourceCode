using System;
using System.Collections.Generic;
using UnityEngine;

public class GameSettingMenu : MonoBehaviour
{
	public Transform[] AllItems;

	public float YStart;

	public float YOffset;

	public Transform[] HideForOnline;

	private void OnEnable()
	{
		int num = 0;
		for (int i = 0; i < AllItems.Length; i++)
		{
			Transform transform = AllItems[i];
			if (!transform.gameObject.activeSelf)
			{
				continue;
			}
			if (AmongUsClient.Instance.GameMode == GameModes.OnlineGame && HideForOnline.IndexOf(transform) != -1)
			{
				transform.gameObject.SetActive(value: false);
				continue;
			}
			if (transform.name.Equals("MapName", StringComparison.OrdinalIgnoreCase))
			{
				int num2 = 0;
				List<KeyValuePair<string, int>> list = new List<KeyValuePair<string, int>>();
				for (int j = 0; j < GameOptionsData.MapNames.Length; j++)
				{
					if (SaveManager.GetMapPurchased(j))
					{
						list.Add(new KeyValuePair<string, int>(GameOptionsData.MapNames[j], j));
						num2++;
					}
				}
				transform.GetComponent<KeyValueOption>().Values = list;
				if (num2 == 1)
				{
					transform.gameObject.SetActive(value: false);
					continue;
				}
			}
			Vector3 localPosition = transform.localPosition;
			localPosition.y = YStart - (float)num * YOffset;
			transform.localPosition = localPosition;
			num++;
		}
		GetComponent<Scroller>().YBounds.max = (float)num * YOffset - 2f * YStart - 0.1f;
	}
}
