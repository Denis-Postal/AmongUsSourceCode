using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using PowerTools;
using UnityEngine;
using UnityEngine.Purchasing;

public class StoreMenu : MonoBehaviour, IStoreListener
{
	public SpriteRenderer HatSlot;

	public SpriteRenderer SkinSlot;

	public SpriteAnim PetSlot;

	public TextRenderer ItemName;

	public SpriteRenderer PurchaseBackground;

	public TextRenderer PriceText;

	public PurchaseButton PurchasablePrefab;

	public SpriteRenderer HortLinePrefab;

	public TextRenderer LoadingText;

	public TextRenderer RestorePurchasesButton;

	public GameObject RestorePurchasesObj;

	public SpriteRenderer BannerPrefab;

	public Sprite HatBanner;

	public Sprite SkinsBanner;

	public Sprite HolidayBanner;

	public Sprite PetsBanner;

	public SpriteRenderer TopArrow;

	public SpriteRenderer BottomArrow;

	public const string BoughtAdsProductId = "bought_ads";

	private IStoreController controller;

	private IExtensionProvider extensions;

	public Scroller Scroller;

	public Vector2 StartPositionVertical;

	public FloatRange XRange = new FloatRange(-1f, 1f);

	public int NumPerRow = 4;

	private PurchaseButton CurrentButton;

	private List<GameObject> AllObjects = new List<GameObject>();

	private const float NormalHeight = -0.45f;

	private const float BoxHeight = -0.75f;

	public PurchaseStates PurchaseState { get; private set; }

	public void Start()
	{
		PetSlot.gameObject.SetActive(value: false);
		SteamPurchasingModule steamPurchasingModule = new SteamPurchasingModule(this);
		foreach (PetBehaviour allPet in DestroyableSingleton<HatManager>.Instance.AllPets)
		{
			if (allPet.SteamId != 0)
			{
				steamPurchasingModule.IdTranslator.Add(allPet.ProdId, allPet);
			}
		}
		foreach (MapBuyable allMap in DestroyableSingleton<HatManager>.Instance.AllMaps)
		{
			if (allMap.SteamId != 0)
			{
				steamPurchasingModule.IdTranslator.Add(allMap.ProdId, allMap);
			}
		}
		ConfigurationBuilder configurationBuilder = ConfigurationBuilder.Instance(steamPurchasingModule);
		foreach (PetBehaviour allPet2 in DestroyableSingleton<HatManager>.Instance.AllPets)
		{
			if (!allPet2.Free)
			{
				configurationBuilder.AddProduct(allPet2.ProdId, ProductType.NonConsumable);
			}
		}
		foreach (MapBuyable allMap2 in DestroyableSingleton<HatManager>.Instance.AllMaps)
		{
			configurationBuilder.AddProduct(allMap2.ProdId, ProductType.NonConsumable);
		}
		UnityPurchasing.Initialize(this, configurationBuilder);
		PurchaseBackground.color = new Color(0.5f, 0.5f, 0.5f, 1f);
		PriceText.Color = new Color(0.8f, 0.8f, 0.8f, 1f);
		PriceText.Text = "";
	}

	public void Update()
	{
		TopArrow.enabled = !Scroller.AtTop;
		BottomArrow.enabled = !Scroller.AtBottom;
	}

	public void RestorePurchases()
	{
	}

	private void DestroySliderObjects()
	{
		for (int i = 0; i < AllObjects.Count; i++)
		{
			UnityEngine.Object.Destroy(AllObjects[i]);
		}
		AllObjects.Clear();
	}

	private void FinishRestoring()
	{
		ShowAllButtons();
		RestorePurchasesButton.Text = "Purchases Restored";
	}

