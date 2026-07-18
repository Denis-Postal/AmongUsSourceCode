#if STEAMWORKS_NET || STEAMWORKS
using Steamworks;
using UnityEngine;

public class SteamStoreRecovery : MonoBehaviour
{
	private void Start()
	{
		foreach (PetBehaviour allPet in DestroyableSingleton<HatManager>.Instance.AllPets)
		{
			if (allPet.SteamAppId != 0)
			{
				if (SteamApps.BIsDlcInstalled(new AppId_t(allPet.SteamAppId)))
				{
					SaveManager.SetPurchased(allPet.ProdId);
				}
				else
				{
					SaveManager.ClearPurchased(allPet.ProdId);
				}
			}
		}
		foreach (MapBuyable allMap in DestroyableSingleton<HatManager>.Instance.AllMaps)
		{
			if (SteamApps.BIsDlcInstalled(new AppId_t(allMap.SteamAppId)))
			{
				SaveManager.SetPurchased(allMap.ProdId);
			}
			else
			{
				SaveManager.ClearPurchased(allMap.ProdId);
			}
		}
	}
}
#else
using UnityEngine;

public class SteamStoreRecovery : MonoBehaviour
{
}
#endif
