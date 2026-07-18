using System;
using Hazel;

namespace InnerNet
{
	[Serializable]
	public struct GameListing
	{
		public int GameId;

		public byte PlayerCount;

		public string HostName;

		public int Age;

		public GameOptionsData Options;

		public GameListing(MessageReader reader)
		{
			GameId = reader.ReadInt32();
			HostName = reader.ReadString();
			PlayerCount = reader.ReadByte();
			Age = reader.ReadPackedInt32();
			Options = GameOptionsData.Deserialize(reader);
		}

		public GameListing(int id, byte numImpostors, byte playerCount, byte maxPlayers, byte mapId, int age, string host)
		{
			GameId = id;
			PlayerCount = playerCount;
			HostName = host;
			Age = age;
			Options = new GameOptionsData
			{
				NumImpostors = numImpostors,
				MaxPlayers = maxPlayers,
				MapId = mapId
			};
		}

		public GameListing(int id, byte numImpostors, byte playerCount, byte maxPlayers, int age, string host)
			: this(id, numImpostors, playerCount, maxPlayers, 0, age, host)
		{
		}
	}
}
