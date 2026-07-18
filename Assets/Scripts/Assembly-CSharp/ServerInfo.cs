using System;
using System.IO;
using System.Net;
using Hazel;

[Serializable]
public class ServerInfo
{
	public string Name = "Custom";

	public string Ip;

	public ushort Port;

	public int Players;

	public int ConnectionFailures;

	public ServerInfo()
	{
	}

	public ServerInfo(string name, string ip, ushort port)
	{
		Name = name;
		Ip = ip;
		Port = port;
	}

	public void Serialize(BinaryWriter writer)
	{
		writer.Write(Name);
		writer.Write((uint)IPAddress.Parse(Ip).Address);
		writer.Write(Port);
		writer.Write(ConnectionFailures);
	}

	public static ServerInfo Deserialize(BinaryReader reader)
	{
		return new ServerInfo
		{
			Name = reader.ReadString(),
			Ip = new IPAddress(reader.ReadUInt32()).ToString(),
			Port = reader.ReadUInt16(),
			ConnectionFailures = reader.ReadInt32()
		};
	}

	internal static ServerInfo Deserialize(MessageReader parts)
	{
		return new ServerInfo
		{
			Name = parts.ReadString(),
			Ip = new IPAddress(parts.ReadUInt32()).ToString(),
			Port = parts.ReadUInt16(),
			Players = parts.ReadPackedInt32()
		};
	}

	public override int GetHashCode()
	{
		return Ip.GetHashCode();
	}

	public override bool Equals(object obj)
	{
		if (obj is ServerInfo serverInfo)
		{
			return serverInfo.Ip.Equals(Ip);
		}
		return false;
	}
}