	public void SetProduct(PurchaseButton button)
	{
		if (PurchaseState == PurchaseStates.Started)
		{
			return;
		}
		CurrentButton = button;
		if (CurrentButton.Product is HatBehaviour)
		{
			HatBehaviour hatBehaviour = (HatBehaviour)CurrentButton.Product;
			HatSlot.gameObject.SetActive(value: true);
			SkinSlot.gameObject.SetActive(value: false);
			PetSlot.gameObject.SetActive(value: false);
			PlayerControl.SetHatImage(hatBehaviour, HatSlot);
			ItemName.Text = (string.IsNullOrWhiteSpace(hatBehaviour.StoreName) ? hatBehaviour.name : hatBehaviour.StoreName);
			if ((bool)hatBehaviour.RelatedSkin)
			{
				ItemName.Text += " (Includes skin!)";
				SkinSlot.gameObject.SetActive(value: true);
				PlayerControl.SetSkinImage(hatBehaviour.RelatedSkin, SkinSlot);
			}
		}
		else if (CurrentButton.Product is SkinData)
		{
			SkinData skinData = (SkinData)CurrentButton.Product;
			SkinSlot.gameObject.SetActive(value: true);
			HatSlot.gameObject.SetActive(value: true);
			PetSlot.gameObject.SetActive(value: false);
			PlayerControl.SetHatImage(skinData.RelatedHat, HatSlot);
			PlayerControl.SetSkinImage(skinData, SkinSlot);
			ItemName.Text = (string.IsNullOrWhiteSpace(skinData.StoreName) ? skinData.name : skinData.StoreName);
		}
		else if (CurrentButton.Product is PetBehaviour)
		{
			PetBehaviour petBehaviour = (PetBehaviour)CurrentButton.Product;
			SkinSlot.gameObject.SetActive(value: false);
			HatSlot.gameObject.SetActive(value: false);
			PetSlot.gameObject.SetActive(value: true);
			SpriteRenderer component = PetSlot.GetComponent<SpriteRenderer>();
			component.material = new Material(petBehaviour.rend.sharedMaterial);
			PlayerControl.SetPlayerMaterialColors(SaveManager.BodyColor, component);
			PetSlot.Play(petBehaviour.idleClip);
			ItemName.Text = (string.IsNullOrWhiteSpace(petBehaviour.StoreName) ? petBehaviour.name : petBehaviour.StoreName);
		}
		else if (CurrentButton.Product is MapBuyable)
		{
			MapBuyable mapBuyable = (MapBuyable)CurrentButton.Product;
			SkinSlot.gameObject.SetActive(value: false);
			HatSlot.gameObject.SetActive(value: false);
			PetSlot.gameObject.SetActive(value: false);
			ItemName.Text = mapBuyable.StoreName;
		}
		else
		{
			HatSlot.gameObject.SetActive(value: false);
			SkinSlot.gameObject.SetActive(value: false);
			PetSlot.gameObject.SetActive(value: false);
			ItemName.Text = "Remove All Ads";
		}
		if (button.Purchased)
		{
			PurchaseBackground.color = new Color(0.5f, 0.5f, 0.5f, 1f);
			PriceText.Color = new Color(0.8f, 0.8f, 0.8f, 1f);
			PriceText.Text = "Owned";
		}
		else
		{
			PurchaseBackground.color = Color.white;
			PriceText.Color = Color.white;
			PriceText.Text = button.Price;
		}
	}

	public void BuyProduct()
	{
		if ((bool)CurrentButton && !CurrentButton.Purchased && PurchaseState != PurchaseStates.Started)
		{
			StartCoroutine(WaitForPurchaseAds(CurrentButton));
		}
	}

	public IEnumerator WaitForPurchaseAds(PurchaseButton button)
	{
		PurchaseState = PurchaseStates.Started;
		controller.InitiatePurchase(button.ProductId);
		while (PurchaseState == PurchaseStates.Started)
		{
			yield return null;
		}
		if (PurchaseState == PurchaseStates.Success)
		{
			foreach (PurchaseButton item in from p in AllObjects
				select p.GetComponent<PurchaseButton>() into h
				where (bool)h && h.ProductId == button.ProductId
				select h)
			{
				item.SetPurchased();
			}
		}
		SetProduct(button);
	}

	public void Close()
	{
		HatsTab hatsTab = UnityEngine.Object.FindObjectOfType<HatsTab>();
		if ((bool)hatsTab)
		{
			hatsTab.OnDisable();
			hatsTab.OnEnable();
		}
		base.gameObject.SetActive(value: false);
	}

	private void ShowAllButtons()
	{
		DestroySliderObjects();
		string text = "";
		try
		{
			text = "Couldn't fetch products";
			Product[] all = controller.products.all;
			foreach (Product product in all)
			{
				if (product != null && product.hasReceipt)
				{
					try
					{
						SaveManager.SetPurchased(product.definition.id);
					}
					catch
					{
					}
				}
			}
			Vector3 position = StartPositionVertical;
			UnityEngine.Object.Destroy(RestorePurchasesObj);
			text = "Couldn't fetch products";
			text = "Couldn't fetch map data";
			position.y += -0.375f;
			List<MapBuyable> allMaps = DestroyableSingleton<HatManager>.Instance.AllMaps;
			position = InsertMapsFromList(position, all, allMaps);
			text = "Couldn't fetch pet data";
			position.y += -0.375f;
			PetBehaviour[] array = (from p in DestroyableSingleton<HatManager>.Instance.AllPets
				where !p.Free
				orderby p.StoreName
				select p).ToArray();
			position = InsertBanner(position, PetsBanner);
			Vector3 position2 = position;
			IBuyable[] hats = array;
			position = InsertHatsFromList(position2, all, hats);
			text = "Couldn't finalize menu";
			Scroller.YBounds.max = Mathf.Max(0f, 0f - position.y - 2.5f);
			try
			{
				LoadingText.gameObject.SetActive(value: false);
			}
			catch
			{
			}
		}
		catch (Exception ex)
		{
			Debug.Log("Exception: " + text + ": " + ex);
			DestroySliderObjects();
			LoadingText.Text = "Loading Failed:\r\n" + text;
			LoadingText.gameObject.SetActive(value: true);
		}
	}

