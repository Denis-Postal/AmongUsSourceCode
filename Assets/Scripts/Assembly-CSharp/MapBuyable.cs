using UnityEngine;

[CreateAssetMenu]
public class MapBuyable : ScriptableObject, IBuyable, ISteamBuyable
{
	public string StoreName;

	[Multiline]
	public string SubText;

	public string productId;

	public uint SteamId;

	public Sprite StoreImage;

	public string ProdId => productId;

	public string SteamPrice => "$3.99";

	public uint SteamAppId => SteamId;
}
