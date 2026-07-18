using System.IO;
using System.Text;
using Hazel;
using InnerNet;
using UnityEngine;

public class GameOptionsData : IBytesSerializable
{
	private const byte GameDataVersion = 1;

	public static readonly string[] MapNames = new string[3] { "The Skeld", "MIRA HQ", "???" };

	public static readonly float[] KillDistances = new float[3] { 1f, 1.8f, 2.5f };

	public static readonly string[] KillDistanceStrings = new string[3] { "Short", "Normal", "Long" };

	public int MaxPlayers = 10;

	public GameKeywords Keywords = GameKeywords.English;

	public byte MapId;

	public float PlayerSpeedMod = 1f;

	public float CrewLightMod = 1f;

	public float ImpostorLightMod = 1.5f;

	public float KillCooldown = 15f;

	public int NumCommonTasks = 1;

	public int NumLongTasks = 1;

	public int NumShortTasks = 2;

	public int NumEmergencyMeetings = 1;

	public int EmergencyCooldown = 15;

	public int NumImpostors = 1;

	public bool GhostsDoTasks = true;

	public int KillDistance = 1;

	public int DiscussionTime = 15;

	public int VotingTime = 120;

	public bool isDefaults = true;

	private static readonly int[] RecommendedKillCooldown = new int[11]
	{
		0, 0, 0, 0, 45, 30, 15, 35, 30, 25,
		20
	};

	private static readonly int[] RecommendedImpostors = new int[11]
	{
		0, 0, 0, 0, 1, 1, 1, 2, 2, 2,
		2
	};

	private static readonly int[] MaxImpostors = new int[11]
	{
		0, 0, 0, 0, 1, 1, 1, 2, 2, 3,
		3
	};

	public static readonly int[] MinPlayers = new int[4] { 4, 4, 7, 9 };

	public void ToggleMapFilter(byte newId)
	{
		byte b = (byte)((MapId ^ (1 << (int)newId)) & 3);
		if (b != 0)
		{
			MapId = b;
		}
	}

	public bool FilterContainsMap(byte newId)
	{
		int num = 1 << (int)newId;
		return (MapId & num) == num;
	}

	public GameOptionsData()
	{
		try
		{
			switch (Application.systemLanguage)
			{
			case SystemLanguage.Portuguese:
				Keywords = GameKeywords.Portuguese;
				break;
			case SystemLanguage.Spanish:
				Keywords = GameKeywords.Spanish;
				break;
			case SystemLanguage.Korean:
				Keywords = GameKeywords.Korean;
				break;
			case SystemLanguage.Russian:
				Keywords = GameKeywords.Russian;
				break;
			}
		}
		catch
		{
		}
	}

	public void SetRecommendations(int numPlayers, GameModes modes)
	{
		numPlayers = Mathf.Clamp(numPlayers, 4, 10);
		PlayerSpeedMod = 1f;
		CrewLightMod = 1f;
		ImpostorLightMod = 1.5f;
		KillCooldown = RecommendedKillCooldown[numPlayers];
		NumCommonTasks = 1;
		NumLongTasks = 1;
		NumShortTasks = 2;
		NumEmergencyMeetings = 1;
		if (modes != GameModes.OnlineGame)
		{
			NumImpostors = RecommendedImpostors[numPlayers];
		}
		KillDistance = 1;
		DiscussionTime = 15;
		VotingTime = 120;
		isDefaults = true;
		EmergencyCooldown = ((modes == GameModes.OnlineGame) ? 15 : 0);
	}

	public void Serialize(BinaryWriter writer)
	{
		writer.Write((byte)1);
		writer.Write((byte)MaxPlayers);
		writer.Write((uint)Keywords);
		writer.Write(MapId);
		writer.Write(PlayerSpeedMod);
		writer.Write(CrewLightMod);
		writer.Write(ImpostorLightMod);
		writer.Write(KillCooldown);
		writer.Write((byte)NumCommonTasks);
		writer.Write((byte)NumLongTasks);
		writer.Write((byte)NumShortTasks);
		writer.Write(NumEmergencyMeetings);
		writer.Write((byte)NumImpostors);
		writer.Write((byte)KillDistance);
		writer.Write(DiscussionTime);
		writer.Write(VotingTime);
		writer.Write(isDefaults);
		writer.Write((byte)EmergencyCooldown);
	}

	public static GameOptionsData Deserialize(BinaryReader reader)
	{
		try
		{
			byte b = reader.ReadByte();
			if (b != 1 && b != 2)
			{
				return null;
			}
			GameOptionsData gameOptionsData = new GameOptionsData();
			gameOptionsData.MaxPlayers = reader.ReadByte();
			gameOptionsData.Keywords = (GameKeywords)reader.ReadUInt32();
			gameOptionsData.MapId = reader.ReadByte();
			gameOptionsData.PlayerSpeedMod = reader.ReadSingle();
			gameOptionsData.CrewLightMod = reader.ReadSingle();
			gameOptionsData.ImpostorLightMod = reader.ReadSingle();
			gameOptionsData.KillCooldown = reader.ReadSingle();
			gameOptionsData.NumCommonTasks = reader.ReadByte();
			gameOptionsData.NumLongTasks = reader.ReadByte();
			gameOptionsData.NumShortTasks = reader.ReadByte();
			gameOptionsData.NumEmergencyMeetings = reader.ReadInt32();
			gameOptionsData.NumImpostors = reader.ReadByte();
			gameOptionsData.KillDistance = reader.ReadByte();
			gameOptionsData.DiscussionTime = reader.ReadInt32();
			gameOptionsData.VotingTime = reader.ReadInt32();
			gameOptionsData.isDefaults = reader.ReadBoolean();
			try
			{
				gameOptionsData.EmergencyCooldown = reader.ReadByte();
			}
			catch
			{
			}
			return gameOptionsData;
		}
		catch
		{
		}
		return null;
	}

