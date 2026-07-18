using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using Assets.CoreScripts;
using ExitGames.Client.Photon;
using Hazel;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.SceneManagement;
using Hashtable = ExitGames.Client.Photon.Hashtable;

namespace InnerNet
{
	public abstract class InnerNetClient : MonoBehaviourPunCallbacks, IOnEventCallback
	{
		public enum GameStates
		{
			NotJoined = 0,
			Joined = 1,
			Started = 2,
			Ended = 3
		}

		private static readonly DisconnectReasons[] disconnectReasons = new DisconnectReasons[8]
		{
			DisconnectReasons.Error,
			DisconnectReasons.GameFull,
			DisconnectReasons.GameStarted,
			DisconnectReasons.GameNotFound,
			DisconnectReasons.IncorrectVersion,
			DisconnectReasons.Banned,
			DisconnectReasons.Kicked,
			DisconnectReasons.Custom
		};

		public const int NoClientId = -1;

		private string networkAddress = "127.0.0.1";

		private int networkPort;

		[Range(-1f, 5000f)]
		public int TestLagMs = -1;

		private const byte PhotonInnerNetEvent = 42;

		private const string RoomHostNameProperty = "host";

		private const string RoomImpostorsProperty = "imp";

		private const string RoomMaxPlayersProperty = "max";

		private const string RoomMapProperty = "map";

		private const string RoomKeywordsProperty = "kw";

		private const string RoomPublicProperty = "pub";

		private const string RoomStartedProperty = "started";

		private static readonly string[] PhotonLobbyRoomProperties = new string[] { RoomHostNameProperty, RoomImpostorsProperty, RoomMaxPlayersProperty, RoomMapProperty, RoomKeywordsProperty, RoomPublicProperty, RoomStartedProperty };

		private const string PlayerSceneProperty = "scene";

		private bool photonSessionActive;

		private bool suppressPhotonDisconnect;

		private readonly List<RoomInfo> photonRoomCache = new List<RoomInfo>();

		private bool lastGameListIncludePrivate = true;

		private GameOptionsData lastGameListSettings;

		public MatchMakerModes mode;

		public int GameId = 32;

		public int HostId;

		public int ClientId = -1;

		public List<ClientData> allClients = new List<ClientData>();

		public DisconnectReasons LastDisconnectReason;

		public string LastCustomDisconnect;

		private readonly List<Action> DispatchQueue = new List<Action>();

		public GameStates GameState;

		private volatile bool appPaused = false;

		public const int CurrentClient = -3;

		public const int InvalidClient = -2;

		internal const byte DataFlag = 1;

		internal const byte RpcFlag = 2;

		internal const byte SpawnFlag = 4;

		internal const byte DespawnFlag = 5;

		internal const byte SceneChangeFlag = 6;

		internal const byte ReadyFlag = 7;

		internal const byte ChangeSettingsFlag = 8;

		public float MinSendInterval = 0.1f;

		private uint NetIdCnt = 1u;

		private float timer;

		public InnerNetObject[] SpawnableObjects;

		public List<InnerNetObject> allObjects = new List<InnerNetObject>();

		private Dictionary<uint, InnerNetObject> allObjectsFast = new Dictionary<uint, InnerNetObject>();

		private MessageWriter[] Streams;

		public int Ping
		{
			get
			{
				return PhotonNetwork.IsConnected ? PhotonNetwork.GetPing() : 0;
			}
		}

		public int BytesSent
		{
			get
			{
				return 0;
			}
		}

		public int BytesGot
		{
			get
			{
				return 0;
			}
		}

		public bool AmHost
		{
			get
			{
				return HostId == ClientId;
			}
		}

		public bool AmClient
		{
			get
			{
				return ClientId > 0;
			}
		}

		public bool IsGamePublic { get; private set; }

		public bool IsGameStarted
		{
			get
			{
				return GameState == GameStates.Started;
			}
		}

		public bool IsGameOver
		{
			get
			{
				return GameState == GameStates.Ended;
			}
		}

		public void SetEndpoint(string addr, ushort port)
		{
			networkAddress = addr;
			networkPort = port;
		}

		public virtual void Start()
		{
			PhotonNetwork.AutomaticallySyncScene = false;
			SceneManager.activeSceneChanged += delegate(Scene oldScene, Scene scene)
			{
				SendSceneChange(scene.name);
			};
			ClientId = -1;
			GameId = 32;
		}

		public ClientData GetHost()
		{
			for (int i = 0; i < allClients.Count; i++)
			{
				if (allClients[i].Id == HostId)
				{
					return allClients[i];
				}
			}
			return null;
		}

		public int GetClientIdFromCharacter(InnerNetObject character)
		{
			for (int i = 0; i < allClients.Count; i++)
			{
				if (allClients[i].Character == character)
				{
					return allClients[i].Id;
				}
			}
			return -1;
		}

		public virtual void OnDestroy()
		{
			if (AmongUsClient.Instance != this)
			{
				return;
			}
			if (photonSessionActive || mode != MatchMakerModes.None)
			{
				DisconnectInternal(DisconnectReasons.Destroy);
			}
		}

		public IEnumerator CoConnect()
		{
			LastDisconnectReason = DisconnectReasons.ExitGame;
			bool useOfflinePhoton = AmongUsClient.Instance != null && AmongUsClient.Instance.GameMode != GameModes.OnlineGame;
			if (useOfflinePhoton)
			{
				if (!PhotonNetwork.OfflineMode)
				{
					if (PhotonNetwork.IsConnected)
					{
						suppressPhotonDisconnect = true;
						PhotonNetwork.Disconnect();
						yield return WaitWithTimeout(() => !PhotonNetwork.IsConnected);
						suppressPhotonDisconnect = false;
					}
					if (!PhotonNetwork.IsConnected)
					{
						PhotonNetwork.OfflineMode = true;
					}
				}
				yield break;
			}
			if (PhotonNetwork.OfflineMode)
			{
				PhotonNetwork.OfflineMode = false;
			}
			ConfigurePhotonStandaloneTransport();
			if (!PhotonNetwork.IsConnected)
			{
				PhotonNetwork.NickName = SaveManager.PlayerName;
				PhotonNetwork.ConnectUsingSettings();
				PhotonNetwork.GameVersion = Constants.GetBroadcastVersion().ToString();
				yield return WaitWithTimeout(IsReadyForMatchmaking);
			}
			else if (!IsReadyForMatchmaking())
			{
				yield return WaitWithTimeout(IsReadyForMatchmaking);
			}
		}

		private static void ConfigurePhotonStandaloneTransport()
		{
			string appVersion = Constants.GetBroadcastVersion().ToString();
			if (PhotonNetwork.PhotonServerSettings != null && PhotonNetwork.PhotonServerSettings.AppSettings != null)
			{
				PhotonNetwork.PhotonServerSettings.AppSettings.AppVersion = appVersion;
				PhotonNetwork.PhotonServerSettings.AppSettings.Protocol = ConnectionProtocol.Udp;
				PhotonNetwork.PhotonServerSettings.AppSettings.AuthMode = AuthModeOption.Auth;
				PhotonNetwork.PhotonServerSettings.AppSettings.EnableProtocolFallback = true;
				PhotonNetwork.PhotonServerSettings.AppSettings.FixedRegion = "eu";
				PhotonNetwork.PhotonServerSettings.AppSettings.UseNameServer = true;
				PhotonNetwork.PhotonServerSettings.AppSettings.Server = string.Empty;
				PhotonNetwork.PhotonServerSettings.AppSettings.Port = 0;
				PhotonNetwork.PhotonServerSettings.AppSettings.ProxyServer = string.Empty;
			}
			if (PhotonNetwork.NetworkingClient != null && PhotonNetwork.NetworkingClient.LoadBalancingPeer != null)
			{
				PhotonNetwork.GameVersion = appVersion;
				PhotonNetwork.NetworkingClient.AuthMode = AuthModeOption.Auth;
				PhotonNetwork.NetworkingClient.ExpectedProtocol = null;
				PhotonNetwork.NetworkingClient.EnableProtocolFallback = true;
				PhotonNetwork.NetworkingClient.LoadBalancingPeer.TransportProtocol = ConnectionProtocol.Udp;
			}
		}

