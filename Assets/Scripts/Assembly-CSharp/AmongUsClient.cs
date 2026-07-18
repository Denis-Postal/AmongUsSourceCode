using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Assets.CoreScripts;
using InnerNet;
using UnityEngine;
using UnityEngine.SceneManagement;

public class AmongUsClient : InnerNetClient
{
	public static AmongUsClient Instance;

	public GameModes GameMode;

	public string OnlineScene;

	public string MainMenuScene;

	public GameData GameDataPrefab;

	public PlayerControl PlayerPrefab;

	public List<ShipStatus> ShipPrefabs;

	public int TutorialMapId;

	public float SpawnRadius = 1.75f;

	public DiscoveryState discoverState;

	public List<IDisconnectHandler> DisconnectHandlers = new List<IDisconnectHandler>();

	public List<IGameListHandler> GameListHandlers = new List<IGameListHandler>();

	public void Awake()
	{
		if ((bool)Instance)
		{
			if (Instance != this)
			{
				UnityEngine.Object.Destroy(base.gameObject);
			}
		}
		else
		{
			Instance = this;
			UnityEngine.Object.DontDestroyOnLoad(base.gameObject);
			Application.targetFrameRate = 30;
		}
	}

	protected override byte[] GetConnectionData()
	{
		using (MemoryStream memoryStream = new MemoryStream())
		{
			using (BinaryWriter binaryWriter = new BinaryWriter(memoryStream))
			{
				binaryWriter.Write(Constants.GetBroadcastVersion());
				binaryWriter.Write(SaveManager.PlayerName);
				binaryWriter.Flush();
				return memoryStream.ToArray();
			}
		}
	}

	public void StartGame()
	{
		SendStartGame();
		discoverState = DiscoveryState.Off;
	}

	public void ExitGame(DisconnectReasons reason = DisconnectReasons.ExitGame)
	{
		if (DestroyableSingleton<WaitForHostPopup>.InstanceExists)
		{
			DestroyableSingleton<WaitForHostPopup>.Instance.Hide();
		}
		SoundManager.Instance.StopAllSound();
		discoverState = DiscoveryState.Off;
		DisconnectHandlers.Clear();
		DisconnectInternal(reason);
		SceneManager.LoadScene(MainMenuScene);
	}

	protected override void OnGetGameList(int totalGames, List<GameListing> availableGames)
	{
		for (int i = 0; i < GameListHandlers.Count; i++)
		{
			try
			{
				GameListHandlers[i].HandleList(totalGames, availableGames);
			}
			catch
			{
			}
		}
	}

	protected override void OnGameCreated(string gameIdString)
	{
	}

	protected override void OnWaitForHost(string gameIdString)
	{
		if (GameState != GameStates.Joined)
		{
			Debug.Log("Waiting for host: " + gameIdString);
			if (DestroyableSingleton<WaitForHostPopup>.InstanceExists)
			{
				DestroyableSingleton<WaitForHostPopup>.Instance.Show();
			}
		}
	}

	protected override void OnStartGame()
	{
		Debug.Log("Received game start: " + base.AmHost);
		StartCoroutine(CoStartGame());
	}

