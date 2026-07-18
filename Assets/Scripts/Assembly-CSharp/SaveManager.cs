using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;
using UnityEngine.Analytics;

public static class SaveManager
{
	private class SecureDataFile
	{
		private string filePath;

		public bool Loaded { get; private set; }

		public SecureDataFile(string filePath)
		{
			this.filePath = filePath;
		}

		public void LoadData(Action<BinaryReader> performRead)
		{
			Loaded = true;
			Debug.Log("Loading secure: " + filePath);
			if (!File.Exists(filePath))
			{
				return;
			}
			byte[] array = File.ReadAllBytes(filePath);
			for (int i = 0; i < array.Length; i++)
			{
				array[i] ^= (byte)(i % 212);
			}
			try
			{
				using (MemoryStream input = new MemoryStream(array))
				{
					using (BinaryReader binaryReader = new BinaryReader(input))
					{
						if (binaryReader.ReadString() != SystemInfo.deviceUniqueIdentifier)
						{
							Debug.Log("Invalid secure file");
							Analytics.CustomEvent("MismatchSave", new Dictionary<string, object> { 
							{
								"Language",
								Application.systemLanguage
							} });
						}
						performRead(binaryReader);
					}
				}
			}
			catch
			{
				Debug.Log("Deleted corrupt secure file inner");
				Analytics.CustomEvent("CorruptedSave", new Dictionary<string, object> { 
				{
					"Language",
					Application.systemLanguage
				} });
				Delete();
			}
		}

		public void SaveData(params object[] items)
		{
			byte[] array;
			using (MemoryStream memoryStream = new MemoryStream())
			{
				using (BinaryWriter binaryWriter = new BinaryWriter(memoryStream))
				{
					binaryWriter.Write(SystemInfo.deviceUniqueIdentifier);
					foreach (object obj in items)
					{
						if (obj is long)
						{
							binaryWriter.Write((long)obj);
						}
						else
						{
							if (!(obj is HashSet<string>))
							{
								continue;
							}
							foreach (string item in (HashSet<string>)obj)
							{
								binaryWriter.Write(item);
							}
						}
					}
					binaryWriter.Flush();
					memoryStream.Position = 0L;
					array = memoryStream.ToArray();
				}
			}
			for (int j = 0; j < array.Length; j++)
			{
				array[j] ^= (byte)(j % 212);
			}
			File.WriteAllBytes(filePath, array);
		}

		public void Delete()
		{
			try
			{
				File.Delete(filePath);
			}
			catch
			{
			}
		}
	}

	private static bool loaded;

	private static bool loadedStats;

	private static bool loadedAnnounce;

	private static string lastPlayerName;

	private static byte sfxVolume = byte.MaxValue;

	private static byte musicVolume = byte.MaxValue;

	private static bool showMinPlayerWarning;

	private static bool showOnlineHelp = true;

	private static byte showAdsScreen = 0;

	private static bool censorChat = true;

	private static int touchConfig;

	private static float joyStickSize = 1f;

	private static uint colorConfig;

	private static uint lastPet;

	private static uint lastHat;

	private static uint lastSkin;

	private static uint lastLanguage;

	private static int shadowQuality = 1;

	private static GameOptionsData hostOptionsData;

	private static GameOptionsData searchOptionsData;

	private static Announcement lastAnnounce;

	private static SecureDataFile purchaseFile = new SecureDataFile(Path.Combine(Application.persistentDataPath, "secureNew"));

	private static HashSet<string> purchases = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

	public static FreeWeekendState IsFreeWeekend = FreeWeekendState.NotFree;

	public static Announcement LastAnnouncement
	{
		get
		{
			LoadAnnouncement();
			return lastAnnounce;
		}
		set
		{
			lastAnnounce = value;
			SaveAnnouncement();
		}
	}

	public static bool BoughtNoAds
	{
		get
		{
			LoadSecureData();
			return purchases.Contains("bought_ads");
		}
	}

	public static bool CensorChat
	{
		get
		{
			LoadPlayerPrefs();
			return censorChat;
		}
		set
		{
			LoadPlayerPrefs();
			censorChat = value;
			SavePlayerPrefs();
		}
	}