		private static bool IsReadyForMatchmaking()
		{
			return PhotonNetwork.InRoom || PhotonNetwork.InLobby || PhotonNetwork.NetworkClientState == ClientState.ConnectedToMasterServer;
		}

		private void Connection_DataReceivedRaw(byte[] data)
		{
			Debug.Log("Client Got: " + string.Join(" ", data.Select((byte b) => b.ToString())));
		}

		private void Connection_DataSentRaw(byte[] data, int length)
		{
			Debug.Log("Client Sent: " + string.Join(" ", data.Select((byte b) => b.ToString()).ToArray(), 0, length));
		}

		public void Connect(MatchMakerModes mode)
		{
			Debug.Log("Photon connect requested: " + mode);
			if (this.mode != MatchMakerModes.None)
			{
				DisconnectInternal(DisconnectReasons.NewConnection);
			}
			this.mode = mode;
			StartCoroutine(CoConnectMode(mode));
		}

		private IEnumerator CoConnectMode(MatchMakerModes targetMode)
		{
			yield return CoConnect();
			if (mode == MatchMakerModes.None)
			{
				yield break;
			}
			if (!PhotonNetwork.IsConnectedAndReady && !PhotonNetwork.InRoom)
			{
				yield break;
			}
			switch (targetMode)
			{
			case MatchMakerModes.Client:
				this.mode = MatchMakerModes.Client;
				JoinGame();
				yield return WaitWithTimeout(() => ClientId >= 0);
				if (PhotonNetwork.InRoom)
				{
				}
				break;
			case MatchMakerModes.HostAndClient:
				this.mode = MatchMakerModes.HostAndClient;
				GameId = 0;
				PlayerControl.GameOptions = SaveManager.GameHostOptions;
				HostGame(SaveManager.GameHostOptions);
				yield return WaitWithTimeout(() => GameId != 0);
				if (PhotonNetwork.InRoom)
				{
					yield return WaitWithTimeout(() => ClientId >= 0);
					if (PhotonNetwork.InRoom)
					{
					}
				}
				break;
			}
		}

		public IEnumerator WaitForConnectionOrFail()
		{
			for (float timer = 0f; mode != MatchMakerModes.None; timer += Time.deltaTime)
			{
				switch (mode)
				{
				default:
					yield break;
				case MatchMakerModes.Client:
					if (ClientId >= 0)
					{
						yield break;
					}
					break;
				case MatchMakerModes.HostAndClient:
					if (GameId != 0 && ClientId >= 0)
					{
						yield break;
					}
					break;
				}
				if (timer > 30f)
				{
					LastCustomDisconnect = "Timed out while connecting to Photon.";
					DisconnectInternal(DisconnectReasons.Custom);
					yield break;
				}
				yield return null;
			}
		}

		private IEnumerator WaitWithTimeout(Func<bool> success)
		{
			bool failed = true;
			for (float timer = 0f; timer < 20f; timer += Time.deltaTime)
			{
				if (success())
				{
					failed = false;
					break;
				}
				yield return null;
			}
			if (failed)
			{
				DisconnectInternal(DisconnectReasons.Error);
			}
		}

		public void Update()
		{
			if (Input.GetKeyDown(KeyCode.O))
			{
			}
			if (Input.GetKeyDown(KeyCode.Return) && (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt)))
			{
				ResolutionManager.ToggleFullscreen();
			}
			if (DispatchQueue.Count <= 0)
			{
				return;
			}
			lock (DispatchQueue)
			{
				for (int i = 0; i < DispatchQueue.Count; i++)
				{
					try
					{
						DispatchQueue[i]();
					}
					catch (Exception exception)
					{
						Debug.LogException(exception);
						DispatchQueue.RemoveAt(i);
						i--;
					}
				}
				DispatchQueue.Clear();
			}
		}

		public void HandleDisconnect(DisconnectReasons reason, string stringReason = null)
		{
			if (reason == DisconnectReasons.Custom && !string.IsNullOrEmpty(stringReason))
			{
				LastCustomDisconnect = stringReason;
			}
			if (reason != DisconnectReasons.ExitGame)
			{
				StatsManager.Instance.LastGameStarted = DateTime.MinValue;
			}
			StopAllCoroutines();
			DestroyableSingleton<Telemetry>.Instance.WriteDisconnect(LastDisconnectReason);
			DisconnectInternal(reason, stringReason);
			OnDisconnected();
		}

		protected void EnqueueDisconnect(DisconnectReasons reason, string stringReason = null)
		{
			lock (DispatchQueue)
			{
				DispatchQueue.Clear();
				DispatchQueue.Add(delegate
				{
					HandleDisconnect(reason, stringReason);
				});
			}
		}

		protected void DisconnectInternal(DisconnectReasons reason, string stringReason = null)
		{
			Debug.Log(string.Format("Client DC because {0}", reason));
			lock (DispatchQueue)
			{
				DispatchQueue.Clear();
			}
			NetIdCnt = 0u;
			allObjects.Clear();
			allClients.Clear();
			allObjectsFast.Clear();
			LastDisconnectReason = reason;
			if (mode == MatchMakerModes.HostAndClient)
			{
				GameId = 0;
			}
			if (mode == MatchMakerModes.Client || mode == MatchMakerModes.HostAndClient)
			{
				ClientId = -1;
			}
			mode = MatchMakerModes.None;
			GameState = GameStates.NotJoined;
			photonSessionActive = false;
			if (PhotonNetwork.InRoom)
			{
				PhotonNetwork.LeaveRoom();
			}
			if ((bool)DestroyableSingleton<InnerNetServer>.Instance)
			{
				DestroyableSingleton<InnerNetServer>.Instance.StopServer();
			}
		}

		public void HostGame(IBytesSerializable settings)
		{
			if (!IsReadyForMatchmaking())
			{
				Debug.Log("Photon HostGame waits for Master Server. Current state=" + PhotonNetwork.NetworkClientState);
				StartCoroutine(CoHostGameWhenReady(settings));
				return;
			}
			IsGamePublic = false;
			GameOptionsData gameOptionsData = settings as GameOptionsData;
			string roomName = CreatePhotonRoomName();
			GameId = GameNameToInt(roomName);
			RoomOptions roomOptions = new RoomOptions();
			roomOptions.MaxPlayers = (byte)((gameOptionsData != null) ? gameOptionsData.MaxPlayers : 10);
			roomOptions.IsVisible = false;
			roomOptions.IsOpen = true;
			roomOptions.CleanupCacheOnLeave = false;
			roomOptions.CustomRoomProperties = new Hashtable
			{
				{ RoomHostNameProperty, SaveManager.PlayerName },
				{ RoomImpostorsProperty, (byte)((gameOptionsData != null) ? gameOptionsData.NumImpostors : 1) },
				{ RoomMaxPlayersProperty, (byte)roomOptions.MaxPlayers },
				{ RoomMapProperty, (byte)((gameOptionsData != null) ? gameOptionsData.MapId : 0) },
				{ RoomKeywordsProperty, (int)((gameOptionsData != null) ? gameOptionsData.Keywords : GameKeywords.English) },
				{ RoomPublicProperty, false },
				{ RoomStartedProperty, false }
			};
			roomOptions.CustomRoomPropertiesForLobby = PhotonLobbyRoomProperties;
			try
			{
				PhotonNetwork.CreateRoom(roomName, roomOptions, TypedLobby.Default);
				Debug.Log("Photon CreateRoom requested: " + roomName + " state=" + PhotonNetwork.NetworkClientState);
			}
			catch (Exception ex)
			{
				LastCustomDisconnect = "Photon CreateRoom exception: " + ex.Message;
				Debug.LogException(ex);
				EnqueueDisconnect(DisconnectReasons.Error, LastCustomDisconnect);
			}
		}

