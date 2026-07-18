using System;
using UnityEngine;

public static class Constants
{
	public const string LocalNetAddress = "127.0.0.1";

	public const ushort GamePlayPort = 22023;

	public const ushort AnnouncementPort = 22024;

	public const string InfinitySymbol = "∞";

	public static readonly int ShipOnlyMask = LayerMask.GetMask("Ship");

	public static readonly int ShipAndObjectsMask = LayerMask.GetMask("Ship", "Objects");

	public static readonly int ShipAndAllObjectsMask = LayerMask.GetMask("Ship", "Objects", "ShortObjects");

	public static readonly int NotShipMask = ~LayerMask.GetMask("Ship");

	public static readonly int Usables = ~LayerMask.GetMask("Ship", "UI");

	public static readonly int PlayersOnlyMask = LayerMask.GetMask("Players", "Ghost");

	public static readonly int ShadowMask = LayerMask.GetMask("Shadow", "Objects", "IlluminatedBlocking");

	public static readonly int[] CompatVersions = new int[1] { GetBroadcastVersion() };

	public const int Year = 2019;

	public const int Month = 8;

	public const int Day = 16;

	public const int Revision = 0;

	internal static int GetBroadcastVersion()
	{
		return 50490200;
	}

	internal static int GetVersion(int year, int month, int day, int rev)
	{
		return year * 25000 + month * 1800 + day * 50 + rev;
	}

	internal static byte[] GetBroadcastVersionBytes()
	{
		return BitConverter.GetBytes(GetBroadcastVersion());
	}

	public static bool ShouldPlaySfx()
	{
		if ((bool)AmongUsClient.Instance && AmongUsClient.Instance.GameMode == GameModes.LocalGame)
		{
			return true;
		}
		return true;
	}
}