	public static ShowAdsState ShowAdsScreen
	{
		get
		{
			LoadPlayerPrefs();
			return (ShowAdsState)showAdsScreen;
		}
		set
		{
			LoadPlayerPrefs();
			showAdsScreen = (byte)value;
			SavePlayerPrefs();
		}
	}

	public static bool ShowMinPlayerWarning
	{
		get
		{
			LoadPlayerPrefs();
			return showMinPlayerWarning;
		}
		set
		{
			LoadPlayerPrefs();
			showMinPlayerWarning = value;
			SavePlayerPrefs();
		}
	}

	public static bool ShowOnlineHelp
	{
		get
		{
			LoadPlayerPrefs();
			return showOnlineHelp;
		}
		set
		{
			LoadPlayerPrefs();
			showOnlineHelp = value;
			SavePlayerPrefs();
		}
	}

	public static float SfxVolume
	{
		get
		{
			LoadPlayerPrefs();
			return (float)(int)sfxVolume / 255f;
		}
		set
		{
			LoadPlayerPrefs();
			sfxVolume = (byte)(value * 255f);
			SavePlayerPrefs();
		}
	}

	public static float MusicVolume
	{
		get
		{
			LoadPlayerPrefs();
			return (float)(int)musicVolume / 255f;
		}
		set
		{
			LoadPlayerPrefs();
			musicVolume = (byte)(value * 255f);
			SavePlayerPrefs();
		}
	}

	public static int TouchConfig
	{
		get
		{
			LoadPlayerPrefs();
			return touchConfig;
		}
		set
		{
			LoadPlayerPrefs();
			touchConfig = value;
			SavePlayerPrefs();
		}
	}

	public static float JoystickSize
	{
		get
		{
			LoadPlayerPrefs();
			return joyStickSize;
		}
		set
		{
			LoadPlayerPrefs();
			joyStickSize = value;
			SavePlayerPrefs();
		}
	}

	public static string PlayerName
	{
		get
		{
			LoadPlayerPrefs();
			if (!string.IsNullOrWhiteSpace(lastPlayerName))
			{
				return lastPlayerName;
			}
			return DestroyableSingleton<TranslationController>.Instance.GetString(StringNames.EnterName);
		}
		set
		{
			LoadPlayerPrefs();
			lastPlayerName = value;
			SavePlayerPrefs();
		}
	}

	public static uint LastPet
	{
		get
		{
			LoadPlayerPrefs();
			return lastPet;
		}
		set
		{
			LoadPlayerPrefs();
			lastPet = value;
			SavePlayerPrefs();
		}
	}

	public static uint LastHat
	{
		get
		{
			LoadPlayerPrefs();
			return lastHat;
		}
		set
		{
			LoadPlayerPrefs();
			lastHat = value;
			SavePlayerPrefs();
		}
	}

	public static uint LastSkin
	{
		get
		{
			LoadPlayerPrefs();
			return lastSkin;
		}
		set
		{
			LoadPlayerPrefs();
			lastSkin = value;
			SavePlayerPrefs();
		}
	}

	public static uint LastLanguage
	{
		get
		{
			LoadPlayerPrefs();
			if (lastLanguage > 5)
			{
				lastLanguage = TranslationController.SelectDefaultLanguage();
			}
			return lastLanguage;
		}
		set
		{
			LoadPlayerPrefs();
			lastLanguage = value;
			SavePlayerPrefs();
		}
	}

	public static int ShadowQuality
	{
		get
		{
			LoadPlayerPrefs();
			return Mathf.Clamp(shadowQuality, 0, 2);
		}
		set
		{
			LoadPlayerPrefs();
			shadowQuality = Mathf.Clamp(value, 0, 2);
			SavePlayerPrefs();
		}
	}

	public static byte BodyColor
	{
		get
		{
			LoadPlayerPrefs();
			return (byte)(colorConfig & 0xFF);
		}
		set
		{
			LoadPlayerPrefs();
			colorConfig = (colorConfig & 0xFFFF00) | (uint)(value & 0xFF);
			SavePlayerPrefs();
		}
	}