		private IEnumerator CoHostGameWhenReady(IBytesSerializable settings)
		{
			yield return WaitWithTimeout(IsReadyForMatchmaking);
			if (IsReadyForMatchmaking() && mode == MatchMakerModes.HostAndClient && GameId == 0)
			{
				HostGame(settings);
			}
		}

		public bool CanBan()
		{
			return AmHost && !IsGameStarted;
		}

		public bool CanKick()
		{
			return !IsGameStarted ? AmHost : true;
		}

		public void JoinGame()
		{
			ClientId = -1;
			if (!PhotonNetwork.IsConnectedAndReady && !PhotonNetwork.InRoom)
			{
				DisconnectInternal(DisconnectReasons.Error);
				return;
			}
			Debug.Log("Client joining game: " + IntToGameName(GameId));
			string roomName = IntToGameName(GameId);
			if (string.IsNullOrEmpty(roomName))
			{
				EnqueueDisconnect(DisconnectReasons.GameNotFound);
				return;
			}
			if (!PhotonNetwork.InRoom || PhotonNetwork.CurrentRoom.Name != roomName)
			{
				PhotonNetwork.JoinRoom(roomName);
			}
			if (Streams == null)
			{
				Streams = new MessageWriter[2];
				for (int i = 0; i < Streams.Length; i++)
				{
					Streams[i] = MessageWriter.Get((SendOption)i);
				}
			}
			for (int j = 0; j < Streams.Length; j++)
			{
				MessageWriter messageWriter2 = Streams[j];
				messageWriter2.Clear((SendOption)j);
				messageWriter2.StartMessage(5);
				messageWriter2.Write(GameId);
			}
		}

		public void KickPlayer(int clientId, bool ban)
		{
			if (AmHost)
			{
				MessageWriter messageWriter = MessageWriter.Get(SendOption.Reliable);
				messageWriter.StartMessage(11);
				messageWriter.Write(GameId);
				messageWriter.WritePacked(clientId);
				messageWriter.Write(ban);
				messageWriter.EndMessage();
				SendPhotonMessage(messageWriter, SendOption.Reliable, -1, true);
				messageWriter.Recycle();
			}
		}

		protected void SendLateRejection(int targetId, DisconnectReasons reason)
		{
			if (targetId == ClientId)
			{
				HandleDisconnect(reason);
				return;
			}
			MessageWriter messageWriter = MessageWriter.Get(SendOption.Reliable);
			messageWriter.StartMessage(1);
			messageWriter.Write((int)reason);
			messageWriter.EndMessage();
			SendPhotonMessage(messageWriter, SendOption.Reliable, targetId);
			messageWriter.Recycle();
		}

		public MessageWriter StartEndGame()
		{
			MessageWriter messageWriter = MessageWriter.Get(SendOption.Reliable);
			messageWriter.StartMessage(8);
			messageWriter.Write(GameId);
			return messageWriter;
		}

		public void FinishEndGame(MessageWriter msg)
		{
			msg.EndMessage();
			SendPhotonMessage(msg, SendOption.Reliable, -1, true);
			msg.Recycle();
		}

		protected void SendClientReady()
		{
			if (AmHost)
			{
				ClientData clientData = FindClientById(ClientId);
				clientData.IsReady = true;
				return;
			}
			MessageWriter messageWriter = MessageWriter.Get(SendOption.Reliable);
			messageWriter.StartMessage(5);
			messageWriter.Write(GameId);
			messageWriter.StartMessage(7);
			messageWriter.WritePacked(ClientId);
			messageWriter.EndMessage();
			messageWriter.EndMessage();
			SendPhotonMessage(messageWriter, SendOption.Reliable, HostId);
			messageWriter.Recycle();
		}

		protected void SendStartGame()
		{
			GameState = GameStates.Started;
			if (PhotonNetwork.InRoom && PhotonNetwork.IsMasterClient)
			{
				PhotonNetwork.CurrentRoom.IsOpen = false;
				PhotonNetwork.CurrentRoom.IsVisible = false;
				PhotonNetwork.CurrentRoom.SetCustomProperties(new Hashtable { { RoomStartedProperty, true } });
			}
			MessageWriter messageWriter = MessageWriter.Get(SendOption.Reliable);
			messageWriter.StartMessage(2);
			messageWriter.Write(GameId);
			messageWriter.EndMessage();
			SendPhotonMessage(messageWriter, SendOption.Reliable, -1, true);
			messageWriter.Recycle();
		}

		public void RequestGameList(bool includePrivate, IBytesSerializable settings)
		{
			lastGameListIncludePrivate = includePrivate;
			lastGameListSettings = settings as GameOptionsData;
			if (PhotonNetwork.IsConnectedAndReady && !PhotonNetwork.InLobby && !PhotonNetwork.InRoom)
			{
				PhotonNetwork.JoinLobby();
			}
			DispatchPhotonRoomList(includePrivate);
		}

		public void ChangeGamePublic(bool isPublic)
		{
			if (AmHost)
			{
				MessageWriter messageWriter = MessageWriter.Get(SendOption.Reliable);
				messageWriter.StartMessage(10);
				messageWriter.Write(GameId);
				messageWriter.Write((byte)1);
				messageWriter.Write(isPublic);
				messageWriter.EndMessage();
				SendPhotonMessage(messageWriter, SendOption.Reliable, -1, true);
				messageWriter.Recycle();
				IsGamePublic = isPublic;
				if (PhotonNetwork.InRoom)
				{
					PhotonNetwork.CurrentRoom.IsVisible = isPublic;
					PhotonNetwork.CurrentRoom.SetCustomProperties(new Hashtable { { RoomPublicProperty, isPublic } });
					PhotonNetwork.CurrentRoom.SetPropertiesListedInLobby(PhotonLobbyRoomProperties);
					Debug.Log("Photon room public changed: " + PhotonNetwork.CurrentRoom.Name + " public=" + isPublic + " visible=" + PhotonNetwork.CurrentRoom.IsVisible);
				}
			}
		}

		public void OnEvent(EventData photonEvent)
		{
			if (photonEvent.Code != PhotonInnerNetEvent || !(photonEvent.CustomData is byte[]))
			{
				return;
			}
			MessageReader message = MessageReader.Get((byte[])photonEvent.CustomData);
			try
			{
				while (message.Position < message.Length)
				{
					HandleMessage(message.ReadMessage());
				}
			}
			catch (Exception exception)
			{
				Debug.LogException(exception);
			}
			finally
			{
				message.Recycle();
			}
		}

