using System;
using System.Collections;
using UnityEngine;

public class MainMenuManager : MonoBehaviour
{
	public AdDataCollectScreen AdsPolicy;

	public AnnouncementPopUp Announcement;

	public void Start()
	{
		StartCoroutine(Announcement.Init());
		RunStartUp();
	}

	private void RunStartUp()
	{
		DateTime utcNow = DateTime.UtcNow;
		for (int i = 0; i < DestroyableSingleton<HatManager>.Instance.AllHats.Count; i++)
		{
			HatBehaviour hatBehaviour = DestroyableSingleton<HatManager>.Instance.AllHats[i];
			if ((hatBehaviour.LimitedMonth == utcNow.Month || hatBehaviour.LimitedMonth == 0) && (hatBehaviour.LimitedYear == utcNow.Year || hatBehaviour.LimitedYear == 0) && !SaveManager.GetPurchase(hatBehaviour.ProductId))
			{
				SaveManager.SetPurchased(hatBehaviour.ProductId);
			}
		}
	}

	private void Update()
	{
		if (Input.GetKeyUp(KeyCode.Escape))
		{
			Application.Quit();
		}
	}
}
