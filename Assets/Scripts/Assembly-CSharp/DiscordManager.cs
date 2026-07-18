#if DISCORD_GAME_SDK || DISCORD
using System;
using System.Collections;
using Discord;
using InnerNet;
using UnityEngine;
using UnityEngine.SceneManagement;

public class DiscordManager : DestroyableSingleton<DiscordManager>
{
	private const long ClientId = 477175586805252107L;

	[NonSerialized]
	private global::Discord.Discord presence;

	private DateTime? StartTime;

	private static readonly DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

	public void Start()
	{
		if (!(DestroyableSingleton<DiscordManager>.Instance == this))
		{
			return;
		}
		try
		{
			presence = new global::Discord.Discord(477175586805252107L, 1uL);
			ActivityManager activityManager = presence.GetActivityManager();
			activityManager.OnActivityJoinRequest += HandleAutoJoin;
			activityManager.OnActivityJoin += HandleJoinRequest;
			SetInMenus();
			SceneManager.sceneLoaded += delegate(Scene scene, LoadSceneMode mode)
			{
				OnSceneChange(scene.name);
			};
		}
		catch
		{
		}
	}

	private void HandleError(int errorCode, string message)
	{
		Debug.LogError(message ?? $"No message: {errorCode}");
	}

	private void OnSceneChange(string name)
	{
		switch (name)
		{
		case "MatchMaking":
		case "MMOnline":
		case "MainMenu":
			SetInMenus();
			break;
		}
	}

	public void FixedUpdate()
	{
		if (presence == null)
		{
			return;
		}
		try
		{
			presence.RunCallbacks();
		}
		catch (ResultException)
		{
			presence.Dispose();
			presence = null;
		}
	}

	public void SetInMenus()
	{
		if (presence != null)
		{
			ClearPresence();
			StartTime = null;
			Activity activity = new Activity
			{
				State = "In Menus",
				Assets = 
				{
					LargeImage = "icon"
				}
			};
			presence.GetActivityManager().UpdateActivity(activity, delegate
			{
			});
		}
	}

	public void SetPlayingGame()
	{
		if (presence != null)
		{
			if (!StartTime.HasValue)
			{
				StartTime = DateTime.UtcNow;
			}
			Activity activity = new Activity
			{
				State = "In Game",
				Details = "Playing",
				Assets = 
				{
					LargeImage = "icon"
				},
				Timestamps = 
				{
					Start = ToUnixTime(StartTime.Value)
				}
			};
			presence.GetActivityManager().UpdateActivity(activity, delegate
			{
			});
		}
	}

	public void SetHowToPlay()
	{
		if (presence != null)
		{
			ClearPresence();
			Activity activity = new Activity
			{
				State = "In Freeplay",
				Assets = 
				{
					LargeImage = "icon"
				}
			};
			presence.GetActivityManager().UpdateActivity(activity, delegate
			{
			});
		}
	}

	public void SetInLobbyClient()
	{
		if (presence != null)
		{
			if (!StartTime.HasValue)
			{
				StartTime = DateTime.UtcNow;
			}
			ClearPresence();
			Activity activity = new Activity
			{
				State = "In Lobby",
				Assets = 
				{
					LargeImage = "icon"
				},
				Timestamps = 
				{
					Start = ToUnixTime(StartTime.Value)
				}
			};
			presence.GetActivityManager().UpdateActivity(activity, delegate
			{
			});
		}
	}

	private void ClearPresence()
	{
		if (presence != null)
		{
			presence.GetActivityManager().ClearActivity(delegate
			{
			});
		}
	}