		private void HandleMessage(MessageReader reader)
		{
			switch (reader.Tag)
			{
			case 0:
				GameId = reader.ReadInt32();
				Debug.Log("Client hosting game: " + IntToGameName(GameId));
				lock (DispatchQueue)
				{
					DispatchQueue.Add(delegate
					{
						OnGameCreated(IntToGameName(GameId));
					});
					break;
				}
			case 4:
			{
				int num5 = reader.ReadInt32();
				if (GameId != num5)
				{
					break;
				}
				int playerIdThatLeft = reader.ReadInt32();
				bool amHost = AmHost;
				HostId = reader.ReadInt32();
				RemovePlayer(playerIdThatLeft, DisconnectReasons.ExitGame);
				if (!AmHost || amHost)
				{
					break;
				}
				lock (DispatchQueue)
				{
					DispatchQueue.Add(delegate
					{
						OnBecomeHost();
					});
					break;
				}
			}
			case 8:
			{
				int num13 = reader.ReadInt32();
				if (GameId != num13 || GameState == GameStates.Ended)
				{
					break;
				}
				GameState = GameStates.Ended;
				lock (allClients)
				{
					allClients.Clear();
				}
				GameOverReason reason = (GameOverReason)reader.ReadByte();
				bool showAd = reader.ReadBoolean();
				lock (DispatchQueue)
				{
					DispatchQueue.Add(delegate
					{
						OnGameEnd(reason, showAd);
					});
					break;
				}
			}
			case 12:
			{
				int num10 = reader.ReadInt32();
				if (GameId != num10)
				{
					break;
				}
				ClientId = reader.ReadInt32();
				lock (DispatchQueue)
				{
					DispatchQueue.Add(delegate
					{
						OnWaitForHost(IntToGameName(GameId));
					});
					break;
				}
			}
			case 7:
			{
				int num7 = reader.ReadInt32();
				if (GameId != num7)
				{
					break;
				}
				ClientId = reader.ReadInt32();
				GameState = GameStates.Joined;
				ClientData myClient = GetOrCreateClient(ClientId);
				bool amHost2 = AmHost;
				HostId = reader.ReadInt32();
				int num8 = reader.ReadPackedInt32();
				for (int num9 = 0; num9 < num8; num9++)
				{
					GetOrCreateClient(reader.ReadPackedInt32());
				}
				lock (DispatchQueue)
				{
					DispatchQueue.Add(delegate
					{
						OnGameJoined(IntToGameName(GameId), myClient);
					});
					break;
				}
			}
			case 1:
			{
				DisconnectReasons dcReason;
				int num11 = (int)(dcReason = (DisconnectReasons)reader.ReadInt32());
				if (disconnectReasons.Contains(dcReason))
				{
					if (dcReason == DisconnectReasons.Custom)
					{
						LastCustomDisconnect = reader.ReadString();
					}
					GameId = -1;
					lock (DispatchQueue)
					{
						DispatchQueue.Add(delegate
						{
							HandleDisconnect(dcReason);
						});
						break;
					}
				}
				if (GameId == num11)
				{
					int num12 = reader.ReadInt32();
					bool amHost3 = AmHost;
					HostId = reader.ReadInt32();
					ClientData client = GetOrCreateClient(num12);
					Debug.Log(string.Format("Player {0} joined", num12));
					lock (DispatchQueue)
					{
						DispatchQueue.Add(delegate
						{
							OnPlayerJoined(client);
						});
					}
					if (!AmHost || amHost3)
					{
						break;
					}
					lock (DispatchQueue)
					{
						DispatchQueue.Add(delegate
						{
							OnBecomeHost();
						});
						break;
					}
				}
				lock (DispatchQueue)
				{
					DispatchQueue.Add(delegate
					{
						HandleDisconnect(DisconnectReasons.IncorrectGame);
					});
					break;
				}
			}
			case 5:
			case 6:
			{
				int num3 = reader.ReadInt32();
				if (GameId != num3)
				{
					break;
				}
				if (reader.Tag == 6)
				{
					int num4 = reader.ReadPackedInt32();
					if (ClientId != num4)
					{
						Debug.LogWarning(string.Format("Got data meant for {0}", num4));
						break;
					}
				}
				MessageReader subReader = MessageReader.Get(reader);
				lock (DispatchQueue)
				{
					DispatchQueue.Add(delegate
					{
						HandleGameData(subReader);
					});
					break;
				}
			}
			case 9:
			{
				int totalGames = reader.ReadPackedInt32();
				List<GameListing> output = new List<GameListing>();
				while (reader.Position < reader.Length)
				{
					output.Add(new GameListing(reader.ReadInt32(), reader.ReadByte(), reader.ReadByte(), reader.ReadByte(), reader.ReadPackedInt32(), reader.ReadString()));
				}
				lock (DispatchQueue)
				{
					DispatchQueue.Add(delegate
					{
						OnGetGameList(totalGames, output);
					});
					break;
				}
			}
			case 10:
			{
				int num6 = reader.ReadInt32();
				if (GameId == num6)
				{
					byte b = reader.ReadByte();
					byte b2 = b;
					if (b2 == 1)
					{
						IsGamePublic = reader.ReadBoolean();
						Debug.Log("Alter Public = " + IsGamePublic);
					}
					else
					{
						Debug.Log("Alter unknown");
					}
				}
				break;
			}
			case 3:
				lock (DispatchQueue)
				{
					DispatchQueue.Add(delegate
					{
						HandleDisconnect(DisconnectReasons.ServerRequest);
					});
					break;
				}
			case 2:
				GameState = GameStates.Started;
				lock (DispatchQueue)
				{
					DispatchQueue.Add(delegate
					{
						OnStartGame();
					});
					break;
				}
			case 11:
			{
				int num = reader.ReadInt32();
				if (GameId != num)
				{
					break;
				}
				int num2 = reader.ReadPackedInt32();
				if (num2 != ClientId)
				{
					break;
				}
				bool ban = reader.ReadBoolean();
				lock (DispatchQueue)
				{
					DispatchQueue.Add(delegate
					{
						HandleDisconnect(ban ? DisconnectReasons.Banned : DisconnectReasons.Kicked);
					});
					break;
				}
			}
			case 13:
			{
				uint address = reader.ReadUInt32();
				ushort port = reader.ReadUInt16();
				AmongUsClient.Instance.SetEndpoint(AddressToString(address), port);
				lock (DispatchQueue)
				{
					DispatchQueue.Add(delegate
					{
						Debug.Log(string.Format("Redirected to: {0}:{1}", networkAddress, networkPort));
						StopAllCoroutines();
						Connect(mode);
					});
					break;
				}
			}
			default:
				Debug.Log(string.Format("Bad tag {0} at {1}+{2}={3}:  ", reader.Tag, reader.Offset, reader.Position, reader.Length) + string.Join(" ", reader.Buffer));
				break;
			}
		}

		public override void OnCreatedRoom()
		{
			Debug.Log("Photon OnCreatedRoom: " + PhotonNetwork.CurrentRoom.Name);
			GameId = GameNameToInt(PhotonNetwork.CurrentRoom.Name);
			lock (DispatchQueue)
			{
				DispatchQueue.Add(delegate
				{
					OnGameCreated(PhotonNetwork.CurrentRoom.Name);
				});
			}
		}

		public override void OnCreateRoomFailed(short returnCode, string message)
		{
			Debug.LogWarning("Photon create room failed: " + returnCode + " " + message);
			EnqueueDisconnect(DisconnectReasons.Error);
		}

		public override void OnJoinRoomFailed(short returnCode, string message)
		{
			Debug.LogWarning("Photon join room failed: " + message);
			EnqueueDisconnect(DisconnectReasons.GameNotFound);
		}

		public override void OnConnectedToMaster()
		{
			Debug.Log("Photon OnConnectedToMaster: " + PhotonNetwork.NetworkClientState);
			if (mode == MatchMakerModes.Client && lastGameListSettings != null && !PhotonNetwork.InLobby && !PhotonNetwork.InRoom)
			{
				PhotonNetwork.JoinLobby();
			}
		}

		public override void OnJoinedLobby()
		{
			Debug.Log("Photon OnJoinedLobby");
			if (mode == MatchMakerModes.Client)
			{
				DispatchPhotonRoomList(lastGameListIncludePrivate);
			}
		}

		public override void OnJoinedRoom()
		{
			Debug.Log("Photon OnJoinedRoom: " + PhotonNetwork.CurrentRoom.Name + " actors=" + PhotonNetwork.PlayerList.Length);
			photonSessionActive = true;
			ClientId = PhotonNetwork.LocalPlayer.ActorNumber;
			HostId = PhotonNetwork.MasterClient.ActorNumber;
			GameId = GameNameToInt(PhotonNetwork.CurrentRoom.Name);
			GameState = GetRoomStarted(PhotonNetwork.CurrentRoom) ? GameStates.Started : GameStates.Joined;
			IsGamePublic = GetRoomPublic(PhotonNetwork.CurrentRoom);
			PhotonNetwork.LocalPlayer.SetCustomProperties(new Hashtable { { PlayerSceneProperty, SceneManager.GetActiveScene().name } });
			lock (allClients)
			{
				allClients.Clear();
				for (int i = 0; i < PhotonNetwork.PlayerList.Length; i++)
				{
					allClients.Add(new ClientData(PhotonNetwork.PlayerList[i].ActorNumber));
				}
			}
			if (Streams == null)
			{
				Streams = new MessageWriter[2];
				for (int i = 0; i < Streams.Length; i++)
				{
					Streams[i] = MessageWriter.Get((SendOption)i);
				}
			}
			for (int j = 0; j < Streams.Length; j++)
			{
				MessageWriter messageWriter = Streams[j];
				messageWriter.Clear((SendOption)j);
				messageWriter.StartMessage(5);
				messageWriter.Write(GameId);
			}
			ClientData myClient = FindClientById(ClientId);
			lock (DispatchQueue)
			{
				DispatchQueue.Add(delegate
				{
					OnGameJoined(IntToGameName(GameId), myClient);
				});
			}
		}

