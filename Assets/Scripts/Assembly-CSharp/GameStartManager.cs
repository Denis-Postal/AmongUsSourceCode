using InnerNet;
using UnityEngine;

public class GameStartManager : DestroyableSingleton<GameStartManager>, IDisconnectHandler
{
	private enum StartingStates
	{
		NotStarting = 0,
		Countdown = 1,
		Starting = 2
	}

	public int MinPlayers = 4;

	public TextRenderer PlayerCounter;

	private int LastPlayerCount = -1;

	public GameObject GameSizePopup;

	public TextRenderer GameRoomName;

	public LobbyBehaviour LobbyPrefab;

	public TextRenderer GameStartText;

	public SpriteRenderer StartButton;

	public SpriteRenderer MakePublicButton;

	public Sprite PublicGameImage;

	public Sprite PrivateGameImage;

	private StartingStates startState;

	private float countDownTimer;

	public void Start()
	{
		if ((bool)GameSizePopup)
		{
			GameSizePopup.SetActive(value: false);
		}
		if (DestroyableSingleton<TutorialManager>.InstanceExists)
		{
			Object.Destroy(base.gameObject);
			return;
		}
		string text = InnerNetClient.IntToGameName(AmongUsClient.Instance.GameId);
		if (AmongUsClient.Instance.GameMode == GameModes.LocalGame)
		{
			if ((bool)GameRoomName)
			{
				GameRoomName.gameObject.SetActive(value: false);
			}
			StartButton.transform.localPosition = new Vector3(0f, -0.2f, 0f);
			PlayerCounter.transform.localPosition = new Vector3(0f, -0.8f, 0f);
		}
		else if (text != null)
		{
			GameRoomName.Text = DestroyableSingleton<TranslationController>.Instance.GetString(StringNames.RoomCode) + "\r\n" + text;
		}
		else
		{
			StartButton.transform.localPosition = new Vector3(0f, -0.2f, 0f);
			PlayerCounter.transform.localPosition = new Vector3(0f, -0.8f, 0f);
		}
		AmongUsClient.Instance.DisconnectHandlers.AddUnique(this);
		if (!AmongUsClient.Instance.AmHost)
		{
			StartButton.gameObject.SetActive(value: false);
		}
		else
		{
			LobbyBehaviour.Instance = Object.Instantiate(LobbyPrefab);
			AmongUsClient.Instance.Spawn(LobbyBehaviour.Instance);
		}
		MakePublicButton.gameObject.SetActive(AmongUsClient.Instance.GameMode == GameModes.OnlineGame);
	}

	public void MakePublic()
	{
		if (AmongUsClient.Instance.AmHost)
		{
			AmongUsClient.Instance.ChangeGamePublic(!AmongUsClient.Instance.IsGamePublic);
		}
	}

	public void Update()
	{
		if ((bool)GameSizePopup && GameSizePopup.activeSelf)
		{
			GameSizePopup.SetActive(value: false);
		}
		if (!GameData.Instance)
		{
			return;
		}
		MakePublicButton.sprite = (AmongUsClient.Instance.IsGamePublic ? PublicGameImage : PrivateGameImage);
		if (GameData.Instance.PlayerCount != LastPlayerCount)
		{
			LastPlayerCount = GameData.Instance.PlayerCount;
			string arg = "[FF0000FF]";
			if (LastPlayerCount > MinPlayers)
			{
				arg = "[00FF00FF]";
			}
			if (LastPlayerCount == MinPlayers)
			{
				arg = "[FFFF00FF]";
			}
			PlayerCounter.Text = $"{arg}{LastPlayerCount}/{PlayerControl.GameOptions.MaxPlayers}";
			StartButton.color = ((LastPlayerCount >= MinPlayers) ? Palette.EnabledColor : Palette.DisabledColor);
			if (DestroyableSingleton<DiscordManager>.InstanceExists)
			{
				if (AmongUsClient.Instance.AmHost && AmongUsClient.Instance.GameMode == GameModes.OnlineGame)
				{
					DestroyableSingleton<DiscordManager>.Instance.SetInLobbyHost(LastPlayerCount, AmongUsClient.Instance.GameId);
				}
				else
				{
					DestroyableSingleton<DiscordManager>.Instance.SetInLobbyClient();
				}
			}
		}
		if (!AmongUsClient.Instance.AmHost)
		{
			return;
		}
		if (startState == StartingStates.Countdown)
		{
			int num = Mathf.CeilToInt(countDownTimer);
			countDownTimer -= Time.deltaTime;
			int num2 = Mathf.CeilToInt(countDownTimer);
			GameStartText.Text = $"Starting in {num2}";
			if (num != num2)
			{
				PlayerControl.LocalPlayer.RpcSetStartCounter(num2);
			}
			if (num2 <= 0)
			{
				FinallyBegin();
			}
		}
		else
		{
			GameStartText.Text = string.Empty;
		}
	}

	public void ResetStartState()
	{
		startState = StartingStates.NotStarting;
		if ((bool)StartButton && (bool)StartButton.gameObject)
		{
			StartButton.gameObject.SetActive(AmongUsClient.Instance.AmHost);
		}
		if ((bool)PlayerControl.LocalPlayer)
		{
			PlayerControl.LocalPlayer.RpcSetStartCounter(-1);
		}
	}

	public void SetStartCounter(sbyte sec)
	{
		if (sec == -1)
		{
			GameStartText.Text = string.Empty;
			return;
		}
		GameStartText.Text = DestroyableSingleton<TranslationController>.Instance.GetString(StringNames.GameStarting, sec);
	}

	public void BeginGame()
	{
		if (startState == StartingStates.NotStarting)
		{
			if (GameData.Instance.PlayerCount < MinPlayers)
			{
				StartCoroutine(Effects.SwayX(PlayerCounter.transform));
			}
			else
			{
				ReallyBegin(neverShow: false);
			}
		}
	}

	public void ReallyBegin(bool neverShow)
	{
		startState = StartingStates.Countdown;
		if (neverShow)
		{
			SaveManager.ShowMinPlayerWarning = false;
		}
		StartButton.gameObject.SetActive(value: false);
		countDownTimer = 10.0001f;
		startState = StartingStates.Countdown;
	}

	public void FinallyBegin()
	{
		if (startState == StartingStates.Countdown)
		{
			startState = StartingStates.Starting;
			AmongUsClient.Instance.StartGame();
			AmongUsClient.Instance.DisconnectHandlers.Remove(this);
			Object.Destroy(base.gameObject);
		}
	}

	public void HandleDisconnect(PlayerControl pc, DisconnectReasons reason)
	{
		if (AmongUsClient.Instance.AmHost)
		{
			LastPlayerCount = -1;
			if ((bool)StartButton)
			{
				StartButton.gameObject.SetActive(value: true);
			}
		}
	}

	public void HandleDisconnect()
	{
		HandleDisconnect(null, DisconnectReasons.ExitGame);
	}
}
