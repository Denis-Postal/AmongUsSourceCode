using System;
using GoogleMobileAds.Common;

namespace GoogleMobileAds.Api
{
	public class MobileAds
	{
		public static class Utils
		{
			public static float GetDeviceScale()
			{
				return client.GetDeviceScale();
			}
		}

		private static readonly IMobileAdsClient client = GetMobileAdsClient();

		public static void Initialize(string appId)
		{
			client.Initialize(appId);
			MobileAdsEventExecutor.Initialize();
		}

		public static void Initialize(Action<InitializationStatus> initCompleteAction)
		{
			client.Initialize(initCompleteAction);
			MobileAdsEventExecutor.Initialize();
		}

		public static void SetApplicationMuted(bool muted)
		{
			client.SetApplicationMuted(muted);
		}

		public static void SetApplicationVolume(float volume)
		{
			client.SetApplicationVolume(volume);
		}

		public static void SetiOSAppPauseOnBackground(bool pause)
		{
			client.SetiOSAppPauseOnBackground(pause);
		}

		private static IMobileAdsClient GetMobileAdsClient()
		{
			return GoogleMobileAdsClientFactory.MobileAdsInstance();
		}
	}
}
