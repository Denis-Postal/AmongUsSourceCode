#if STEAMWORKS_NET || STEAMWORKS
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Steamworks;
using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Extension;

internal class SteamPurchasingModule : IPurchasingModule, IStore
{
	public const string Name = "Steam";

	public Dictionary<string, ISteamBuyable> IdTranslator = new Dictionary<string, ISteamBuyable>(StringComparer.OrdinalIgnoreCase);

	private IStoreCallback storeCallback;

	private Callback<GameOverlayActivated_t> overlayCallback;

	private bool steamOverlayOpen;

	private StoreMenu parent;

	public SteamPurchasingModule(StoreMenu parent)
	{
		this.parent = parent;
	}

	public void Configure(IPurchasingBinder binder)
	{
		binder.RegisterStore("Steam", this);
	}

	public void FinishTransaction(ProductDefinition product, string transactionId)
	{
	}

	public void Initialize(IStoreCallback callback)
	{
		storeCallback = callback;
		if (!SteamManager.Initialized || !SteamUtils.IsOverlayEnabled())
		{
			storeCallback.OnSetupFailed(InitializationFailureReason.PurchasingUnavailable);
		}
		overlayCallback = Callback<GameOverlayActivated_t>.Create(HandleOverlayActivate);
	}

	private void HandleOverlayActivate(GameOverlayActivated_t param)
	{
		steamOverlayOpen = param.m_bActive != 0;
	}

	public void Purchase(ProductDefinition product, string developerPayload)
	{
		ISteamBuyable value;
		if (!SteamUtils.IsOverlayEnabled())
		{
			storeCallback.OnPurchaseFailed(new PurchaseFailureDescription(product.storeSpecificId, PurchaseFailureReason.PurchasingUnavailable, "Steam overlay is disabled, but required for in-game purchasing."));
		}
		else if (IdTranslator.TryGetValue(product.id, out value))
		{
			AppId_t appId_t = new AppId_t(value.SteamAppId);
			SteamFriends.ActivateGameOverlayToStore(appId_t, EOverlayToStoreFlag.k_EOverlayToStoreFlag_AddToCartAndShow);
			parent.StartCoroutine(WaitForDlcPurchase(product, appId_t));
		}
		else
		{
			storeCallback.OnPurchaseFailed(new PurchaseFailureDescription(product.storeSpecificId, PurchaseFailureReason.ProductUnavailable, "Couldn't find Product Id for " + product.id));
		}
	}

	private IEnumerator WaitForDlcPurchase(ProductDefinition product, AppId_t appId)
	{
		while (!steamOverlayOpen)
		{
			SteamAPI.RunCallbacks();
			yield return null;
		}
		while (steamOverlayOpen)
		{
			SteamAPI.RunCallbacks();
			yield return null;
		}
		ulong punBytesDownloaded;
		while (SteamApps.GetDlcDownloadProgress(appId, out punBytesDownloaded, out punBytesDownloaded))
		{
			yield return null;
		}
		if (SteamApps.BIsDlcInstalled(appId))
		{
			storeCallback.OnPurchaseSucceeded(product.id, "FakeReceipt", UnityEngine.Random.value.ToString());
		}
		else
		{
			storeCallback.OnPurchaseFailed(new PurchaseFailureDescription(product.id, PurchaseFailureReason.UserCancelled, "Steam overlay closed without purchase completing"));
		}
	}

	public void RetrieveProducts(ReadOnlyCollection<ProductDefinition> products)
	{
		if (!SteamManager.Initialized)
		{
			return;
		}
		List<ProductDescription> list = new List<ProductDescription>(products.Count);
		for (int i = 0; i < products.Count; i++)
		{
			ProductDefinition productDefinition = products[i];
			if (IdTranslator.TryGetValue(productDefinition.id, out var value))
			{
				bool flag = SteamApps.BIsDlcInstalled(new AppId_t(value.SteamAppId));
				list.Add(new ProductDescription(productDefinition.id, new ProductMetadata(value.SteamPrice, null, null, "USD", 1m), flag ? "Bought" : null, null));
			}
		}
		storeCallback.OnProductsRetrieved(list);
	}
}
#else
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Extension;

internal class SteamPurchasingModule : IPurchasingModule, IStore
{
	public const string Name = "Steam";

	public Dictionary<string, ISteamBuyable> IdTranslator = new Dictionary<string, ISteamBuyable>(StringComparer.OrdinalIgnoreCase);

	private IStoreCallback storeCallback;

	public SteamPurchasingModule(StoreMenu parent)
	{
	}

	public void Configure(IPurchasingBinder binder)
	{
		binder.RegisterStore("Steam", this);
	}

	public void FinishTransaction(ProductDefinition product, string transactionId)
	{
	}

	public void Initialize(IStoreCallback callback)
	{
		storeCallback = callback;
		storeCallback.OnSetupFailed(InitializationFailureReason.PurchasingUnavailable);
	}

	public void Purchase(ProductDefinition product, string developerPayload)
	{
		storeCallback?.OnPurchaseFailed(new PurchaseFailureDescription(product.storeSpecificId, PurchaseFailureReason.PurchasingUnavailable, "Steamworks.NET is not available."));
	}

	public void RetrieveProducts(ReadOnlyCollection<ProductDefinition> products)
	{
		storeCallback?.OnProductsRetrieved(new List<ProductDescription>());
	}
}
#endif