	private IEnumerator CoStartGame()
	{
		yield return null;
		while (!DestroyableSingleton<HudManager>.InstanceExists)
		{
			yield return null;
		}
		while (!PlayerControl.LocalPlayer)
		{
			yield return null;
		}
		PlayerControl.LocalPlayer.moveable = false;
		CustomPlayerMenu customPlayerMenu = UnityEngine.Object.FindObjectOfType<CustomPlayerMenu>();
		if ((bool)customPlayerMenu)
		{
			customPlayerMenu.Close(canMove: false);
		}
		if (DestroyableSingleton<GameStartManager>.InstanceExists)
		{
			DisconnectHandlers.Remove(DestroyableSingleton<GameStartManager>.Instance);
			UnityEngine.Object.Destroy(DestroyableSingleton<GameStartManager>.Instance.gameObject);
		}
		if (DestroyableSingleton<DiscordManager>.InstanceExists)
		{
			DestroyableSingleton<DiscordManager>.Instance.SetPlayingGame();
		}
		yield return DestroyableSingleton<HudManager>.Instance.CoFadeFullScreen(Color.clear, Color.black);
		while (!GameData.Instance)
		{
			yield return null;
		}
		while (true)
		{
			if (base.AmHost)
			{
				GameData.Instance.SetDirty();
				SendClientReady();
				float timer = 0f;
				while (true)
				{
					bool stopWaiting = true;
					lock (allClients)
					{
						for (int i = 0; i < allClients.Count; i++)
						{
							ClientData clientData = allClients[i];
							if (!clientData.IsReady)
							{
								if (timer < 5f)
								{
									stopWaiting = false;
									continue;
								}
								SendLateRejection(clientData.Id, DisconnectReasons.Error);
								clientData.IsReady = true;
								OnPlayerLeft(clientData, DisconnectReasons.Error);
							}
						}
					}
					yield return null;
					if (stopWaiting)
					{
						break;
					}
					timer += Time.deltaTime;
				}
				if ((bool)LobbyBehaviour.Instance)
				{
					LobbyBehaviour.Instance.Despawn();
				}
				if (!ShipStatus.Instance)
				{
					int index = Mathf.Clamp(PlayerControl.GameOptions.MapId, 0, ShipPrefabs.Count - 1);
					ShipStatus.Instance = UnityEngine.Object.Instantiate(ShipPrefabs[index]);
				}
				Spawn(ShipStatus.Instance);
				ShipStatus.Instance.SelectInfected();
				ShipStatus.Instance.Begin();
				break;
			}
			while (PlayerControl.LocalPlayer.Data == null && !base.AmHost)
			{
				yield return null;
			}
			if (!base.AmHost)
			{
				SendClientReady();
				while (!ShipStatus.Instance && !base.AmHost)
				{
					yield return null;
				}
				if (!base.AmHost)
				{
					break;
				}
			}
		}
		for (int j = 0; j < GameData.Instance.PlayerCount; j++)
		{
			PlayerControl playerControl = GameData.Instance.AllPlayers[j].Object;
			if ((bool)playerControl)
			{
				playerControl.moveable = true;
				playerControl.NetTransform.enabled = true;
				playerControl.MyPhysics.enabled = true;
				playerControl.MyPhysics.Awake();
				playerControl.MyPhysics.ResetAnim();
				playerControl.Collider.enabled = true;
				Vector2 spawnLocation = ShipStatus.Instance.GetSpawnLocation(j, GameData.Instance.PlayerCount, initialSpawn: true);
				playerControl.NetTransform.SnapTo(spawnLocation);
			}
		}
	}

	protected override void OnBecomeHost()
	{
		ClientData clientData = FindClientById(ClientId);
		if (!clientData.Character)
		{
			OnGameJoined(null, clientData);
		}
		Debug.Log("Became Host");
		RemoveUnownedObjects();
	}

	protected override void OnGameEnd(GameOverReason gameOverReason, bool showAd)
	{
		StatsManager.Instance.BanPoints -= 1.5f;
		StatsManager.Instance.LastGameStarted = DateTime.MinValue;
		DisconnectHandlers.Clear();
		if ((bool)Minigame.Instance)
		{
			Minigame.Instance.Close();
			Minigame.Instance.Close();
		}
		try
		{
			DestroyableSingleton<Telemetry>.Instance.EndGame(gameOverReason);
		}
		catch
		{
		}
		TempData.EndReason = gameOverReason;
		TempData.showAd = showAd;
		bool flag = TempData.DidHumansWin(gameOverReason);
		TempData.winners = new List<WinningPlayerData>();
		for (int i = 0; i < GameData.Instance.PlayerCount; i++)
		{
			GameData.PlayerInfo playerInfo = GameData.Instance.AllPlayers[i];
			if (gameOverReason == GameOverReason.HumansDisconnect || gameOverReason == GameOverReason.ImpostorDisconnect || flag != playerInfo.IsImpostor)
			{
				TempData.winners.Add(new WinningPlayerData(playerInfo));
			}
		}
		StartCoroutine(CoEndGame());
	}