		public override void OnPlayerEnteredRoom(Photon.Realtime.Player newPlayer)
		{
			ClientData client = GetOrCreateClient(newPlayer.ActorNumber);
			object sceneName;
			string scene = null;
			bool shouldNotifyScene = false;
			if (newPlayer.CustomProperties.TryGetValue(PlayerSceneProperty, out sceneName))
			{
				scene = sceneName as string;
				shouldNotifyScene = IsGameScene(scene) && !client.InScene;
				if (shouldNotifyScene)
				{
					client.InScene = true;
				}
			}
			lock (DispatchQueue)
			{
				DispatchQueue.Add(delegate
				{
					OnPlayerJoined(client);
					if (shouldNotifyScene)
					{
						OnPlayerChangedScene(client, scene);
					}
				});
			}
		}

		public override void OnPlayerPropertiesUpdate(Photon.Realtime.Player targetPlayer, Hashtable changedProps)
		{
			if (targetPlayer.ActorNumber == ClientId)
			{
				return;
			}
			object sceneName;
			if (!AmHost || !changedProps.TryGetValue(PlayerSceneProperty, out sceneName))
			{
				return;
			}
			string scene = sceneName as string;
			if (!IsGameScene(scene))
			{
				return;
			}
			ClientData client = GetOrCreateClient(targetPlayer.ActorNumber);
			if (client.InScene)
			{
				return;
			}
			client.InScene = true;
			lock (DispatchQueue)
			{
				DispatchQueue.Add(delegate
				{
					OnPlayerChangedScene(client, scene);
				});
			}
		}

		public override void OnPlayerLeftRoom(Photon.Realtime.Player otherPlayer)
		{
			bool wasHost = AmHost;
			HostId = PhotonNetwork.MasterClient != null ? PhotonNetwork.MasterClient.ActorNumber : -1;
			RemovePlayer(otherPlayer.ActorNumber, DisconnectReasons.ExitGame);
			if (AmHost && !wasHost)
			{
				lock (DispatchQueue)
				{
					DispatchQueue.Add(delegate
					{
						OnBecomeHost();
					});
				}
			}
		}

		public override void OnMasterClientSwitched(Photon.Realtime.Player newMasterClient)
		{
			bool wasHost = AmHost;
			HostId = newMasterClient.ActorNumber;
			if (AmHost && !wasHost)
			{
				lock (DispatchQueue)
				{
					DispatchQueue.Add(delegate
					{
						OnBecomeHost();
					});
				}
			}
		}

		public override void OnLeftRoom()
		{
			if (photonSessionActive)
			{
				photonSessionActive = false;
				lock (DispatchQueue)
				{
					DispatchQueue.Add(delegate
					{
						OnDisconnected();
					});
				}
			}
		}

		public override void OnDisconnected(DisconnectCause cause)
		{
			if (suppressPhotonDisconnect)
			{
				return;
			}
			if (mode != MatchMakerModes.None)
			{
				Debug.LogWarning("Photon disconnected: " + cause);
				LastCustomDisconnect = GetPhotonDisconnectDetails(cause);
				EnqueueDisconnect(DisconnectReasons.Error);
			}
		}

		private static string GetPhotonDisconnectDetails(DisconnectCause cause)
		{
			if (PhotonNetwork.NetworkingClient == null)
			{
				return "Photon: " + cause;
			}

			string text = "Photon: " + cause;
			if (PhotonNetwork.NetworkingClient.LoadBalancingPeer != null)
			{
				text += " (" + PhotonNetwork.NetworkingClient.LoadBalancingPeer.UsedProtocol + ")";
			}

			SystemConnectionSummary systemConnectionSummary = PhotonNetwork.NetworkingClient.SystemConnectionSummary;
			if (systemConnectionSummary != null)
			{
				text += "\n" + GetPhotonProtocolName(systemConnectionSummary.UsedProtocol) + " socket err: " + systemConnectionSummary.SocketErrorCode;
			}

			bool usingWebSocket = PhotonNetwork.NetworkingClient.LoadBalancingPeer != null && (PhotonNetwork.NetworkingClient.LoadBalancingPeer.UsedProtocol == ConnectionProtocol.WebSocket || PhotonNetwork.NetworkingClient.LoadBalancingPeer.UsedProtocol == ConnectionProtocol.WebSocketSecure);
			if (usingWebSocket && !string.IsNullOrEmpty(PhotonWebSocketDebug.LastError))
			{
				text += "\nWS: " + TrimWebSocketError(PhotonWebSocketDebug.LastError);
			}
			else if (!string.IsNullOrEmpty(PhotonNetwork.NetworkingClient.DisconnectMessage))
			{
				text += "\nMsg: " + TrimDisconnectLine(PhotonNetwork.NetworkingClient.DisconnectMessage, 48);
			}
			return text;
		}

		private static string GetPhotonProtocolName(byte protocol)
		{
			switch (protocol)
			{
			case 0:
				return "UDP";
			case 1:
				return "TCP";
			case 4:
				return "WS";
			case 5:
				return "WSS";
			default:
				return protocol.ToString();
			}
		}

		private static string TrimDisconnectLine(string text, int maxLength)
		{
			if (string.IsNullOrEmpty(text))
			{
				return string.Empty;
			}
			text = text.Replace("\r", " ").Replace("\n", " ");
			if (text.Length <= maxLength)
			{
				return text;
			}
			return text.Substring(0, maxLength - 3) + "...";
		}

		private static string TrimWebSocketError(string text)
		{
			if (string.IsNullOrEmpty(text))
			{
				return string.Empty;
			}
			text = text.Replace("Connecting WebSocketSharp: ", string.Empty);
			text = text.Replace("WebSocketSharp: ", string.Empty);
			text = text.Replace("\r", " ").Replace("\n", " ");
			if (text.Contains("Value does not fall within the expected range"))
			{
				return "Value out of range";
			}
			return TrimDisconnectLine(text, 48);
		}

		public override void OnRoomListUpdate(List<RoomInfo> roomList)
		{
			Debug.Log("Photon OnRoomListUpdate: " + roomList.Count);
			for (int i = 0; i < roomList.Count; i++)
			{
				RoomInfo roomInfo = roomList[i];
				Debug.Log("Photon room: " + roomInfo.Name + " removed=" + roomInfo.RemovedFromList + " open=" + roomInfo.IsOpen + " visible=" + roomInfo.IsVisible + " pub=" + GetRoomPublic(roomInfo) + " map=" + GetRoomByte(roomInfo, RoomMapProperty, 0) + " imp=" + GetRoomByte(roomInfo, RoomImpostorsProperty, 1) + " kw=" + GetRoomInt(roomInfo, RoomKeywordsProperty, (int)GameKeywords.AllLanguages) + " players=" + roomInfo.PlayerCount + "/" + roomInfo.MaxPlayers);
				int index = photonRoomCache.FindIndex((RoomInfo cached) => cached.Name == roomInfo.Name);
				if (roomInfo.RemovedFromList)
				{
					if (index >= 0)
					{
						photonRoomCache.RemoveAt(index);
					}
				}
				else if (index >= 0)
				{
					photonRoomCache[index] = roomInfo;
				}
				else
				{
					photonRoomCache.Add(roomInfo);
				}
			}
			DispatchPhotonRoomList(lastGameListIncludePrivate);
		}