	private Vector3 InsertHortLine(Vector3 position)
	{
		position.x = 1.2f;
		SpriteRenderer spriteRenderer = UnityEngine.Object.Instantiate(HortLinePrefab, Scroller.Inner);
		spriteRenderer.transform.localPosition = position;
		spriteRenderer.gameObject.SetActive(value: true);
		position.y += -0.33749998f;
		return position;
	}

	private Vector3 InsertMapsFromList(Vector3 position, Product[] allProducts, List<MapBuyable> maps)
	{
		position.y += -0.1875f;
		for (int i = 0; i < maps.Count; i++)
		{
			IBuyable item = maps[i];
			Product product = allProducts.FirstOrDefault((Product p) => item.ProdId == p.definition.id);
			if (product != null && product.definition != null && product.availableToPurchase)
			{
				position.x = StartPositionVertical.x + XRange.Lerp(0.5f);
				InsertProduct(position, product, item);
				position.y += -1.5749999f;
			}
		}
		return position;
	}

	private Vector3 InsertHatsFromList(Vector3 position, Product[] allProducts, IBuyable[] hats)
	{
		int num = 0;
		foreach (IBuyable item in hats)
		{
			Product product = allProducts.FirstOrDefault((Product p) => item.ProdId == p.definition.id);
			if (product != null && product.definition != null && product.availableToPurchase)
			{
				int num2 = num % NumPerRow;
				position.x = StartPositionVertical.x + XRange.Lerp((float)num2 / ((float)NumPerRow - 1f));
				if (num2 == 0 && num > 1)
				{
					position.y += -0.75f;
				}
				InsertProduct(position, product, item);
				num++;
			}
		}
		position.y += -0.75f;
		return position;
	}

	private void InsertProduct(Vector3 position, Product product, IBuyable item)
	{
		PurchaseButton purchaseButton = UnityEngine.Object.Instantiate(PurchasablePrefab, Scroller.Inner);
		AllObjects.Add(purchaseButton.gameObject);
		purchaseButton.transform.localPosition = position;
		purchaseButton.Parent = this;
		purchaseButton.SetItem(item, product.definition.id, product.metadata?.localizedTitle?.Replace("(Among Us)", ""), product.metadata?.localizedPriceString, product.hasReceipt || SaveManager.GetPurchase(product.definition.id));
	}

	private Vector3 InsertBanner(Vector3 position, Sprite s)
	{
		position.x = StartPositionVertical.x;
		SpriteRenderer spriteRenderer = UnityEngine.Object.Instantiate(BannerPrefab, Scroller.Inner);
		spriteRenderer.sprite = s;
		spriteRenderer.transform.localPosition = position;
		position.y += 0f - spriteRenderer.sprite.bounds.size.y;
		AllObjects.Add(spriteRenderer.gameObject);
		return position;
	}

	public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
	{
		this.controller = controller;
		this.extensions = extensions;
		if (this.controller == null || this.controller.products == null)
		{
			LoadingText.Text = "Product controller\r\nfailed to load";
		}
		else
		{
			ShowAllButtons();
		}
	}

	public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs e)
	{
		PurchaseState = PurchaseStates.Success;
		SaveManager.SetPurchased(e.purchasedProduct.definition.id);
		return PurchaseProcessingResult.Complete;
	}

	public void OnInitializeFailed(InitializationFailureReason error)
	{
		RestorePurchasesObj.SetActive(value: false);
		LoadingText.gameObject.SetActive(value: true);
		switch (error)
		{
		case InitializationFailureReason.NoProductsAvailable:
			LoadingText.Text = "Coming Soon!";
			break;
		case InitializationFailureReason.PurchasingUnavailable:
			LoadingText.Text = "Loading Failed:\r\nSteam must be running and logged in to view products.";
			break;
		default:
			LoadingText.Text = "Loading Failed:\r\n" + error;
			break;
		}
	}

	public void OnPurchaseFailed(Product i, PurchaseFailureReason error)
	{
		switch (error)
		{
		case PurchaseFailureReason.ProductUnavailable:
			DestroySliderObjects();
			LoadingText.gameObject.SetActive(value: true);
			LoadingText.Text = "Coming Soon!";
			break;
		case PurchaseFailureReason.PurchasingUnavailable:
			DestroySliderObjects();
			LoadingText.gameObject.SetActive(value: true);
			LoadingText.Text = "Steam overlay is required for in-game purchasing. You can still buy and install DLC in Steam.";
			break;
		default:
			DestroySliderObjects();
			LoadingText.gameObject.SetActive(value: true);
			LoadingText.Text = "Loading Failed:\r\n" + error;
			break;
		}
		Debug.LogError("Failed: " + error);
		PurchaseState = PurchaseStates.Fail;
	}
}
