using System;
using GoogleMobileAds.Api;

namespace GoogleMobileAds.Common
{
	public interface IMobileAdsClient
	{
		void Initialize(string appId);

		void Initialize(Action<InitializationStatus> initCompleteAction);

		void SetApplicationVolume(float volume);

		void SetApplicationMuted(bool muted);

		void SetiOSAppPauseOnBackground(bool pause);

		float GetDeviceScale();
	}
}
