using UnityEngine;

public class DevCheats : MonoBehaviour
{
	public bool ShowPanel = true;

	private int fakePlayerCount;
	private string status = "Ready";

	private void Update()
	{
		if (Input.GetKeyDown(KeyCode.F10))
		{
			ShowPanel = !ShowPanel;
		}
	}

	private void OnGUI()
	{
		if (!ShowPanel)
		{
			return;
		}
		GUI.Box(new Rect(10f, 10f, 285f, 420f), "Dev Cheats");
		GUI.Label(new Rect(20f, 35f, 250f, 20f), "F10 - show/hide");
		if (GUI.Button(new Rect(20f, 60f, 190f, 28f), "Add Fake Player"))
		{
			AddFakePlayer();
		}
		GUI.Label(new Rect(20f, 98f, 220f, 20f), "Set my role/state:");
		if (GUI.Button(new Rect(20f, 122f, 95f, 26f), "Crewmate"))
		{
			SetMyImpostor(false);
		}
		if (GUI.Button(new Rect(125f, 122f, 95f, 26f), "Impostor"))
		{
			SetMyImpostor(true);
		}
		if (GUI.Button(new Rect(20f, 154f, 95f, 26f), "Ghost"))
		{
			SetMyGhost();
		}
		if (GUI.Button(new Rect(125f, 154f, 95f, 26f), "Revive"))
		{
			ReviveMe();
		}
		GUI.Label(new Rect(20f, 192f, 240f, 20f), "Players:");
		DrawPlayerButtons();
		GUI.Label(new Rect(20f, 360f, 245f, 55f), status);
	}

	public void AddFakePlayer()
	{
		if (!(bool)AmongUsClient.Instance || !(bool)GameData.Instance)
		{
			status = "No client or GameData";
			return;
		}
		if (!AmongUsClient.Instance.AmHost)
		{
			status = "Host only";
			return;
		}
		PlayerControl prefab = AmongUsClient.Instance.PlayerPrefab;
		if (!(bool)prefab)
		{
			status = "No PlayerPrefab";
			return;
		}
		sbyte availableId = GameData.Instance.GetAvailableId();
		if (availableId < 0)
		{
			status = "No free player id";
			return;
		}
		Vector2 spawnPosition = GetSpawnPosition((byte)availableId);
		PlayerControl playerControl = Instantiate(prefab, spawnPosition, Quaternion.identity);
		playerControl.PlayerId = (byte)availableId;
		GameData.Instance.AddPlayer(playerControl);
		AmongUsClient.Instance.Spawn(playerControl);
		playerControl.CmdCheckName("Fake " + (++fakePlayerCount));
		playerControl.CmdCheckColor((byte)(availableId % Palette.PlayerColors.Length));
		if (DestroyableSingleton<HatManager>.InstanceExists)
		{
			HatManager instance = DestroyableSingleton<HatManager>.Instance;
			if (instance.AllHats.Count > 0)
			{
				playerControl.RpcSetHat((uint)(availableId % instance.AllHats.Count));
			}
			if (instance.AllSkins.Count > 0)
			{
				playerControl.RpcSetSkin((uint)(availableId % instance.AllSkins.Count));
			}
			if (instance.AllPets.Count > 0)
			{
				playerControl.RpcSetPet((uint)(availableId % instance.AllPets.Count));
			}
		}
		playerControl.gameObject.AddComponent<DevFakePlayer>();
		if ((bool)playerControl.NetTransform)
		{
			playerControl.NetTransform.enabled = false;
		}
		if ((bool)playerControl.MyPhysics)
		{
			playerControl.MyPhysics.enabled = false;
		}
		playerControl.moveable = false;
		status = "Added " + playerControl.PlayerId;
	}