	public static GameOptionsData GameHostOptions
	{
		get
		{
			if (hostOptionsData == null)
			{
				hostOptionsData = LoadGameOptions("gameHostOptions");
			}
			hostOptionsData.NumImpostors = Mathf.Clamp(hostOptionsData.NumImpostors, 1, 3);
			hostOptionsData.KillDistance = Mathf.Clamp(hostOptionsData.KillDistance, 0, 2);
			if (!GetMapPurchased(hostOptionsData.MapId))
			{
				hostOptionsData.MapId = 0;
			}
			return hostOptionsData;
		}
		set
		{
			hostOptionsData = value;
			SaveGameOptions(hostOptionsData, "gameHostOptions");
		}
	}

	public static GameOptionsData GameSearchOptions
	{
		get
		{
			if (searchOptionsData == null)
			{
				searchOptionsData = LoadGameOptions("gameSearchOptions");
			}
			if (!GetMapPurchased(1))
			{
				searchOptionsData.MapId = 1;
			}
			return searchOptionsData;
		}
		set
		{
			searchOptionsData = value;
			SaveGameOptions(searchOptionsData, "gameSearchOptions");
		}
	}

	public static uint GetMapPurchaseField()
	{
		return (uint)((GetMapPurchased(0) ? 1 : 0) | (GetMapPurchased(1) ? 2 : 0));
	}

	public static bool GetMapPurchased(int id)
	{
		switch (id)
		{
		case 0:
			return true;
		case 1:
			if (!IsFreeWeekend.HasFlag(FreeWeekendState.FreeMIRA))
			{
				return GetPurchase("map_mira");
			}
			return true;
		default:
			return false;
		}
	}

	public static bool GetPurchase(string key)
	{
		return true;
	}

	public static void ClearPurchased(string key)
	{
		LoadSecureData();
		purchases.Remove(key);
		SaveSecureData();
	}

	public static void SetPurchased(string key)
	{
		LoadSecureData();
		purchases.Add(key ?? "null");
		if (key == "bought_ads")
		{
			ShowAdsScreen = ShowAdsState.Purchased;
		}
		SaveSecureData();
	}

	private static void LoadSecureData()
	{
		if (purchaseFile.Loaded)
		{
			return;
		}
		try
		{
			purchaseFile.LoadData(delegate(BinaryReader reader)
			{
				while (reader.BaseStream.Position < reader.BaseStream.Length)
				{
					purchases.Add(reader.ReadString());
				}
			});
		}
		catch (NullReferenceException)
		{
		}
		catch (Exception ex2)
		{
			Debug.Log("Deleted corrupt secure file outer: " + ex2);
			purchaseFile.Delete();
		}
	}

	private static void SaveSecureData()
	{
		purchaseFile.SaveData(purchases);
	}

	private static GameOptionsData LoadGameOptions(string filename)
	{
		string path = Path.Combine(Application.persistentDataPath, filename);
		if (File.Exists(path))
		{
			using (FileStream input = File.OpenRead(path))
			{
				using (BinaryReader reader = new BinaryReader(input))
				{
					return GameOptionsData.Deserialize(reader) ?? new GameOptionsData();
				}
			}
		}
		return new GameOptionsData();
	}

	private static void SaveGameOptions(GameOptionsData data, string filename)
	{
		using (FileStream output = new FileStream(Path.Combine(Application.persistentDataPath, filename), FileMode.Create, FileAccess.Write))
		{
			using (BinaryWriter writer = new BinaryWriter(output))
			{
				data.Serialize(writer);
			}
		}
	}

	private static void LoadAnnouncement()
	{
		if (loadedAnnounce)
		{
			return;
		}
		loadedAnnounce = true;
		string path = Path.Combine(Application.persistentDataPath, "announcement");
		if (File.Exists(path))
		{
			string[] array = File.ReadAllText(path).Split(default(char));
			if (array.Length == 3)
			{
				Announcement announcement = default(Announcement);
				TryGetUint(array, 0, out announcement.Id);
				announcement.AnnounceText = array[1];
				TryGetDateTime(array, 2, out announcement.DateFetched);
				lastAnnounce = announcement;
			}
			else
			{
				lastAnnounce = default(Announcement);
			}
		}
	}