		private void DispatchPhotonRoomList(bool includePrivate)
		{
			List<GameListing> output = new List<GameListing>();
			for (int i = 0; i < photonRoomCache.Count; i++)
			{
				RoomInfo room = photonRoomCache[i];
				if (!room.IsOpen || GetRoomStarted(room) || (!includePrivate && !GetRoomPublic(room)))
				{
					continue;
				}
				int gameId = GameNameToInt(room.Name);
				if (gameId == -1)
				{
					continue;
				}
				byte impostors = GetRoomByte(room, RoomImpostorsProperty, 1);
				byte maxPlayers = GetRoomByte(room, RoomMaxPlayersProperty, (byte)room.MaxPlayers);
				byte mapId = GetRoomByte(room, RoomMapProperty, 0);
				string filterReason;
				if (!RoomMatchesSearch(room, mapId, impostors, out filterReason))
				{
					Debug.Log("Photon room filtered: " + room.Name + " reason=" + filterReason);
					continue;
				}
				output.Add(new GameListing(gameId, impostors, (byte)room.PlayerCount, maxPlayers, mapId, 0, GetRoomString(room, RoomHostNameProperty, room.Name)));
			}
			OnGetGameList(photonRoomCache.Count, output);
		}

		private bool RoomMatchesSearch(RoomInfo room, byte mapId, byte impostors, out string reason)
		{
			GameOptionsData search = lastGameListSettings ?? SaveManager.GameSearchOptions;
			reason = string.Empty;
			if (search == null)
			{
				return true;
			}
			if (search.MapId != 0 && !search.FilterContainsMap(mapId))
			{
				reason = "map search=" + search.MapId + " room=" + mapId;
				return false;
			}
			if (search.NumImpostors > 0 && impostors != search.NumImpostors)
			{
				reason = "impostors search=" + search.NumImpostors + " room=" + impostors;
				return false;
			}
			return true;
		}

		private void SendPhotonMessage(MessageWriter msg, SendOption option, int targetClientId = -1, bool includeSelf = false)
		{
			if (!PhotonNetwork.InRoom)
			{
				EnqueueDisconnect(DisconnectReasons.Error);
				return;
			}
			RaiseEventOptions raiseOptions = new RaiseEventOptions();
			if (targetClientId > 0)
			{
				raiseOptions.TargetActors = new int[] { targetClientId };
			}
			else
			{
				raiseOptions.Receivers = includeSelf ? ReceiverGroup.All : ReceiverGroup.Others;
			}
			PhotonNetwork.RaiseEvent(PhotonInnerNetEvent, MessageToByteArray(msg), raiseOptions, new ExitGames.Client.Photon.SendOptions { Reliability = option == SendOption.Reliable });
		}

		private static byte[] MessageToByteArray(MessageWriter msg)
		{
			return msg.ToByteArray(false);
		}

		private static string CreatePhotonRoomName()
		{
			const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
			char[] name = new char[4];
			for (int i = 0; i < name.Length; i++)
			{
				name[i] = chars[UnityEngine.Random.Range(0, chars.Length)];
			}
			return new string(name);
		}

		private static bool GetRoomStarted(RoomInfo room)
		{
			object value;
			return room.CustomProperties.TryGetValue(RoomStartedProperty, out value) && value is bool && (bool)value;
		}

		private static bool GetRoomPublic(RoomInfo room)
		{
			object value;
			return room.CustomProperties.TryGetValue(RoomPublicProperty, out value) && value is bool && (bool)value;
		}

		private static byte GetRoomByte(RoomInfo room, string key, byte fallback)
		{
			object value;
			if (!room.CustomProperties.TryGetValue(key, out value))
			{
				return fallback;
			}
			if (value is byte)
			{
				return (byte)value;
			}
			if (value is int)
			{
				return (byte)(int)value;
			}
			return fallback;
		}

		private static int GetRoomInt(RoomInfo room, string key, int fallback)
		{
			object value;
			if (!room.CustomProperties.TryGetValue(key, out value))
			{
				return fallback;
			}
			if (value is int)
			{
				return (int)value;
			}
			if (value is uint)
			{
				return unchecked((int)(uint)value);
			}
			if (value is short)
			{
				return (short)value;
			}
			if (value is ushort)
			{
				return (ushort)value;
			}
			if (value is byte)
			{
				return (byte)value;
			}
			return fallback;
		}

		private static string GetRoomString(RoomInfo room, string key, string fallback)
		{
			object value;
			return room.CustomProperties.TryGetValue(key, out value) && value is string ? (string)value : fallback;
		}

		private static bool IsGameScene(string sceneName)
		{
			return sceneName == "OnlineGame" || sceneName == "Tutorial";
		}

		private static string AddressToString(uint address)
		{
			return string.Format("{0}.{1}.{2}.{3}", (byte)address, (byte)(address >> 8), (byte)(address >> 16), (byte)(address >> 24));
		}

		private ClientData GetOrCreateClient(int clientId)
		{
			ClientData clientData;
			lock (allClients)
			{
				clientData = allClients.FirstOrDefault((ClientData c) => c.Id == clientId);
				if (clientData == null)
				{
					clientData = new ClientData(clientId);
					allClients.Add(clientData);
				}
			}
			return clientData;
		}

		private void RemovePlayer(int playerIdThatLeft, DisconnectReasons reason = DisconnectReasons.ExitGame)
		{
			ClientData client = null;
			lock (allClients)
			{
				int num = allClients.FindIndex((ClientData c) => c.Id == playerIdThatLeft);
				if (num != -1)
				{
					client = allClients[num];
					allClients.RemoveAt(num);
				}
			}
			if (client == null)
			{
				return;
			}
			lock (DispatchQueue)
			{
				DispatchQueue.Add(delegate
				{
					OnPlayerLeft(client, reason);
				});
			}
		}

		protected virtual void OnApplicationPause(bool pause)
		{
			appPaused = pause;
			if (!pause)
			{
				Debug.Log("Resumed Game");
				if (AmHost)
				{
					RemoveUnownedObjects();
				}
			}
			else if (GameState != GameStates.Ended)
			{
				Debug.Log("Lost focus during game");
				ThreadPool.QueueUserWorkItem(WaitToDisconnect);
			}
		}

		private void WaitToDisconnect(object state)
		{
			DateTime now = DateTime.Now;
			while (appPaused && (DateTime.Now - now).TotalSeconds < 10.0)
			{
				Thread.Sleep(1000);
			}
			if (appPaused && GameState != GameStates.Ended)
			{
				DisconnectInternal(DisconnectReasons.FocusLost);
				EnqueueDisconnect(DisconnectReasons.FocusLost);
			}
		}

		protected void SendInitialData(int clientId)
		{
			MessageWriter messageWriter = MessageWriter.Get(SendOption.Reliable);
			messageWriter.StartMessage(6);
			messageWriter.Write(GameId);
			messageWriter.WritePacked(clientId);
			lock (allObjects)
			{
				HashSet<GameObject> hashSet = new HashSet<GameObject>();
				for (int i = 0; i < allObjects.Count; i++)
				{
					InnerNetObject innerNetObject = allObjects[i];
					if ((bool)innerNetObject && hashSet.Add(innerNetObject.gameObject))
					{
						WriteSpawnMessage(innerNetObject, innerNetObject.OwnerId, innerNetObject.SpawnFlags, messageWriter);
					}
				}
			}
			messageWriter.EndMessage();
			SendPhotonMessage(messageWriter, SendOption.Reliable, clientId);
			messageWriter.Recycle();
		}

		protected abstract void OnGameCreated(string gameIdString);

		protected abstract void OnGameJoined(string gameIdString, ClientData client);

		protected abstract void OnWaitForHost(string gameIdString);

		protected abstract void OnStartGame();

