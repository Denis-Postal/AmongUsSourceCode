using System;
using System.Collections;
using System.IO;
using System.Linq;
using Photon.Pun;
using UnityEngine;

public class ServerManager : DestroyableSingleton<ServerManager>
{
	private enum UpdateState
	{
		Connecting = 0,
		Failed = 1,
		Success = 2
	}

	public const string DefaultOnlineServer = "PhotonCloud";

	public static readonly ServerInfo[] DefaultOnlineServers = new ServerInfo[1]
	{
		new ServerInfo("Photon Cloud", DefaultOnlineServer, 0)
	};

	public ServerInfo[] AvailableServers = DefaultOnlineServers;

	[Tooltip("Photon PUN 2 Realtime AppId. Can be empty if PhotonServerSettings.asset has AppId.")]
	public string PhotonAppId = "";

	[Tooltip("Example: eu, us, asia. Empty = Best Region.")]
	public string PhotonFixedRegion = "eu";

	private string serverInfoFile;

	private UpdateState state;

	public ServerInfo CurrentServer { get; private set; }

	public string OnlineNetAddress => CurrentServer.Ip;

	public override void Awake()
	{
		base.Awake();
		if (!(DestroyableSingleton<ServerManager>.Instance != this))
		{
			serverInfoFile = Path.Combine(Application.persistentDataPath, "serverInfo.dat");
			ApplyPhotonSettings();
			LoadServers();
			state = UpdateState.Success;
		}
	}

	private void ApplyPhotonSettings()
	{
		if (!string.IsNullOrWhiteSpace(PhotonAppId))
		{
			PhotonNetwork.PhotonServerSettings.AppSettings.AppIdRealtime = PhotonAppId.Trim();
		}
		if (!string.IsNullOrWhiteSpace(PhotonFixedRegion))
		{
			PhotonNetwork.PhotonServerSettings.AppSettings.FixedRegion = PhotonFixedRegion.Trim();
		}
		if (string.IsNullOrWhiteSpace(PhotonNetwork.PhotonServerSettings.AppSettings.AppIdRealtime))
		{
			Debug.LogWarning("Photon AppId is empty. Fill Assets/Photon/PhotonUnityNetworking/Resources/PhotonServerSettings.asset or ServerManager.PhotonAppId.");
		}
	}

	[ContextMenu("Reselect Server")]
	internal void ReselectServer()
	{
		if (AvailableServers.Length == 0)
		{
			AvailableServers = DefaultOnlineServers;
		}
		if (AvailableServers.All((ServerInfo s) => s.Players == 0))
		{
			AvailableServers.Shuffle();
		}
		CurrentServer = (from s in AvailableServers
			orderby s.ConnectionFailures, s.Players
			select s).First();
		Debug.Log("Selected server: " + CurrentServer.Name);
	}

	public IEnumerator WaitForServers()
	{
		while (state == UpdateState.Connecting)
		{
			yield return null;
		}
	}

	internal void SetServers(ServerInfo[] servers)
	{
		if (servers.Length == 0)
		{
			return;
		}
		for (int i = 0; i < AvailableServers.Length; i++)
		{
			ServerInfo existingServer = AvailableServers[i];
			ServerInfo serverInfo = servers.FirstOrDefault((ServerInfo s) => s.Ip.Equals(existingServer.Ip));
			if (serverInfo != null)
			{
				existingServer.Players = serverInfo.Players;
			}
		}
		AvailableServers = AvailableServers.Intersect(servers).Union(servers).ToArray();
		ReselectServer();
		SaveServers(servers, AvailableServers.IndexOf(CurrentServer));
	}

	private void SaveServers(ServerInfo[] servers, int last)
	{
		try
		{
			using (FileStream output = new FileStream(serverInfoFile, FileMode.Create, FileAccess.Write))
			{
				using (BinaryWriter binaryWriter = new BinaryWriter(output))
				{
					binaryWriter.Write(last);
					binaryWriter.Write(servers.Length);
					for (int i = 0; i < servers.Length; i++)
					{
						servers[i].Serialize(binaryWriter);
					}
				}
			}
		}
		catch
		{
		}
	}

	private void LoadServers()
	{
		if (File.Exists(serverInfoFile))
		{
			try
			{
				using (BinaryReader binaryReader = new BinaryReader(File.OpenRead(serverInfoFile)))
				{
					int num = binaryReader.ReadInt32();
					ServerInfo[] array = new ServerInfo[binaryReader.ReadInt32()];
					for (int i = 0; i < array.Length; i++)
					{
						array[i] = ServerInfo.Deserialize(binaryReader);
					}
					AvailableServers = DefaultOnlineServers;
					CurrentServer = DefaultOnlineServers[0];
					Debug.Log("Loaded server: " + CurrentServer.Name);
					return;
				}
			}
			catch (Exception arg)
			{
				Debug.Log($"Couldn't load servers: {arg}");
				ReselectServer();
				return;
			}
		}
		ReselectServer();
	}

	internal bool TrackServerFailure(string networkAddress)
	{
		ServerInfo srv = AvailableServers.FirstOrDefault((ServerInfo s) => s.Ip == networkAddress);
		if (srv != null)
		{
			srv.ConnectionFailures++;
			ServerInfo serverInfo = AvailableServers.OrderBy((ServerInfo s) => s.Players).FirstOrDefault((ServerInfo s) => s.ConnectionFailures < srv.ConnectionFailures);
			if (serverInfo != null)
			{
				CurrentServer = serverInfo;
				AmongUsClient.Instance.SetEndpoint(serverInfo.Ip, serverInfo.Port);
				return true;
			}
		}
		return false;
	}
}