	public static void SaveAnnouncement()
	{
		File.WriteAllText(Path.Combine(Application.persistentDataPath, "announcement"), string.Join("\0", lastAnnounce.Id, lastAnnounce.AnnounceText, lastAnnounce.DateFetched));
	}

	private static void LoadPlayerPrefs()
	{
		if (loaded)
		{
			return;
		}
		loaded = true;
		string path = Path.Combine(Application.persistentDataPath, "playerPrefs");
		if (File.Exists(path))
		{
			string[] array = File.ReadAllText(path).Split(',');
			lastPlayerName = array[0];
			if (array.Length > 1)
			{
				int.TryParse(array[1], out touchConfig);
			}
			if (array.Length <= 2 || !uint.TryParse(array[2], out colorConfig))
			{
				colorConfig = (uint)((byte)(Palette.PlayerColors.RandomIdx() << 16) | (byte)(Palette.PlayerColors.RandomIdx() << 8) | (byte)Palette.PlayerColors.RandomIdx());
			}
			TryGetByte(array, 7, out showAdsScreen);
			TryGetBool(array, 8, out showMinPlayerWarning);
			TryGetBool(array, 9, out showOnlineHelp);
			TryGetUint(array, 10, out lastHat);
			TryGetByte(array, 11, out sfxVolume);
			TryGetByte(array, 12, out musicVolume);
			TryGetFloat(array, 13, out joyStickSize, 1f);
			TryGetUint(array, 15, out lastSkin);
			TryGetUint(array, 16, out lastPet);
			TryGetBool(array, 17, out censorChat, @default: true);
			TryGetUint(array, 18, out lastLanguage, uint.MaxValue);
			TryGetInt(array, 19, out shadowQuality, 1);
		}
	}

	private static void SavePlayerPrefs()
	{
		LoadPlayerPrefs();
		File.WriteAllText(Path.Combine(Application.persistentDataPath, "playerPrefs"), string.Join(",", lastPlayerName, touchConfig, colorConfig, (byte)1, false, false, false, showAdsScreen, showMinPlayerWarning, showOnlineHelp, lastHat, sfxVolume, musicVolume, joyStickSize.ToString(CultureInfo.InvariantCulture), 0L, lastSkin, lastPet, censorChat, lastLanguage, shadowQuality));
	}

	private static void TryGetBool(string[] parts, int index, out bool value, bool @default = false)
	{
		value = @default;
		if (parts.Length > index)
		{
			bool.TryParse(parts[index], out value);
		}
	}

	private static void TryGetByte(string[] parts, int index, out byte value)
	{
		value = 0;
		if (parts.Length > index)
		{
			byte.TryParse(parts[index], out value);
		}
	}

	private static void TryGetFloat(string[] parts, int index, out float value, float @default = 0f)
	{
		value = @default;
		if (parts.Length > index)
		{
			float.TryParse(parts[index], NumberStyles.Number, CultureInfo.InvariantCulture, out value);
		}
	}

	private static void TryGetInt(string[] parts, int index, out int value, int @default = 0)
	{
		value = @default;
		if (parts.Length > index)
		{
			int parsedValue;
			if (int.TryParse(parts[index], out parsedValue))
			{
				value = parsedValue;
			}
		}
	}

	private static void TryGetDateTime(string[] parts, int index, out DateTime value)
	{
		value = default(DateTime);
		if (parts.Length > index)
		{
			DateTime.TryParse(parts[index], out value);
		}
	}

	private static void TryGetUint(string[] parts, int index, out uint value, uint @default = 0u)
	{
		value = @default;
		if (parts.Length > index)
		{
			uint.TryParse(parts[index], out value);
		}
	}

	private static void TryGetDateTicks(string[] parts, int index, out DateTime value)
	{
		value = DateTime.MinValue;
		if (parts.Length > index && long.TryParse(parts[index], out var result))
		{
			value = new DateTime(result, DateTimeKind.Utc);
		}
	}
}