		protected abstract void OnGameEnd(GameOverReason reason, bool showAd);

		protected abstract void OnBecomeHost();

		protected abstract void OnPlayerJoined(ClientData client);

		protected abstract void OnPlayerChangedScene(ClientData client, string targetScene);

		protected abstract void OnPlayerLeft(ClientData client, DisconnectReasons reason);

		protected abstract void OnDisconnected();

		protected abstract void OnGetGameList(int totalGames, List<GameListing> availableGames);

		protected abstract byte[] GetConnectionData();

		protected ClientData FindClientById(int id)
		{
			lock (allClients)
			{
				for (int i = 0; i < allClients.Count; i++)
				{
					if (allClients[i].Id == id)
					{
						return allClients[i];
					}
				}
				return null;
			}
		}

		public static string IntToGameName(int gameId)
		{
			char[] array = new char[4]
			{
				(char)(gameId & 0xFF),
				(char)((gameId >> 8) & 0xFF),
				(char)((gameId >> 16) & 0xFF),
				(char)((gameId >> 24) & 0xFF)
			};
			if (array.Any((char c) => c < 'A' || c > 'z'))
			{
				return null;
			}
			return new string(array);
		}

		public static int GameNameToInt(string gameId)
		{
			if (gameId.Length != 4)
			{
				return -1;
			}
			gameId = gameId.ToUpperInvariant();
			return (int)(gameId[0] | ((uint)gameId[1] << 8) | ((uint)gameId[2] << 16) | ((uint)gameId[3] << 24));
		}

		private void FixedUpdate()
		{
			if (mode == MatchMakerModes.None || Streams == null)
			{
				timer = 0f;
				return;
			}
			timer += Time.fixedDeltaTime;
			if (timer < MinSendInterval)
			{
				return;
			}
			timer = 0f;
			lock (allObjects)
			{
				for (int i = 0; i < allObjects.Count; i++)
				{
					InnerNetObject innerNetObject = allObjects[i];
					if ((bool)innerNetObject && innerNetObject.DirtyBits != 0 && (innerNetObject.AmOwner || (innerNetObject.OwnerId == -2 && AmHost)))
					{
						MessageWriter messageWriter = Streams[(uint)innerNetObject.sendMode];
						messageWriter.StartMessage(1);
						messageWriter.WritePacked(innerNetObject.NetId);
						if (innerNetObject.Serialize(messageWriter, false))
						{
							messageWriter.EndMessage();
						}
						else
						{
							messageWriter.CancelMessage();
						}
					}
				}
			}
			for (int j = 0; j < Streams.Length; j++)
			{
				MessageWriter messageWriter2 = Streams[j];
				try
				{
					if (!messageWriter2.HasBytes(7))
					{
						continue;
					}
					messageWriter2.EndMessage();
					SendPhotonMessage(messageWriter2, (SendOption)j);
					goto IL_019b;
				}
				catch (Exception exception)
				{
					Debug.LogException(exception);
					goto IL_019b;
				}
				IL_019b:
				messageWriter2.Clear((SendOption)j);
				messageWriter2.StartMessage(5);
				messageWriter2.Write(GameId);
			}
		}

		public T FindObjectByNetId<T>(uint netId) where T : InnerNetObject
		{
			InnerNetObject value;
			if (allObjectsFast.TryGetValue(netId, out value))
			{
				return (T)value;
			}
			Debug.LogWarning("Couldn't find target object: " + netId);
			return null;
		}

		public void SendRpcImmediately(uint targetNetId, byte callId, SendOption option)
		{
			MessageWriter messageWriter = MessageWriter.Get(option);
			messageWriter.StartMessage(5);
			messageWriter.Write(GameId);
			messageWriter.StartMessage(2);
			messageWriter.WritePacked(targetNetId);
			messageWriter.Write(callId);
			messageWriter.EndMessage();
			messageWriter.EndMessage();
			SendPhotonMessage(messageWriter, option);
			messageWriter.Recycle();
		}

		public MessageWriter StartRpcImmediately(uint targetNetId, byte callId, SendOption option, int targetClientId = -1)
		{
			MessageWriter messageWriter = MessageWriter.Get(option);
			if (targetClientId < 0)
			{
				messageWriter.StartMessage(5);
				messageWriter.Write(GameId);
			}
			else
			{
				messageWriter.StartMessage(6);
				messageWriter.Write(GameId);
				messageWriter.WritePacked(targetClientId);
			}
			messageWriter.StartMessage(2);
			messageWriter.WritePacked(targetNetId);
			messageWriter.Write(callId);
			return messageWriter;
		}

		public void FinishRpcImmediately(MessageWriter msg)
		{
			msg.EndMessage();
			msg.EndMessage();
			SendPhotonMessage(msg, SendOption.Reliable);
			msg.Recycle();
		}

		public void SendRpc(uint targetNetId, byte callId, SendOption option = SendOption.Reliable)
		{
			MessageWriter messageWriter = StartRpc(targetNetId, callId, option);
			messageWriter.EndMessage();
		}

		public MessageWriter StartRpc(uint targetNetId, byte callId, SendOption option = SendOption.Reliable)
		{
			MessageWriter messageWriter = Streams[(uint)option];
			messageWriter.StartMessage(2);
			messageWriter.WritePacked(targetNetId);
			messageWriter.Write(callId);
			return messageWriter;
		}

		private void SendSceneChange(string sceneName)
		{
			if (PhotonNetwork.InRoom)
			{
				PhotonNetwork.LocalPlayer.SetCustomProperties(new Hashtable { { PlayerSceneProperty, sceneName } });
				StartCoroutine(CoSendSceneChange(sceneName));
			}
		}

		private IEnumerator CoSendSceneChange(string sceneName)
		{
			lock (allObjects)
			{
				int i = allObjects.Count - 1;
				while (i > -1)
				{
					InnerNetObject obj = allObjects[i];
					if (!obj)
					{
						allObjects.RemoveAt(i);
					}
					int num = i - 1;
					i = num;
				}
			}
			while (PhotonNetwork.InRoom && ClientId < 0)
			{
				yield return null;
			}
			if (!PhotonNetwork.InRoom)
			{
				yield break;
			}
			if (!AmHost)
			{
				MessageWriter msg = MessageWriter.Get(SendOption.Reliable);
				msg.StartMessage(5);
				msg.Write(GameId);
				msg.StartMessage(6);
				msg.WritePacked(ClientId);
				msg.Write(sceneName);
				msg.EndMessage();
				msg.EndMessage();
				try
				{
					SendPhotonMessage(msg, SendOption.Reliable, HostId);
				}
				catch
				{
				}
				finally
				{
					msg.Recycle();
				}
			}
			ClientData client = FindClientById(ClientId);
			if (client != null)
			{
				Debug.Log(string.Format("Changed scene: {0} {1}", ClientId, sceneName));
				lock (DispatchQueue)
				{
					DispatchQueue.Add(delegate
					{
						OnPlayerChangedScene(client, sceneName);
					});
				}
			}
			else
			{
				Debug.Log(string.Format("Couldn't find self in clients: {0}.", ClientId));
			}
		}

		public void Spawn(InnerNetObject netObjParent, int ownerId = -2, SpawnFlags flags = SpawnFlags.None)
		{
			if (!AmHost)
			{
				if (AmClient)
				{
					Debug.LogError("Tried to spawn while not host:" + netObjParent);
				}
			}
			else
			{
				ownerId = ((ownerId == -3) ? ClientId : ownerId);
				MessageWriter msg = Streams[1];
				WriteSpawnMessage(netObjParent, ownerId, flags, msg);
			}
		}