	private void SetMyImpostor(bool impostor)
	{
		PlayerControl localPlayer = PlayerControl.LocalPlayer;
		if (!(bool)localPlayer || localPlayer.Data == null)
		{
			status = "No local player";
			return;
		}
		if (localPlayer.Data.IsDead)
		{
			localPlayer.Revive();
		}
		localPlayer.Data.IsImpostor = impostor;
		localPlayer.nameText.Color = impostor ? Palette.ImpostorRed : Color.white;
		GameData.Instance.RecomputeTaskCounts();
		if (DestroyableSingleton<HudManager>.InstanceExists && (bool)DestroyableSingleton<HudManager>.Instance.KillButton)
		{
			DestroyableSingleton<HudManager>.Instance.KillButton.gameObject.SetActive(impostor);
			if (impostor)
			{
				localPlayer.SetKillTimer(0f);
			}
		}
		status = "Role: " + (impostor ? "Impostor" : "Crewmate");
	}

	private void SetMyGhost()
	{
		PlayerControl localPlayer = PlayerControl.LocalPlayer;
		if (!(bool)localPlayer || localPlayer.Data == null)
		{
			status = "No local player";
			return;
		}
		if (!localPlayer.Data.IsDead)
		{
			localPlayer.Die(DeathReason.Exile);
		}
		localPlayer.moveable = true;
		status = "State: Ghost";
	}

	private void ReviveMe()
	{
		PlayerControl localPlayer = PlayerControl.LocalPlayer;
		if (!(bool)localPlayer || localPlayer.Data == null)
		{
			status = "No local player";
			return;
		}
		localPlayer.Revive();
		localPlayer.moveable = true;
		status = "Revived";
	}

	private void DrawPlayerButtons()
	{
		if (!(bool)GameData.Instance)
		{
			GUI.Label(new Rect(20f, 215f, 230f, 22f), "No GameData");
			return;
		}
		int shown = 0;
		for (int i = 0; i < GameData.Instance.AllPlayers.Count && shown < 5; i++)
		{
			GameData.PlayerInfo playerInfo = GameData.Instance.AllPlayers[i];
			if (playerInfo == null || playerInfo.Disconnected)
			{
				continue;
			}
			PlayerControl obj = playerInfo.Object;
			if (!(bool)obj)
			{
				continue;
			}
			float y = 218f + shown * 28f;
			string text = string.IsNullOrEmpty(playerInfo.PlayerName) ? ("Player " + playerInfo.PlayerId) : playerInfo.PlayerName;
			GUI.Label(new Rect(20f, y, 105f, 22f), text);
			if (GUI.Button(new Rect(130f, y, 60f, 22f), "Kill"))
			{
				KillPlayer(playerInfo);
			}
			if (GUI.Button(new Rect(198f, y, 60f, 22f), "Remove"))
			{
				RemovePlayer(playerInfo);
			}
			shown++;
		}
		if (shown == 0)
		{
			GUI.Label(new Rect(20f, 218f, 230f, 22f), "No players");
		}
	}

	private void KillPlayer(GameData.PlayerInfo playerInfo)
	{
		if (playerInfo == null || !(bool)playerInfo.Object)
		{
			status = "No player";
			return;
		}
		if (!playerInfo.IsDead)
		{
			playerInfo.Object.Exiled();
		}
		status = "Killed " + playerInfo.PlayerName;
	}

	private void RemovePlayer(GameData.PlayerInfo playerInfo)
	{
		if (playerInfo == null || !(bool)playerInfo.Object)
		{
			status = "No player";
			return;
		}
		if (!AmongUsClient.Instance.AmHost)
		{
			status = "Host only";
			return;
		}
		PlayerControl obj = playerInfo.Object;
		byte playerId = playerInfo.PlayerId;
		GameData.Instance.RemovePlayer(playerId);
		obj.Despawn();
		GameData.Instance.RecomputeTaskCounts();
		status = "Removed " + playerInfo.PlayerName;
	}

	private Vector2 GetSpawnPosition(byte playerId)
	{
		if ((bool)PlayerControl.LocalPlayer)
		{
			return (Vector2)PlayerControl.LocalPlayer.transform.position + Vector2.right * (1f + fakePlayerCount * 0.35f);
		}
		return Vector2.up.Rotate((float)(int)playerId * (360f / (float)Palette.PlayerColors.Length)) * AmongUsClient.Instance.SpawnRadius;
	}
}
