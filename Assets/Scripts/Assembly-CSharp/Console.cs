using Assets.CoreScripts;
using UnityEngine;

public class Console : MonoBehaviour, IUsable
{
	public float usableDistance = 1f;

	public int ConsoleId;

	public bool onlyFromBelow;

	public bool onlySameRoom;

	public bool GhostsIgnored;

	public bool AllowImpostor;

	public SystemTypes Room;

	public TaskTypes[] TaskTypes;

	public TaskSet[] ValidTasks;

	public SpriteRenderer Image;

	public float UsableDistance => usableDistance;

	public float PercentCool => 0f;

	public void SetOutline(bool on, bool mainTarget)
	{
		if ((bool)Image)
		{
			Image.material.SetFloat("_Outline", on ? 1 : 0);
			Image.material.SetColor("_OutlineColor", Color.yellow);
			Image.material.SetColor("_AddColor", mainTarget ? Color.yellow : Color.clear);
		}
	}

	public float CanUse(GameData.PlayerInfo pc, out bool canUse, out bool couldUse)
	{
		float num = float.MaxValue;
		PlayerControl playerControl = pc.Object;
		Vector2 truePosition = playerControl.GetTruePosition();
		couldUse = (!pc.IsDead || (PlayerControl.GameOptions.GhostsDoTasks && !GhostsIgnored)) && playerControl.CanMove && (AllowImpostor || !pc.IsImpostor) && (!onlySameRoom || ShipStatus.Instance.FastRooms[Room].roomArea.OverlapPoint(truePosition)) && (!onlyFromBelow || playerControl.transform.position.y < base.transform.position.y) && (bool)FindTask(playerControl);
		canUse = couldUse;
		if (canUse)
		{
			num = Vector2.Distance(truePosition, base.transform.position);
			canUse &= num <= UsableDistance;
		}
		return num;
	}

	private PlayerTask FindTask(PlayerControl pc)
	{
		for (int i = 0; i < pc.myTasks.Count; i++)
		{
			PlayerTask playerTask = pc.myTasks[i];
			if (!playerTask.IsComplete && playerTask.ValidConsole(this))
			{
				return playerTask;
			}
		}
		return null;
	}

	public void Use()
	{
		CanUse(PlayerControl.LocalPlayer.Data, out var canUse, out var _);
		if (canUse)
		{
			PlayerControl localPlayer = PlayerControl.LocalPlayer;
			PlayerTask playerTask = FindTask(localPlayer);
			if ((bool)playerTask.MinigamePrefab)
			{
				Minigame minigame = Object.Instantiate(playerTask.MinigamePrefab);
				minigame.transform.SetParent(Camera.main.transform, worldPositionStays: false);
				minigame.transform.localPosition = new Vector3(0f, 0f, -50f);
				minigame.Console = this;
				minigame.Begin(playerTask);
				DestroyableSingleton<Telemetry>.Instance.WriteUse(localPlayer.PlayerId, playerTask.TaskType, base.transform.position);
			}
		}
	}
}
