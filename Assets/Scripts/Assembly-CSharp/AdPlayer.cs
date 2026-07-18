using System;
using System.Collections;
using GoogleMobileAds.Api;
using UnityEngine;

public static class AdPlayer
{
	private static InterstitialAd interstitial;

	private const string appId = "unexpected_platform";

	private const string adUnitId = "unexpected_platform";

	public static void ShowInterstitial(MonoBehaviour parent, bool playAgain)
	{
		parent.StartCoroutine(CoShowAd(playAgain));
	}

	private static IEnumerator CoShowAd(bool playAgain)
	{
		if (playAgain)
		{
			yield return DestroyableSingleton<EndGameManager>.Instance.CoJoinGame();
		}
		else
		{
			AmongUsClient.Instance.ExitGame();
		}
	}

	public static void RequestInterstitial()
	{
		try
		{
			MobileAds.Initialize("unexpected_platform");
			if (interstitial == null)
			{
				interstitial = new InterstitialAd("unexpected_platform");
				AdRequest adRequest = new AdRequest.Builder().Build();
				if (SaveManager.ShowAdsScreen.HasFlag(ShowAdsState.NonPersonalized))
				{
					adRequest.Extras.Add("npa", "1");
				}
				interstitial.OnAdLoaded += Interstitial_OnAdLoaded;
				interstitial.OnAdFailedToLoad += Interstitial_OnAdFailedToLoad;
				interstitial.OnAdClosed += Interstitial_OnAdClosed;
				interstitial.OnAdLeavingApplication += Interstitial_OnAdLeavingApplication;
				interstitial.LoadAd(adRequest);
			}
		}
		catch
		{
			try
			{
				if (interstitial != null)
				{
					interstitial.Destroy();
				}
			}
			catch
			{
			}
			interstitial = null;
		}
	}

	private static void Interstitial_OnAdLoaded(object sender, EventArgs e)
	{
	}

	private static void Interstitial_OnAdLeavingApplication(object sender, EventArgs e)
	{
		try
		{
			if (interstitial != null)
			{
				interstitial.Destroy();
			}
		}
		finally
		{
			interstitial = null;
		}
	}

	private static void Interstitial_OnAdFailedToLoad(object sender, AdFailedToLoadEventArgs e)
	{
		try
		{
			if (interstitial != null)
			{
				interstitial.Destroy();
				Debug.LogError("Couldn't load ad: " + (e.Message ?? "No Message"));
			}
		}
		finally
		{
			interstitial = null;
		}
	}

	private static void Interstitial_OnAdClosed(object sender, EventArgs e)
	{
		try
		{
			if (interstitial != null)
			{
				interstitial.Destroy();
			}
		}
		finally
		{
			interstitial = null;
		}
	}
}