		private void WriteSpawnMessage(InnerNetObject netObjParent, int ownerId, SpawnFlags flags, MessageWriter msg)
		{
			msg.StartMessage(4);
			msg.WritePacked(netObjParent.SpawnId);
			msg.WritePacked(ownerId);
			msg.Write((byte)flags);
			InnerNetObject[] componentsInChildren = netObjParent.GetComponentsInChildren<InnerNetObject>();
			msg.WritePacked(componentsInChildren.Length);
			foreach (InnerNetObject innerNetObject in componentsInChildren)
			{
				innerNetObject.OwnerId = ownerId;
				innerNetObject.SpawnFlags = flags;
				if (innerNetObject.NetId == 0)
				{
					RegisterNetObject(innerNetObject);
				}
				msg.WritePacked(innerNetObject.NetId);
				msg.StartMessage(1);
				innerNetObject.Serialize(msg, true);
				msg.EndMessage();
			}
			msg.EndMessage();
		}

		public void Despawn(InnerNetObject objToDespawn)
		{
			if (objToDespawn.NetId < 1)
			{
				Debug.Log("Tried to net destroy: " + objToDespawn);
				return;
			}
			MessageWriter messageWriter = Streams[1];
			messageWriter.StartMessage(5);
			messageWriter.WritePacked(objToDespawn.NetId);
			messageWriter.EndMessage();
			RemoveNetObject(objToDespawn);
		}

		private void RegisterNetObject(InnerNetObject obj)
		{
			if (obj.NetId == 0)
			{
				obj.NetId = NetIdCnt++;
				allObjects.Add(obj);
				allObjectsFast.Add(obj.NetId, obj);
			}
			else
			{
				Debug.LogError("Attempted to double register: " + obj.name);
			}
		}

		private bool AddNetObject(InnerNetObject obj)
		{
			uint num = obj.NetId + 1;
			NetIdCnt = ((NetIdCnt > num) ? NetIdCnt : num);
			if (!allObjectsFast.ContainsKey(obj.NetId))
			{
				allObjects.Add(obj);
				allObjectsFast.Add(obj.NetId, obj);
				return true;
			}
			return false;
		}

		public void RemoveNetObject(InnerNetObject obj)
		{
			int num = allObjects.BinarySearch(obj);
			if (num > -1)
			{
				allObjects.RemoveAt(num);
			}
			allObjectsFast.Remove(obj.NetId);
			obj.NetId = uint.MaxValue;
		}

		public void RemoveUnownedObjects()
		{
			HashSet<int> hashSet = new HashSet<int>();
			hashSet.Add(-2);
			lock (allClients)
			{
				for (int num = allClients.Count - 1; num >= 0; num--)
				{
					ClientData clientData = allClients[num];
					if ((bool)clientData.Character)
					{
						hashSet.Add(clientData.Id);
					}
				}
			}
			lock (allObjects)
			{
				for (int num2 = allObjects.Count - 1; num2 > -1; num2--)
				{
					InnerNetObject innerNetObject = allObjects[num2];
					if (!innerNetObject)
					{
						allObjects.RemoveAt(num2);
					}
					else if (!hashSet.Contains(innerNetObject.OwnerId))
					{
						innerNetObject.OwnerId = ClientId;
						UnityEngine.Object.Destroy(innerNetObject.gameObject);
					}
				}
			}
		}

		private void HandleGameData(MessageReader parentReader)
		{
			try
			{
				while (parentReader.Position < parentReader.Length)
				{
					MessageReader messageReader = parentReader.ReadMessage();
					switch (messageReader.Tag)
					{
					case 1:
					{
						uint num = messageReader.ReadPackedUInt32();
						InnerNetObject value;
						if (allObjectsFast.TryGetValue(num, out value))
						{
							value.Deserialize(messageReader, false);
						}
						else
						{
							Debug.LogWarning("Couldn't find target obj: " + num);
						}
						break;
					}
					case 2:
					{
						uint num3 = messageReader.ReadPackedUInt32();
						InnerNetObject value2;
						if (allObjectsFast.TryGetValue(num3, out value2))
						{
							value2.HandleRpc(messageReader.ReadByte(), messageReader);
						}
						else
						{
							Debug.LogWarning(string.Format("Couldn't find target obj: {0} = {1}", num3, string.Join(" ", parentReader.Buffer)));
						}
						break;
					}
					case 4:
					{
						uint num4 = messageReader.ReadPackedUInt32();
						if (num4 < SpawnableObjects.Length)
						{
							InnerNetObject innerNetObject2 = UnityEngine.Object.Instantiate(SpawnableObjects[num4]);
							int num5 = messageReader.ReadPackedInt32();
							innerNetObject2.SpawnFlags = (SpawnFlags)messageReader.ReadByte();
							Debug.Log(string.Format("Spawn {0} ({1}) for {2} with flags {3}", num4, innerNetObject2.name, num5, innerNetObject2.SpawnFlags));
							int num6 = messageReader.ReadPackedInt32();
							InnerNetObject[] componentsInChildren = innerNetObject2.GetComponentsInChildren<InnerNetObject>();
							if (num6 != componentsInChildren.Length)
							{
								Debug.LogError("Children didn't match for spawnable " + num4);
								UnityEngine.Object.Destroy(innerNetObject2.gameObject);
								break;
							}
							if ((innerNetObject2.SpawnFlags & SpawnFlags.IsClientCharacter) != SpawnFlags.None)
							{
								ClientData clientData3 = FindClientById(num5);
								if (clientData3 != null)
								{
									if ((bool)clientData3.Character)
									{
										UnityEngine.Object.Destroy(innerNetObject2.gameObject);
										break;
									}
									clientData3.InScene = true;
									clientData3.Character = innerNetObject2 as PlayerControl;
								}
							}
							for (int num7 = 0; num7 < num6; num7++)
							{
								InnerNetObject innerNetObject3 = componentsInChildren[num7];
								innerNetObject3.NetId = messageReader.ReadPackedUInt32();
								innerNetObject3.OwnerId = num5;
								if (!AddNetObject(innerNetObject3))
								{
									Debug.LogWarning(string.Format("Duplicate spawn {0}: {1}", innerNetObject3.NetId, innerNetObject3.name));
									innerNetObject2.NetId = uint.MaxValue;
									UnityEngine.Object.Destroy(innerNetObject2.gameObject);
									break;
								}
								MessageReader messageReader2 = messageReader.ReadMessage();
								if (messageReader2.Length > 0)
								{
									innerNetObject3.Deserialize(messageReader2, true);
								}
							}
						}
						else
						{
							Debug.LogWarning("Couldn't find spawnable prefab: " + num4);
						}
						break;
					}
					case 5:
					{
						uint num2 = messageReader.ReadPackedUInt32();
						InnerNetObject innerNetObject = FindObjectByNetId<InnerNetObject>(num2);
						if ((bool)innerNetObject)
						{
							RemoveNetObject(innerNetObject);
							UnityEngine.Object.Destroy(innerNetObject.gameObject);
						}
						else
						{
							Debug.LogWarning("Couldn't despawn netId: " + num2);
						}
						break;
					}
					case 6:
					{
						ClientData client = FindClientById(messageReader.ReadPackedInt32());
						string targetScene = messageReader.ReadString();
						ClientData clientData2 = client;
						Debug.Log(string.Format("Client {0} changed scene to {1}", (clientData2 != null) ? clientData2.Id : (-1), targetScene));
						if (client == null || string.IsNullOrWhiteSpace(targetScene))
						{
							break;
						}
						lock (DispatchQueue)
						{
							DispatchQueue.Add(delegate
							{
								OnPlayerChangedScene(client, targetScene);
							});
						}
						break;
					}
					case 7:
					{
						ClientData clientData = FindClientById(messageReader.ReadPackedInt32());
						if (clientData != null)
						{
							Debug.Log(string.Format("Client {0} ready", clientData.Id));
							clientData.IsReady = true;
						}
						break;
					}
					default:
						Debug.Log(string.Format("Bad tag {0} at {1}+{2}={3}:  ", messageReader.Tag, messageReader.Offset, messageReader.Position, messageReader.Length) + string.Join(" ", messageReader.Buffer));
						break;
					}
				}
			}
			finally
			{
				parentReader.Recycle();
			}
		}
	}
}