	public IEnumerator CoEndGame()
	{
		yield return DestroyableSingleton<HudManager>.Instance.CoFadeFullScreen(Color.clear, Color.black, 0.5f);
		SceneManager.LoadScene("EndGame");
	}

	protected override void OnPlayerJoined(ClientData data)
	{
		if (DestroyableSingleton<GameStartManager>.InstanceExists)
		{
			DestroyableSingleton<GameStartManager>.Instance.ResetStartState();
		}
		if (base.AmHost && data.InScene)
		{
			CreatePlayer(data);
		}
	}

	protected override void OnGameJoined(string gameIdString, ClientData data)
	{
		if (DestroyableSingleton<WaitForHostPopup>.InstanceExists)
		{
			DestroyableSingleton<WaitForHostPopup>.Instance.Hide();
		}
		if (!string.IsNullOrWhiteSpace(OnlineScene))
		{
			SceneManager.LoadScene(OnlineScene);
		}
	}

	protected override void OnPlayerLeft(ClientData data, DisconnectReasons reason)
	{
		if (DestroyableSingleton<GameStartManager>.InstanceExists)
		{
			DestroyableSingleton<GameStartManager>.Instance.ResetStartState();
		}
		PlayerControl character = data.Character;
		if ((bool)character)
		{
			for (int num = DisconnectHandlers.Count - 1; num > -1; num--)
			{
				try
				{
					DisconnectHandlers[num].HandleDisconnect(character, reason);
				}
				catch (Exception exception)
				{
					Debug.LogException(exception);
					DisconnectHandlers.RemoveAt(num);
				}
			}
			UnityEngine.Object.Destroy(character.gameObject);
		}
		else
		{
			Debug.LogWarning($"A player without a character disconnected: {data.Id}: {data.InScene}");
			for (int num2 = DisconnectHandlers.Count - 1; num2 > -1; num2--)
			{
				try
				{
					DisconnectHandlers[num2].HandleDisconnect();
				}
				catch (Exception exception2)
				{
					Debug.LogException(exception2);
					DisconnectHandlers.RemoveAt(num2);
				}
			}
		}
		if (base.AmHost)
		{
			GameOptionsData gameOptions = PlayerControl.GameOptions;
			if (gameOptions != null && gameOptions.isDefaults)
			{
				PlayerControl.GameOptions.SetRecommendations(GameData.Instance.PlayerCount, Instance.GameMode);
				PlayerControl.LocalPlayer?.RpcSyncSettings(PlayerControl.GameOptions);
			}
		}
		RemoveUnownedObjects();
	}

	protected override void OnDisconnected()
	{
		if (SceneManager.GetActiveScene().name == MainMenuScene)
		{
			if (DestroyableSingleton<MatchMaker>.InstanceExists)
			{
				DestroyableSingleton<MatchMaker>.Instance.NotConnecting();
			}
			if (LastDisconnectReason != DisconnectReasons.ExitGame && LastDisconnectReason != DisconnectReasons.Destroy && DestroyableSingleton<DisconnectPopup>.InstanceExists)
			{
				DestroyableSingleton<DisconnectPopup>.Instance.Show();
			}
			return;
		}
		SceneManager.LoadScene(MainMenuScene);
	}

	protected override void OnPlayerChangedScene(ClientData client, string currentScene)
	{
		client.InScene = true;
		if (!base.AmHost)
		{
			return;
		}
		if (currentScene.Equals("Tutorial"))
		{
			GameData.Instance = UnityEngine.Object.Instantiate(GameDataPrefab);
			Spawn(GameData.Instance);
			Spawn(UnityEngine.Object.Instantiate(ShipPrefabs[TutorialMapId]));
			CreatePlayer(client);
		}
		else
		{
			if (!currentScene.Equals("OnlineGame"))
			{
				return;
			}
			if (client.Id != ClientId)
			{
				SendInitialData(client.Id);
			}
			else
			{
				if (GameMode == GameModes.LocalGame)
				{
					StartCoroutine(CoBroadcastManager());
				}
				if (!GameData.Instance)
				{
					GameData.Instance = UnityEngine.Object.Instantiate(GameDataPrefab);
					Spawn(GameData.Instance);
				}
			}
			CreatePlayer(client);
		}
	}