	public static GameOptionsData Deserialize(MessageReader reader)
	{
		try
		{
			if (reader.ReadByte() != 2)
			{
				return null;
			}
			return new GameOptionsData
			{
				MaxPlayers = reader.ReadByte(),
				Keywords = (GameKeywords)reader.ReadUInt32(),
				MapId = reader.ReadByte(),
				PlayerSpeedMod = reader.ReadSingle(),
				CrewLightMod = reader.ReadSingle(),
				ImpostorLightMod = reader.ReadSingle(),
				KillCooldown = reader.ReadSingle(),
				NumCommonTasks = reader.ReadByte(),
				NumLongTasks = reader.ReadByte(),
				NumShortTasks = reader.ReadByte(),
				NumEmergencyMeetings = reader.ReadInt32(),
				NumImpostors = reader.ReadByte(),
				KillDistance = reader.ReadByte(),
				DiscussionTime = reader.ReadInt32(),
				VotingTime = reader.ReadInt32(),
				isDefaults = reader.ReadBoolean(),
				EmergencyCooldown = reader.ReadByte()
			};
		}
		catch
		{
		}
		return null;
	}

	public byte[] ToBytes()
	{
		using (MemoryStream memoryStream = new MemoryStream())
		{
			using (BinaryWriter binaryWriter = new BinaryWriter(memoryStream))
			{
				Serialize(binaryWriter);
				binaryWriter.Flush();
				memoryStream.Position = 0L;
				return memoryStream.ToArray();
			}
		}
	}

	public static GameOptionsData FromBytes(byte[] bytes)
	{
		using (MemoryStream input = new MemoryStream(bytes))
		{
			using (BinaryReader reader = new BinaryReader(input))
			{
				return Deserialize(reader) ?? new GameOptionsData();
			}
		}
	}

	public override string ToString()
	{
		return ToHudString(10);
	}

	public string ToHudString(int numPlayers)
	{
		numPlayers = Mathf.Clamp(numPlayers, 0, 10);
		StringBuilder stringBuilder = new StringBuilder(256);
		try
		{
			stringBuilder.AppendLine(DestroyableSingleton<TranslationController>.Instance.GetString(isDefaults ? StringNames.GameRecommendedSettings : StringNames.GameCustomSettings));
			int num = MaxImpostors[numPlayers];
			stringBuilder.AppendLine(DestroyableSingleton<TranslationController>.Instance.GetString(StringNames.GameMapName) + ": " + MapNames[MapId]);
			stringBuilder.Append($"{DestroyableSingleton<TranslationController>.Instance.GetString(StringNames.GameNumImpostors)}: {NumImpostors}");
			if (NumImpostors > num)
			{
				stringBuilder.Append($" ({DestroyableSingleton<TranslationController>.Instance.GetString(StringNames.Limit)}: {num})");
			}
			stringBuilder.AppendLine();
			stringBuilder.AppendLine($"{DestroyableSingleton<TranslationController>.Instance.GetString(StringNames.GameNumMeetings)}: {NumEmergencyMeetings}");
			stringBuilder.AppendLine($"{DestroyableSingleton<TranslationController>.Instance.GetString(StringNames.GameEmergencyCooldown)}: {EmergencyCooldown}s");
			stringBuilder.AppendLine($"{DestroyableSingleton<TranslationController>.Instance.GetString(StringNames.GameDiscussTime)}: {DiscussionTime}s");
			if (VotingTime > 0)
			{
				stringBuilder.AppendLine($"{DestroyableSingleton<TranslationController>.Instance.GetString(StringNames.GameVotingTime)}: {VotingTime}s");
			}
			else
			{
				stringBuilder.AppendLine(DestroyableSingleton<TranslationController>.Instance.GetString(StringNames.GameVotingTime) + ": ∞s");
			}
			stringBuilder.AppendLine($"{DestroyableSingleton<TranslationController>.Instance.GetString(StringNames.GamePlayerSpeed)}: {PlayerSpeedMod}x");
			stringBuilder.AppendLine($"{DestroyableSingleton<TranslationController>.Instance.GetString(StringNames.GameCrewLight)}: {CrewLightMod}x");
			stringBuilder.AppendLine($"{DestroyableSingleton<TranslationController>.Instance.GetString(StringNames.GameImpostorLight)}: {ImpostorLightMod}x");
			stringBuilder.AppendLine($"{DestroyableSingleton<TranslationController>.Instance.GetString(StringNames.GameKillCooldown)}: {KillCooldown}s");
			stringBuilder.AppendLine(DestroyableSingleton<TranslationController>.Instance.GetString(StringNames.GameKillDistance) + ": " + KillDistanceStrings[KillDistance]);
			stringBuilder.AppendLine($"{DestroyableSingleton<TranslationController>.Instance.GetString(StringNames.GameCommonTasks)}: {NumCommonTasks}");
			stringBuilder.AppendLine($"{DestroyableSingleton<TranslationController>.Instance.GetString(StringNames.GameLongTasks)}: {NumLongTasks}");
			stringBuilder.Append($"{DestroyableSingleton<TranslationController>.Instance.GetString(StringNames.GameShortTasks)}: {NumShortTasks}");
		}
		catch
		{
		}
		return stringBuilder.ToString();
	}

	public int GetAdjustedNumImpostors(int playerCount)
	{
		int numImpostors = PlayerControl.GameOptions.NumImpostors;
		int max = MaxImpostors[GameData.Instance.PlayerCount];
		return Mathf.Clamp(numImpostors, 1, max);
	}
}
