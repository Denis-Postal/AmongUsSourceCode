using System.Collections.Generic;
using GoogleMobileAds.Api;

namespace GoogleMobileAds.Common
{
	internal interface IInitializationStatusClient
	{
		AdapterStatus getAdapterStatusForClassName(string className);

		Dictionary<string, AdapterStatus> getAdapterStatusMap();
	}
}