	[ContextMenu("Spawn Tester")]
	private void SpawnTester()
	{
		sbyte availableId = GameData.Instance.GetAvailableId();
		Vector2 vector = Vector2.up.Rotate((float)availableId * (360f / (float)Palette.PlayerColors.Length)) * SpawnRadius;
		PlayerControl playerControl = UnityEngine.Object.Instantiate(PlayerPrefab, vector, Quaternion.identity);
		playerControl.PlayerId = (byte)availableId;
		GameData.Instance.AddPlayer(playerControl);
		Spawn(playerControl);
		playerControl.CmdCheckName("Test");
		playerControl.CmdCheckColor(0);
		if (DestroyableSingleton<HatManager>.InstanceExists)
		{
			playerControl.RpcSetHat((uint)(availableId % DestroyableSingleton<HatManager>.Instance.AllHats.Count));
			playerControl.RpcSetSkin((uint)(availableId % DestroyableSingleton<HatManager>.Instance.AllSkins.Count));
			playerControl.RpcSetPet((uint)availableId);
		}
	}

	private void CreatePlayer(ClientData clientData)
	{
		if ((bool)clientData.Character)
		{
			return;
		}
		if (!base.AmHost)
		{
			Debug.Log("Waiting for host to make my player");
			return;
		}
		if (!GameData.Instance)
		{
			GameData.Instance = UnityEngine.Object.Instantiate(GameDataPrefab);
			Spawn(GameData.Instance);
		}
		sbyte availableId = GameData.Instance.GetAvailableId();
		if (availableId == -1)
		{
			SendLateRejection(clientData.Id, DisconnectReasons.GameFull);
			Debug.Log("Overfilled room.");
			return;
		}
		Vector2 vector = Vector2.zero;
		if ((bool)ShipStatus.Instance)
		{
			vector = ShipStatus.Instance.GetSpawnLocation(availableId, Palette.PlayerColors.Length, initialSpawn: false);
		}
		else if (DestroyableSingleton<TutorialManager>.InstanceExists)
		{
			vector = new Vector2(-1.9f, 3.25f);
		}
		Debug.Log($"Spawned player {availableId} for client {clientData.Id}");
		PlayerControl playerControl = UnityEngine.Object.Instantiate(PlayerPrefab, vector, Quaternion.identity);
		playerControl.PlayerId = (byte)availableId;
		clientData.Character = playerControl;
		Spawn(playerControl, clientData.Id, SpawnFlags.IsClientCharacter);
		GameData.Instance.AddPlayer(playerControl);
		if (PlayerControl.GameOptions.isDefaults)
		{
			PlayerControl.GameOptions.SetRecommendations(GameData.Instance.PlayerCount, Instance.GameMode);
		}
		playerControl.RpcSyncSettings(PlayerControl.GameOptions);
	}

	private IEnumerator CoBroadcastManager()
	{
		while (!GameData.Instance)
		{
			yield return null;
		}
		int lastPlayerCount = 0;
		discoverState = DiscoveryState.Broadcast;
		while (discoverState == DiscoveryState.Broadcast)
		{
			if (lastPlayerCount != GameData.Instance.PlayerCount)
			{
				lastPlayerCount = GameData.Instance.PlayerCount;
				string data = $"{SaveManager.PlayerName}~Open~{GameData.Instance.PlayerCount}~";
				DestroyableSingleton<InnerDiscover>.Instance.Interval = 1f;
				DestroyableSingleton<InnerDiscover>.Instance.StartAsServer(data);
			}
			yield return null;
		}
		DestroyableSingleton<InnerDiscover>.Instance.StopServer();
	}
}
