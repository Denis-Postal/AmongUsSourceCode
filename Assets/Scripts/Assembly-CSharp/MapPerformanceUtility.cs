using UnityEngine;

public static class MapPerformanceUtility
{
	private static int cachedMapId = -999;

	private static bool cachedMiraHq;

	public static bool UseMiraHqAndroidOptimization()
	{
		if (Application.platform != RuntimePlatform.Android)
		{
			return false;
		}
		if ((bool)ShipStatus.Instance)
		{
			string shipName = ShipStatus.Instance.name;
			if (!string.IsNullOrEmpty(shipName) && (shipName.IndexOf("HeadQuarters") >= 0 || shipName.IndexOf("MIRA") >= 0 || shipName.IndexOf("Mira") >= 0))
			{
				return true;
			}
		}
		if (PlayerControl.GameOptions == null)
		{
			return false;
		}
		int mapId = PlayerControl.GameOptions.MapId;
		if (mapId == cachedMapId)
		{
			return cachedMiraHq;
		}
		cachedMapId = mapId;
		cachedMiraHq = false;
		if (GameOptionsData.MapNames != null && mapId >= 0 && mapId < GameOptionsData.MapNames.Length)
		{
			cachedMiraHq = GameOptionsData.MapNames[mapId] == "MIRA HQ";
		}
		return cachedMiraHq;
	}
}
