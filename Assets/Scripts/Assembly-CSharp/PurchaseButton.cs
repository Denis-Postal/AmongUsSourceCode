using UnityEngine;

public class PurchaseButton : MonoBehaviour
{
	private const float BorderSize = 0.7f;

	public SpriteRenderer PurchasedIcon;

	public TextRenderer NameText;

	public SpriteRenderer HatImage;

	public Sprite MannequinFrame;

	public SpriteRenderer Background;

	public IBuyable Product;

	public bool Purchased;

	public string Name;

	public string Price;

	public string ProductId;

	public StoreMenu Parent { get; set; }

	public void SetItem(IBuyable product, string productId, string name, string price, bool purchased)
	{
		Product = product;
		Purchased = purchased;
		Name = name;
		Price = price;
		ProductId = productId;
		PurchasedIcon.enabled = Purchased;
		if (Product is HatBehaviour)
		{
			HatBehaviour hat = (HatBehaviour)Product;
			NameText.gameObject.SetActive(value: false);
			HatImage.transform.parent.gameObject.SetActive(value: true);
			PlayerControl.SetHatImage(hat, HatImage);
			SetSquare();
		}
		else if (Product is SkinData)
		{
			SkinData skin = (SkinData)Product;
			NameText.gameObject.SetActive(value: false);
			HatImage.transform.parent.gameObject.SetActive(value: true);
			HatImage.transform.parent.GetComponent<SpriteRenderer>().sprite = MannequinFrame;
			HatImage.transform.parent.localPosition = new Vector3(0f, 0f, -0.01f);
			HatImage.transform.parent.localScale = Vector3.one * 0.3f;
			HatImage.transform.localPosition = new Vector3(0f, 0f, -0.01f);
			HatImage.transform.localScale = Vector3.one * 2f;
			PlayerControl.SetSkinImage(skin, HatImage);
			SetSquare();
		}
		else if (Product is PetBehaviour)
		{
			PetBehaviour petBehaviour = (PetBehaviour)Product;
			NameText.gameObject.SetActive(value: false);
			HatImage.transform.parent.gameObject.SetActive(value: true);
			HatImage.transform.parent.GetComponent<SpriteRenderer>().enabled = false;
			HatImage.material = new Material(petBehaviour.rend.sharedMaterial);
			PlayerControl.SetPetImage(petBehaviour, SaveManager.BodyColor, HatImage);
			SetSquare();
		}
		else if (Product is MapBuyable)
		{
			MapBuyable mapBuyable = (MapBuyable)Product;
			NameText.Text = mapBuyable.SubText;
			NameText.Centered = false;
			NameText.scaleToFit = true;
			NameText.maxWidth = 2.8f;
			NameText.transform.localPosition = new Vector3(-1.4f, -0.75f, -0.01f);
			NameText.Color = Color.black;
			NameText.OutlineColor = Color.clear;
			HatImage.transform.parent.gameObject.SetActive(value: true);
			HatImage.sprite = mapBuyable.StoreImage;
			HatImage.transform.parent.GetComponent<SpriteRenderer>().enabled = false;
			SetBig();
			Background.enabled = false;
		}
		else
		{
			NameText.Text = Name;
		}
	}

	private void SetBig()
	{
		Background.size = new Vector2(2.8f, 1.4f);
		Background.GetComponent<BoxCollider2D>().size = new Vector2(2.8f, 1.4f);
		PurchasedIcon.transform.localPosition = new Vector3(1.1f, -0.45f, -2f);
	}

	private void SetSquare()
	{
		Background.size = new Vector2(0.7f, 0.7f);
		Background.GetComponent<BoxCollider2D>().size = new Vector2(0.7f, 0.7f);
		PurchasedIcon.transform.localPosition = new Vector3(0f, 0f, -1f);
	}

	internal void SetPurchased()
	{
		Purchased = true;
		PurchasedIcon.enabled = true;
	}

	public void DoPurchase()
	{
		Parent.SetProduct(this);
	}
}
