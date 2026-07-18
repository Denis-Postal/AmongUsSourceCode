using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class HatManager : DestroyableSingleton<HatManager>
{
	public HatBehaviour NoneHat;

	public List<PetBehaviour> AllPets = new List<PetBehaviour>();

	public List<HatBehaviour> AllHats = new List<HatBehaviour>();

	public List<SkinData> AllSkins = new List<SkinData>();

	public List<MapBuyable> AllMaps = new List<MapBuyable>();

	internal PetBehaviour GetPetById(uint petId)
	{
		if (petId >= AllPets.Count)
		{
			return AllPets[0];
		}
		return AllPets[(int)petId];
	}

	public uint GetIdFromPet(PetBehaviour pet)
	{
		return (uint)AllPets.FindIndex((PetBehaviour p) => p.idleClip == pet.idleClip);
	}

	public PetBehaviour[] GetUnlockedPets()
	{
		return AllPets.Where((PetBehaviour h) => h.Free || SaveManager.GetPurchase(h.ProductId)).ToArray();
	}

	public HatBehaviour GetHatById(uint hatId)
	{
		if (hatId >= AllHats.Count)
		{
			return NoneHat;
		}
		return AllHats[(int)hatId];
	}

	public HatBehaviour[] GetUnlockedHats()
	{
		return (from o in AllHats
			where o.LimitedMonth == 0 || SaveManager.GetPurchase(o.ProductId)
			orderby o.Order descending, o.name
			select o).ToArray();
	}

	public uint GetIdFromHat(HatBehaviour hat)
	{
		return (uint)AllHats.IndexOf(hat);
	}

	public SkinData[] GetUnlockedSkins()
	{
		return (from o in AllSkins
			orderby o.Order descending, o.name
			select o).ToArray();
	}

	public uint GetIdFromSkin(SkinData skin)
	{
		return (uint)AllSkins.IndexOf(skin);
	}

	internal SkinData GetSkinById(uint skinId)
	{
		if (skinId >= AllSkins.Count)
		{
			return AllSkins[0];
		}
		return AllSkins[(int)skinId];
	}

	internal void SetSkin(SpriteRenderer skinRend, uint skinId)
	{
		SkinData skinById = GetSkinById(skinId);
		if ((bool)skinById)
		{
			skinRend.sprite = skinById.IdleFrame;
		}
	}
}