	public void SetInLobbyHost(int numPlayers, int gameId)
	{
		if (presence != null)
		{
			if (!StartTime.HasValue)
			{
				StartTime = DateTime.UtcNow;
			}
			string text = InnerNetClient.IntToGameName(gameId);
			Activity activity = default(Activity);
			activity.State = "In Lobby";
			activity.Details = "Hosting a game";
			activity.Party.Size.CurrentSize = numPlayers;
			activity.Party.Size.MaxSize = 10;
			activity.Assets.SmallImage = "icon";
			activity.Assets.LargeText = "Ask to play!";
			activity.Secrets.Join = "join" + text;
			activity.Secrets.Match = "match" + text;
			activity.Party.Id = text;
			presence.GetActivityManager().UpdateActivity(activity, delegate
			{
			});
		}
	}

	private void HandleAutoJoin(ref User requestUser)
	{
		Debug.Log("Discord: request from " + requestUser.Username);
		if (AmongUsClient.Instance.IsGameStarted)
		{
			RequestRespondNo(requestUser.Id);
		}
		else
		{
			RequestRespondYes(requestUser.Id);
		}
	}

	private void HandleJoinRequest(string joinSecret)
	{
		if (!joinSecret.StartsWith("join"))
		{
			Debug.LogWarning("Invalid join secret: " + joinSecret);
			return;
		}
		if (!AmongUsClient.Instance)
		{
			Debug.LogWarning("Missing AmongUsClient");
			return;
		}
		if (!DestroyableSingleton<DiscordManager>.InstanceExists)
		{
			Debug.LogWarning("Missing DiscordManager");
			return;
		}
		if (AmongUsClient.Instance.mode != MatchMakerModes.None)
		{
			Debug.LogWarning("Already connected");
			return;
		}
		AmongUsClient.Instance.GameMode = GameModes.OnlineGame;
		AmongUsClient.Instance.GameId = InnerNetClient.GameNameToInt(joinSecret.Substring(4));
		AmongUsClient.Instance.SetEndpoint(DestroyableSingleton<ServerManager>.Instance.OnlineNetAddress, 22023);
		AmongUsClient.Instance.MainMenuScene = "MMOnline";
		AmongUsClient.Instance.OnlineScene = "OnlineGame";
		DestroyableSingleton<DiscordManager>.Instance.StopAllCoroutines();
		DestroyableSingleton<DiscordManager>.Instance.StartCoroutine(DestroyableSingleton<DiscordManager>.Instance.CoJoinGame());
	}

	public IEnumerator CoJoinGame()
	{
		AmongUsClient.Instance.Connect(MatchMakerModes.Client);
		yield return AmongUsClient.Instance.WaitForConnectionOrFail();
		if (AmongUsClient.Instance.ClientId < 0)
		{
			SceneManager.LoadScene("MMOnline");
		}
	}

	public void RequestRespondYes(long userId)
	{
		if (presence != null)
		{
			presence.GetActivityManager().SendRequestReply(userId, ActivityJoinRequestReply.Yes, delegate
			{
			});
		}
	}

	public void RequestRespondNo(long userId)
	{
		if (presence != null)
		{
			Debug.Log("Discord: responding no to Ask to Join request");
			presence.GetActivityManager().SendRequestReply(userId, ActivityJoinRequestReply.No, delegate
			{
			});
		}
	}

	public override void OnDestroy()
	{
		base.OnDestroy();
		if (presence != null)
		{
			presence.Dispose();
		}
	}

	private static long ToUnixTime(DateTime time)
	{
		return (long)(time - epoch).TotalSeconds;
	}
}
#else
using System.Collections;
using UnityEngine;

public class DiscordManager : DestroyableSingleton<DiscordManager>
{
	public void SetInMenus()
	{
	}

	public void SetPlayingGame()
	{
	}

	public void SetHowToPlay()
	{
	}

	public void SetInLobbyClient()
	{
	}

	public void SetInLobbyHost(int numPlayers, int gameId)
	{
	}

	public IEnumerator CoJoinGame()
	{
		yield break;
	}

	public void RequestRespondYes(long userId)
	{
	}

	public void RequestRespondNo(long userId)
	{
	}
}
#endif
