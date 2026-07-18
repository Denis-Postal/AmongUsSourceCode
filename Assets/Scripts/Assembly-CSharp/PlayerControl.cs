using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Assets.CoreScripts;
using Hazel;
using InnerNet;
using PowerTools;
using UnityEngine;

public class PlayerControl : InnerNetObject
{
	public class ColliderComparer : IEqualityComparer<Collider2D>
	{
		public static readonly ColliderComparer Instance = new ColliderComparer();

		public bool Equals(Collider2D x, Collider2D y)
		{
			return x == y;
		}

		public int GetHashCode(Collider2D obj)
		{
			return obj.GetInstanceID();
		}
	}

	public class UsableComparer : IEqualityComparer<IUsable>
	{
		public static readonly UsableComparer Instance = new UsableComparer();

		public bool Equals(IUsable x, IUsable y)
		{
			return x == y;
		}

		public int GetHashCode(IUsable obj)
		{
			return obj.GetHashCode();
		}
	}

	public enum RpcCalls : byte
	{
		PlayAnimation = 0,
		CompleteTask = 1,
		SyncSettings = 2,
		SetInfected = 3,
		Exiled = 4,
		CheckName = 5,
		SetName = 6,
		CheckColor = 7,
		SetColor = 8,
		SetHat = 9,
		SetSkin = 10,
		ReportDeadBody = 11,
		MurderPlayer = 12,
		SendChat = 13,
		TimesImpostor = 14,
		StartMeeting = 15,
		SetScanner = 16,
		SendChatNote = 17,
		SetPet = 18,
		SetStartCounter = 19
	}

	public byte PlayerId = byte.MaxValue;

	public float MaxReportDistance = 5f;

	public bool moveable = true;

	public bool inVent;

	public static PlayerControl LocalPlayer;

	private GameData.PlayerInfo _cachedData;

	public AudioSource FootSteps;

	public AudioClip KillSfx;

	public KillAnimation[] KillAnimations;

	[SerializeField]
	private float killTimer;

	public int RemainingEmergencies;

	public TextRenderer nameText;

	public LightSource LightPrefab;

	private LightSource myLight;

	[HideInInspector]
	public Collider2D Collider;

	[HideInInspector]
	public PlayerPhysics MyPhysics;

	[HideInInspector]
	public CustomNetworkTransform NetTransform;

	public PetBehaviour CurrentPet;

	public SpriteRenderer HatRenderer;

	private SpriteRenderer myRend;

	private static Shader shadowClippedSpriteShader;

	private Collider2D[] hitBuffer = new Collider2D[20];

	public static GameOptionsData GameOptions = new GameOptionsData();

	public List<PlayerTask> myTasks = new List<PlayerTask>();

	[NonSerialized]
	public uint TaskIdCount;

	public SpriteAnim[] ScannerAnims;

	public SpriteRenderer[] ScannersImages;

	public AudioClip[] VentMoveSounds;

	public AudioClip VentEnterSound;

	private IUsable closest;

	private bool isNew = true;

	public float crewStreak;

	public static List<PlayerControl> AllPlayerControls = new List<PlayerControl>();

	private Dictionary<Collider2D, IUsable> cache = new Dictionary<Collider2D, IUsable>(ColliderComparer.Instance);

	private List<IUsable> itemsInRange = new List<IUsable>();

	private List<IUsable> newItemsInRange = new List<IUsable>();

	private byte scannerCount;

	private int LastStartCounter;

	public bool CanMove
	{
		get
		{
			if (moveable && !Minigame.Instance && (!DestroyableSingleton<HudManager>.InstanceExists || (!DestroyableSingleton<HudManager>.Instance.Chat.IsOpen && !DestroyableSingleton<HudManager>.Instance.KillOverlay.IsOpen && !DestroyableSingleton<HudManager>.Instance.GameMenu.IsOpen)) && (!MapBehaviour.Instance || !MapBehaviour.Instance.IsOpenStopped) && !MeetingHud.Instance && !CustomPlayerMenu.Instance && !ExileController.Instance)
			{
				return !IntroCutscene.Instance;
			}
			return false;
		}
	}

	public GameData.PlayerInfo Data
	{
		get
		{
			if (_cachedData == null)
			{
				if (!GameData.Instance)
				{
					return null;
				}
				_cachedData = GameData.Instance.GetPlayerById(PlayerId);
			}
			return _cachedData;
		}
	}

	public bool Visible
	{
		get
		{
			return myRend.enabled;
		}
		set
		{
			myRend.enabled = value;
			MyPhysics.Skin.Visible = value;
			HatRenderer.enabled = value;
			if ((bool)CurrentPet)
			{
				CurrentPet.Visible = value;
			}
			nameText.gameObject.SetActive(value);
		}
	}

	public void SetKillTimer(float time)
	{
		killTimer = time;
		if (GameOptions.KillCooldown > 0f)
		{
			DestroyableSingleton<HudManager>.Instance.KillButton.SetCoolDown(killTimer, GameOptions.KillCooldown);
		}
		else
		{
			DestroyableSingleton<HudManager>.Instance.KillButton.SetCoolDown(0f, GameOptions.KillCooldown);
		}
	}

	private void Awake()
	{
		myRend = GetComponent<SpriteRenderer>();
		MyPhysics = GetComponent<PlayerPhysics>();
		NetTransform = GetComponent<CustomNetworkTransform>();
		Collider = GetComponent<Collider2D>();
		AllPlayerControls.Add(this);
	}

	private void Start()
	{
		RemainingEmergencies = GameOptions.NumEmergencyMeetings;
		if (base.AmOwner)
		{
			myLight = UnityEngine.Object.Instantiate(LightPrefab);
			myLight.transform.SetParent(base.transform);
			myLight.transform.localPosition = Collider.offset;
			LocalPlayer = this;
			Camera.main.GetComponent<FollowerCamera>().SetTarget(this);
			SetName(SaveManager.PlayerName);
			SetColor(SaveManager.BodyColor);
			CmdCheckName(SaveManager.PlayerName);
			CmdCheckColor(SaveManager.BodyColor);
			RpcSetPet(SaveManager.LastPet);
			RpcSetHat(SaveManager.LastHat);
			RpcSetSkin(SaveManager.LastSkin);
			RpcSetTimesImpostor(StatsManager.Instance.CrewmateStreak);
		}
		else
		{
			StartCoroutine(ClientInitialize());
		}
		if (isNew)
		{
			isNew = false;
			StartCoroutine(MyPhysics.CoSpawnPlayer(LobbyBehaviour.Instance));
		}
	}

	private IEnumerator ClientInitialize()
	{
		Visible = false;
		while (!GameData.Instance)
		{
			yield return null;
		}
		while (Data == null)
		{
			yield return null;
		}
		while (string.IsNullOrEmpty(Data.PlayerName))
		{
			yield return null;
		}
		SetName(Data.PlayerName);
		SetColor(Data.ColorId);
		SetHat(Data.HatId);
		SetSkin(Data.SkinId);
		SetPet(Data.PetId);
		Visible = true;
	}

	public override void OnDestroy()
	{
		if ((bool)CurrentPet)
		{
			UnityEngine.Object.Destroy(CurrentPet.gameObject);
		}
		AllPlayerControls.Remove(this);
		base.OnDestroy();
	}

	private void FixedUpdate()
	{
		if (!GameData.Instance)
		{
			return;
		}
		GameData.PlayerInfo data = Data;
		if (data == null)
		{
			return;
		}
		if (data.IsDead && (bool)LocalPlayer && LocalPlayer.Data != null)
		{
			Visible = LocalPlayer.Data.IsDead;
		}
		UpdateShadowClip();
		if (!base.AmOwner)
		{
			return;
		}
		if ((bool)ShipStatus.Instance)
		{
			myLight.LightRadius = ShipStatus.Instance.CalculateLightRadius(data);
		}
		if (data.IsImpostor && CanMove && !data.IsDead)
		{
			SetKillTimer(Mathf.Max(0f, killTimer - Time.fixedDeltaTime));
			PlayerControl target = FindClosestTarget();
			DestroyableSingleton<HudManager>.Instance.KillButton.SetTarget(target);
		}
		else
		{
			DestroyableSingleton<HudManager>.Instance.KillButton.SetTarget(null);
		}
		if (CanMove || inVent)
		{
			newItemsInRange.Clear();
			bool flag = (GameOptions.GhostsDoTasks || !data.IsDead) && (!AmongUsClient.Instance || !AmongUsClient.Instance.IsGameOver) && CanMove;
			Vector2 truePosition = GetTruePosition();
			int num = Physics2D.OverlapCircleNonAlloc(truePosition, MaxReportDistance, hitBuffer, Constants.Usables);
			IUsable usable = null;
			float num2 = float.MaxValue;
			bool flag2 = false;
			for (int i = 0; i < num; i++)
			{
				Collider2D collider2D = hitBuffer[i];
				if (!cache.TryGetValue(collider2D, out var value))
				{
					IUsable usable2 = (cache[collider2D] = collider2D.GetComponent<IUsable>());
					value = usable2;
				}
				if (value != null && (flag || inVent))
				{
					bool canUse;
					bool couldUse;
					float num3 = value.CanUse(data, out canUse, out couldUse);
					if (canUse || couldUse)
					{
						newItemsInRange.Add(value);
					}
					if (canUse && num3 < num2)
					{
						num2 = num3;
						usable = value;
					}
				}
				if (flag && !data.IsDead && !flag2 && collider2D.tag == "DeadBody")
				{
					DeadBody component2 = collider2D.GetComponent<DeadBody>();
					if (!PhysicsHelpers.AnythingBetween(truePosition, component2.TruePosition, Constants.ShipAndObjectsMask, useTriggers: false))
					{
						flag2 = true;
					}
				}
			}
			for (int num4 = itemsInRange.Count - 1; num4 > -1; num4--)
			{
				IUsable item = itemsInRange[num4];
				int num5 = newItemsInRange.FindIndex((IUsable j) => j == item);
				if (num5 == -1)
				{
					item.SetOutline(on: false, mainTarget: false);
					itemsInRange.RemoveAt(num4);
				}
				else
				{
					newItemsInRange.RemoveAt(num5);
					item.SetOutline(on: true, usable == item);
				}
			}
			for (int num6 = 0; num6 < newItemsInRange.Count; num6++)
			{
				IUsable usable3 = newItemsInRange[num6];
				usable3.SetOutline(on: true, usable == usable3);
				itemsInRange.Add(usable3);
			}
			closest = usable;
			DestroyableSingleton<HudManager>.Instance.UseButton.SetTarget(usable);
			DestroyableSingleton<HudManager>.Instance.ReportButton.SetActive(flag2);
		}
		else
		{
			closest = null;
			DestroyableSingleton<HudManager>.Instance.UseButton.SetTarget(null);
			DestroyableSingleton<HudManager>.Instance.ReportButton.SetActive(isActive: false);
		}
	}

	private void UpdateShadowClip()
	{
		if (!myRend)
		{
			return;
		}
		bool enabled = DestroyableSingleton<HudManager>.InstanceExists && (bool)DestroyableSingleton<HudManager>.Instance.ShadowQuad && DestroyableSingleton<HudManager>.Instance.ShadowQuad.gameObject.activeInHierarchy && !Data.IsDead;
		myRend.material.SetFloat("_PlayerShadowClipEnabled", enabled ? 1f : 0f);
		if ((bool)nameText)
		{
			nameText.SetShadowClipEnabled(enabled);
		}
		ApplyShadowClipToSprite(HatRenderer, enabled);
		if ((bool)MyPhysics && (bool)MyPhysics.Skin)
		{
			ApplyShadowClipToSprite(MyPhysics.Skin.layer, enabled);
		}
		if ((bool)CurrentPet && (bool)CurrentPet.rend)
		{
			CurrentPet.rend.material.SetFloat("_PlayerShadowClipEnabled", enabled ? 1f : 0f);
		}
	}

	private static void ApplyShadowClipToSprite(SpriteRenderer spriteRenderer, bool enabled)
	{
		if (!spriteRenderer)
		{
			return;
		}
		Material material = spriteRenderer.material;
		if ((bool)material && !material.HasProperty("_PlayerShadowClipEnabled"))
		{
			if (!shadowClippedSpriteShader)
			{
				shadowClippedSpriteShader = Shader.Find("Unlit/ShadowClippedSprite");
				if (!shadowClippedSpriteShader)
				{
					shadowClippedSpriteShader = Resources.Load<Shader>("shaders/ShadowClippedSprite");
				}
			}
			if ((bool)shadowClippedSpriteShader)
			{
				material.shader = shadowClippedSpriteShader;
			}
		}
		if ((bool)material)
		{
			material.SetFloat("_PlayerShadowClipEnabled", enabled ? 1f : 0f);
		}
	}

	public void UseClosest()
	{
		if (closest != null)
		{
			closest.Use();
		}
		closest = null;
		DestroyableSingleton<HudManager>.Instance.UseButton.SetTarget(null);
	}

	public void ReportClosest()
	{
		if (AmongUsClient.Instance.IsGameOver || LocalPlayer.Data.IsDead)
		{
			return;
		}
		Collider2D[] array = Physics2D.OverlapCircleAll(base.transform.position, MaxReportDistance, Constants.NotShipMask);
		foreach (Collider2D collider2D in array)
		{
			if (collider2D.tag != "DeadBody")
			{
				continue;
			}
			DeadBody component = collider2D.GetComponent<DeadBody>();
			if ((bool)component && !component.Reported)
			{
				component.OnClick();
				if (component.Reported)
				{
					break;
				}
			}
		}
	}

	public void PlayStepSound()
	{
		if (Constants.ShouldPlaySfx() && DestroyableSingleton<HudManager>.InstanceExists && LocalPlayer == this)
		{
			ShipRoom lastRoom = DestroyableSingleton<HudManager>.Instance.roomTracker.LastRoom;
			if ((bool)lastRoom && (bool)lastRoom.FootStepSounds)
			{
				AudioClip clip = lastRoom.FootStepSounds.Random();
				FootSteps.clip = clip;
				FootSteps.Play();
			}
		}
	}

	private void SetScanner(bool on, byte cnt)
	{
		if (cnt < scannerCount)
		{
			return;
		}
		scannerCount = cnt;
		for (int i = 0; i < ScannerAnims.Length; i++)
		{
			SpriteAnim spriteAnim = ScannerAnims[i];
			if (on && !Data.IsDead)
			{
				spriteAnim.gameObject.SetActive(value: true);
				spriteAnim.Play();
				ScannersImages[i].flipX = !myRend.flipX;
				continue;
			}
			if (spriteAnim.isActiveAndEnabled)
			{
				spriteAnim.Stop();
			}
			spriteAnim.gameObject.SetActive(value: false);
		}
	}

	public Vector2 GetTruePosition()
	{
		return (Vector2)base.transform.position + Collider.offset;
	}

	private PlayerControl FindClosestTarget()
	{
		PlayerControl result = null;
		float num = GameOptionsData.KillDistances[Mathf.Clamp(GameOptions.KillDistance, 0, 2)];
		if (!ShipStatus.Instance)
		{
			return null;
		}
		Vector2 truePosition = GetTruePosition();
		List<GameData.PlayerInfo> allPlayers = GameData.Instance.AllPlayers;
		for (int i = 0; i < allPlayers.Count; i++)
		{
			GameData.PlayerInfo playerInfo = allPlayers[i];
			if (playerInfo.Disconnected || playerInfo.PlayerId == PlayerId || playerInfo.IsDead || playerInfo.IsImpostor)
			{
				continue;
			}
			PlayerControl playerControl = playerInfo.Object;
			if ((bool)playerControl)
			{
				Vector2 vector = playerControl.GetTruePosition() - truePosition;
				float magnitude = vector.magnitude;
				if (magnitude <= num && !PhysicsHelpers.AnyNonTriggersBetween(truePosition, vector.normalized, magnitude, Constants.ShipAndObjectsMask))
				{
					result = playerControl;
					num = magnitude;
				}
			}
		}
		return result;
	}

	public void SetTasks(byte[] tasks)
	{
		StartCoroutine(CoSetTasks(tasks));
	}

	private IEnumerator CoSetTasks(byte[] tasks)
	{
		while (!ShipStatus.Instance)
		{
			yield return null;
		}
		if (base.AmOwner)
		{
			DestroyableSingleton<HudManager>.Instance.TaskStuff.SetActive(value: true);
			StatsManager.Instance.GamesStarted++;
			if (Data.IsImpostor)
			{
				StatsManager.Instance.TimesImpostor++;
				StatsManager.Instance.CrewmateStreak = 0u;
			}
			else
			{
				StatsManager.Instance.TimesCrewmate++;
				StatsManager.Instance.CrewmateStreak++;
				DestroyableSingleton<HudManager>.Instance.KillButton.gameObject.SetActive(value: false);
			}
			try
			{
				DestroyableSingleton<Telemetry>.Instance.StartGame(AmongUsClient.Instance.AmHost, GameData.Instance.PlayerCount, GameOptions.NumImpostors, AmongUsClient.Instance.GameMode, StatsManager.Instance.TimesImpostor, StatsManager.Instance.GamesStarted, StatsManager.Instance.CrewmateStreak, Data.ColorId);
			}
			catch
			{
			}
		}
		myTasks.DestroyAll();
		foreach (byte idx in tasks)
		{
			NormalPlayerTask normalPlayerTask = UnityEngine.Object.Instantiate(ShipStatus.Instance.GetTaskById(idx), base.transform);
			normalPlayerTask.Id = TaskIdCount++;
			normalPlayerTask.Owner = this;
			normalPlayerTask.Initialize();
			myTasks.Add(normalPlayerTask);
		}
	}

	public void AddSystemTask(SystemTypes system)
	{
		PlayerTask original;
		switch (system)
		{
		default:
			return;
		case SystemTypes.Reactor:
			original = ShipStatus.Instance.SpecialTasks[0];
			break;
		case SystemTypes.LifeSupp:
			original = ShipStatus.Instance.SpecialTasks[3];
			break;
		case SystemTypes.Electrical:
			original = ShipStatus.Instance.SpecialTasks[1];
			break;
		case SystemTypes.Comms:
			original = ShipStatus.Instance.SpecialTasks[2];
			break;
		}
		PlayerControl localPlayer = LocalPlayer;
		PlayerTask playerTask = UnityEngine.Object.Instantiate(original, localPlayer.transform);
		playerTask.Id = (byte)localPlayer.TaskIdCount++;
		playerTask.Owner = localPlayer;
		playerTask.Initialize();
		localPlayer.myTasks.Add(playerTask);
	}

	public void RemoveTask(PlayerTask task)
	{
		task.OnRemove();
		myTasks.Remove(task);
		GameData.Instance.TutOnlyRemoveTask(PlayerId, task.Id);
		DestroyableSingleton<HudManager>.Instance.UseButton.SetTarget(null);
		UnityEngine.Object.Destroy(task.gameObject);
	}

	private void ClearTasks()
	{
		for (int i = 0; i < myTasks.Count; i++)
		{
			PlayerTask playerTask = myTasks[i];
			playerTask.OnRemove();
			UnityEngine.Object.Destroy(playerTask.gameObject);
		}
		myTasks.Clear();
	}

	public void RemoveInfected()
	{
		GameData.PlayerInfo playerById = GameData.Instance.GetPlayerById(PlayerId);
		if (playerById.IsImpostor)
		{
			playerById.Object.nameText.Color = Color.white;
			playerById.IsImpostor = false;
			myTasks.RemoveAt(0);
			DestroyableSingleton<HudManager>.Instance.KillButton.gameObject.SetActive(value: false);
		}
	}

	public void Die(DeathReason reason)
	{
		if (!DestroyableSingleton<TutorialManager>.InstanceExists)
		{
			StatsManager.Instance.LastGameStarted = DateTime.MinValue;
			StatsManager.Instance.BanPoints--;
		}
		TempData.LastDeathReason = reason;
		if ((bool)CurrentPet)
		{
			CurrentPet.SetMourning();
		}
		Data.IsDead = true;
		base.gameObject.layer = LayerMask.NameToLayer("Ghost");
		nameText.GetComponent<MeshRenderer>().material.SetInt("_Mask", 0);
		if (base.AmOwner)
		{
			DestroyableSingleton<HudManager>.Instance.Chat.SetVisible(visible: true);
		}
	}

	public void Revive()
	{
		Data.IsDead = false;
		base.gameObject.layer = LayerMask.NameToLayer("Players");
		MyPhysics.ResetAnim();
		if ((bool)CurrentPet)
		{
			CurrentPet.Source = this;
		}
		nameText.GetComponent<MeshRenderer>().material.SetInt("_Mask", 4);
		if (base.AmOwner)
		{
			DestroyableSingleton<HudManager>.Instance.ShadowQuad.gameObject.SetActive(value: true);
			DestroyableSingleton<HudManager>.Instance.KillButton.gameObject.SetActive(Data.IsImpostor);
			DestroyableSingleton<HudManager>.Instance.Chat.ForceClosed();
			DestroyableSingleton<HudManager>.Instance.Chat.SetVisible(visible: false);
		}
	}

	public void PlayAnimation(byte animType)
	{
		switch ((TaskTypes)animType)
		{
		case TaskTypes.ClearAsteroids:
			ShipStatus.Instance.FireWeapon();
			break;
		case TaskTypes.PrimeShields:
			ShipStatus.Instance.StartShields();
			break;
		case TaskTypes.EmptyChute:
		case TaskTypes.EmptyGarbage:
			ShipStatus.Instance.OpenHatch();
			break;
		}
	}

	public void CompleteTask(uint idx)
	{
		PlayerTask playerTask = myTasks.Find((PlayerTask p) => p.Id == idx);
		if ((bool)playerTask)
		{
			GameData.Instance.CompleteTask(this, idx);
			playerTask.Complete();
			DestroyableSingleton<Telemetry>.Instance.WriteCompleteTask(PlayerId, playerTask.TaskType);
		}
		else
		{
			Debug.LogWarning(PlayerId + ": Server didn't have task: " + idx);
		}
	}

	public void SetInfected(byte[] infected)
	{
		if (!GameData.Instance)
		{
			Debug.Log("No game data instance.");
		}
		StatsManager.Instance.BanPoints++;
		StatsManager.Instance.LastGameStarted = DateTime.UtcNow;
		for (int i = 0; i < infected.Length; i++)
		{
			GameData.PlayerInfo playerById = GameData.Instance.GetPlayerById(infected[i]);
			if (playerById != null)
			{
				playerById.IsImpostor = true;
			}
			else
			{
				Debug.LogError("Couldn't set impostor: " + infected[i]);
			}
		}
		DestroyableSingleton<HudManager>.Instance.MapButton.gameObject.SetActive(value: true);
		DestroyableSingleton<HudManager>.Instance.ReportButton.gameObject.SetActive(value: true);
		LocalPlayer.RemainingEmergencies = GameOptions.NumEmergencyMeetings;
		GameData.PlayerInfo data = LocalPlayer.Data;
		if (data.IsImpostor)
		{
			ImportantTextTask importantTextTask = new GameObject("_Player").AddComponent<ImportantTextTask>();
			importantTextTask.transform.SetParent(LocalPlayer.transform, worldPositionStays: false);
			importantTextTask.Text = DestroyableSingleton<TranslationController>.Instance.GetString(StringNames.ImpostorTask) + "\r\n[FFFFFFFF]" + DestroyableSingleton<TranslationController>.Instance.GetString(StringNames.FakeTasks);
			myTasks.Insert(0, importantTextTask);
			DestroyableSingleton<HudManager>.Instance.KillButton.gameObject.SetActive(value: true);
			LocalPlayer.SetKillTimer(10f);
			for (int j = 0; j < infected.Length; j++)
			{
				GameData.PlayerInfo playerById2 = GameData.Instance.GetPlayerById(infected[j]);
				if (playerById2 != null)
				{
					playerById2.Object.nameText.Color = Palette.ImpostorRed;
				}
			}
		}
		if (!DestroyableSingleton<TutorialManager>.InstanceExists)
		{
			List<PlayerControl> yourTeam = ((!data.IsImpostor) ? (from pcd in GameData.Instance.AllPlayers
				where !pcd.Disconnected
				select pcd.Object into pc
				orderby (!(pc == LocalPlayer)) ? 1 : 0
				select pc).ToList() : (from pcd in GameData.Instance.AllPlayers
				where !pcd.Disconnected
				where pcd.IsImpostor
				select pcd.Object into pc
				orderby (!(pc == LocalPlayer)) ? 1 : 0
				select pc).ToList());
			StopAllCoroutines();
			DestroyableSingleton<HudManager>.Instance.StartCoroutine(DestroyableSingleton<HudManager>.Instance.CoShowIntro(yourTeam));
		}
	}

	public void Exiled()
	{
		Die(DeathReason.Exile);
		if (base.AmOwner)
		{
			StatsManager.Instance.TimesEjected++;
			DestroyableSingleton<HudManager>.Instance.ShadowQuad.gameObject.SetActive(value: false);
			ImportantTextTask importantTextTask = new GameObject("_Player").AddComponent<ImportantTextTask>();
			importantTextTask.transform.SetParent(base.transform, worldPositionStays: false);
			if (Data.IsImpostor)
			{
				ClearTasks();
				importantTextTask.Text = DestroyableSingleton<TranslationController>.Instance.GetString(StringNames.GhostImpostor);
			}
			else if (!GameOptions.GhostsDoTasks)
			{
				ClearTasks();
				importantTextTask.Text = DestroyableSingleton<TranslationController>.Instance.GetString(StringNames.GhostIgnoreTasks);
			}
			else
			{
				importantTextTask.Text = DestroyableSingleton<TranslationController>.Instance.GetString(StringNames.GhostDoTasks);
			}
			myTasks.Insert(0, importantTextTask);
		}
	}

	public void CheckName(string name)
	{
		List<GameData.PlayerInfo> allPlayers = GameData.Instance.AllPlayers;
		if (allPlayers.Any((GameData.PlayerInfo i) => i.PlayerId != PlayerId && i.PlayerName.Equals(name, StringComparison.OrdinalIgnoreCase)))
		{
			for (int num = 1; num < 100; num++)
			{
				string text = name + " " + num;
				bool flag = false;
				for (int num2 = 0; num2 < allPlayers.Count; num2++)
				{
					if (allPlayers[num2].PlayerId != PlayerId && allPlayers[num2].PlayerName.Equals(text, StringComparison.OrdinalIgnoreCase))
					{
						flag = true;
						break;
					}
				}
				if (!flag)
				{
					name = text;
					break;
				}
			}
		}
		RpcSetName(name);
		GameData.Instance.UpdateName(PlayerId, name);
	}

	public void SetName(string name)
	{
		if ((bool)GameData.Instance)
		{
			GameData.Instance.UpdateName(PlayerId, name);
		}
		base.gameObject.name = name;
		nameText.Text = name;
		nameText.GetComponent<MeshRenderer>().material.SetInt("_Mask", 4);
	}

	public void CheckColor(byte bodyColor)
	{
		List<GameData.PlayerInfo> allPlayers = GameData.Instance.AllPlayers;
		int num = 0;
		while (num++ < 100 && allPlayers.Any((GameData.PlayerInfo p) => !p.Disconnected && p.PlayerId != PlayerId && p.ColorId == bodyColor))
		{
			bodyColor = (byte)((bodyColor + 1) % Palette.PlayerColors.Length);
		}
		RpcSetColor(bodyColor);
	}

	public void SetHatAlpha(float a)
	{
		Color white = Color.white;
		white.a = a;
		HatRenderer.color = white;
	}

	public void SetColor(byte bodyColor)
	{
		if ((bool)GameData.Instance)
		{
			GameData.Instance.UpdateColor(PlayerId, bodyColor);
		}
		if ((object)myRend == null)
		{
			GetComponent<SpriteRenderer>();
		}
		SetPlayerMaterialColors(bodyColor, myRend);
		if ((bool)CurrentPet)
		{
			SetPlayerMaterialColors(bodyColor, CurrentPet.rend);
		}
	}

	public void SetSkin(uint skinId)
	{
		if ((bool)GameData.Instance)
		{
			GameData.Instance.UpdateSkin(PlayerId, skinId);
		}
		MyPhysics.SetSkin(skinId);
	}

	public void SetHat(uint hatId)
	{
		if ((bool)GameData.Instance)
		{
			GameData.Instance.UpdateHat(PlayerId, hatId);
		}
		SetHatImage(hatId, HatRenderer);
		nameText.transform.localPosition = new Vector3(0f, (hatId == 0) ? 0.7f : 1.05f, -0.5f);
	}

	public void SetPet(uint petId)
	{
		if ((bool)CurrentPet)
		{
			UnityEngine.Object.Destroy(CurrentPet.gameObject);
		}
		CurrentPet = UnityEngine.Object.Instantiate(DestroyableSingleton<HatManager>.Instance.GetPetById(petId));
		CurrentPet.transform.position = base.transform.position;
		CurrentPet.Source = this;
		_ = Data;
		if (Data != null)
		{
			GameData.Instance.UpdatePet(PlayerId, petId);
			Data.PetId = petId;
			SetPlayerMaterialColors(Data.ColorId, CurrentPet.rend);
		}
	}

	public static void SetPetImage(uint petId, int colorId, SpriteRenderer target)
	{
		if (DestroyableSingleton<HatManager>.InstanceExists)
		{
			SetPetImage(DestroyableSingleton<HatManager>.Instance.GetPetById(petId), colorId, target);
		}
	}

	public static void SetPetImage(PetBehaviour pet, int colorId, SpriteRenderer target)
	{
		target.sprite = pet.rend.sprite;
		if (target != pet.rend)
		{
			target.material = new Material(pet.rend.sharedMaterial);
			SetPlayerMaterialColors(colorId, target);
		}
	}

	public static void SetSkinImage(uint skinId, SpriteRenderer target)
	{
		if (DestroyableSingleton<HatManager>.InstanceExists)
		{
			SetSkinImage(DestroyableSingleton<HatManager>.Instance.GetSkinById(skinId), target);
		}
	}

	public static void SetSkinImage(SkinData skin, SpriteRenderer target)
	{
		target.sprite = skin.IdleFrame;
	}

	public static void SetHatImage(uint hatId, SpriteRenderer target)
	{
		if (DestroyableSingleton<HatManager>.InstanceExists)
		{
			SetHatImage(DestroyableSingleton<HatManager>.Instance.GetHatById(hatId), target);
		}
	}

	public static void SetHatImage(HatBehaviour hat, SpriteRenderer target)
	{
		if ((bool)target)
		{
			if ((bool)hat)
			{
				target.sprite = hat.MainImage;
				Vector3 localPosition = target.transform.localPosition;
				localPosition.z = (hat.InFront ? (-0.0001f) : 0.0001f);
				target.transform.localPosition = localPosition;
			}
			else
			{
				string text = ((!hat) ? "null" : hat.name);
				Debug.LogError("Player: " + target.name + "\tHat: " + text);
			}
		}
	}

	private void ReportDeadBody(GameData.PlayerInfo target)
	{
		if (!AmongUsClient.Instance.IsGameOver && !MeetingHud.Instance && (target != null || !LocalPlayer.myTasks.Any(PlayerTask.TaskIsEmergency)) && !Data.IsDead)
		{
			MeetingRoomManager.Instance.AssignSelf(this, target);
			if (AmongUsClient.Instance.AmHost && !ShipStatus.Instance.CheckTaskCompletion())
			{
				DestroyableSingleton<HudManager>.Instance.OpenMeetingRoom(this);
				RpcStartMeeting(target);
			}
		}
	}

	public IEnumerator CoStartMeeting(GameData.PlayerInfo target)
	{
		DestroyableSingleton<Telemetry>.Instance.WriteMeetingStarted(target == null);
		while (!MeetingHud.Instance)
		{
			yield return null;
		}
		MeetingRoomManager.Instance.RemoveSelf();
		DeadBody[] array = UnityEngine.Object.FindObjectsOfType<DeadBody>();
		for (int i = 0; i < array.Length; i++)
		{
			UnityEngine.Object.Destroy(array[i].gameObject);
		}
		for (int j = 0; j < AllPlayerControls.Count; j++)
		{
			PlayerControl playerControl = AllPlayerControls[j];
			if (!playerControl.GetComponent<DummyBehaviour>().enabled)
			{
				playerControl.MyPhysics.ExitAllVents();
				playerControl.NetTransform.SnapTo(ShipStatus.Instance.GetSpawnLocation(playerControl.PlayerId, GameData.Instance.PlayerCount, initialSpawn: false));
			}
		}
		if (base.AmOwner)
		{
			if (target != null)
			{
				StatsManager.Instance.BodiesReported++;
			}
			else
			{
				RemainingEmergencies--;
				StatsManager.Instance.EmergenciesCalled++;
			}
		}
		if ((bool)MapBehaviour.Instance)
		{
			MapBehaviour.Instance.Close();
		}
		if ((bool)Minigame.Instance)
		{
			Minigame.Instance.Close();
		}
		KillAnimation.SetMovement(this, canMove: true);
		MeetingHud.Instance.StartCoroutine(MeetingHud.Instance.CoIntro(this, target));
	}

	public void MurderPlayer(PlayerControl target)
	{
		if (AmongUsClient.Instance.IsGameOver)
		{
			return;
		}
		if (!target || Data.IsDead || !Data.IsImpostor || Data.Disconnected)
		{
			Debug.LogWarning($"Bad kill from {PlayerId} to {((int?)target?.PlayerId) ?? (-1)}");
			return;
		}
		GameData.PlayerInfo data = target.Data;
		if (data == null || data.IsDead)
		{
			return;
		}
		if (base.AmOwner)
		{
			StatsManager.Instance.ImpostorKills++;
			if (Constants.ShouldPlaySfx())
			{
				SoundManager.Instance.PlaySound(LocalPlayer.KillSfx, loop: false, 0.8f);
			}
		}
		SetKillTimer(GameOptions.KillCooldown);
		DestroyableSingleton<Telemetry>.Instance.WriteMurder();
		target.gameObject.layer = LayerMask.NameToLayer("Ghost");
		if (target.AmOwner)
		{
			StatsManager.Instance.TimesMurdered++;
			if ((bool)Minigame.Instance)
			{
				Minigame.Instance.Close();
				Minigame.Instance.Close();
			}
			DestroyableSingleton<HudManager>.Instance.KillOverlay.ShowOne(this, data);
			DestroyableSingleton<HudManager>.Instance.ShadowQuad.gameObject.SetActive(value: false);
			target.nameText.GetComponent<MeshRenderer>().material.SetInt("_Mask", 0);
			target.RpcSetScanner(value: false);
			ImportantTextTask importantTextTask = new GameObject("_Player").AddComponent<ImportantTextTask>();
			importantTextTask.transform.SetParent(base.transform, worldPositionStays: false);
			if (!GameOptions.GhostsDoTasks)
			{
				target.ClearTasks();
				importantTextTask.Text = DestroyableSingleton<TranslationController>.Instance.GetString(StringNames.GhostIgnoreTasks);
			}
			else
			{
				importantTextTask.Text = DestroyableSingleton<TranslationController>.Instance.GetString(StringNames.GhostDoTasks);
			}
			target.myTasks.Insert(0, importantTextTask);
		}
		MyPhysics.StartCoroutine(KillAnimations.Random().CoPerformKill(this, target));
	}

	public override bool Serialize(MessageWriter writer, bool initialState)
	{
		if (initialState)
		{
			writer.Write(isNew);
		}
		writer.Write(PlayerId);
		return true;
	}

	public override void Deserialize(MessageReader reader, bool initialState)
	{
		if (initialState)
		{
			isNew = reader.ReadBoolean();
		}
		PlayerId = reader.ReadByte();
	}

	public void SetPlayerMaterialColors(Renderer rend)
	{
		SetPlayerMaterialColors(GameData.Instance.GetPlayerById(PlayerId)?.ColorId ?? 0, rend);
	}

	public static void SetPlayerMaterialColors(int colorId, Renderer rend)
	{
		if ((bool)rend)
		{
			rend.material.SetColor("_BackColor", Palette.ShadowColors[colorId]);
			rend.material.SetColor("_BodyColor", Palette.PlayerColors[colorId]);
			rend.material.SetColor("_VisorColor", Palette.VisorColor);
		}
	}

	public void RpcSetScanner(bool value)
	{
		byte b = ++scannerCount;
		if (AmongUsClient.Instance.AmClient)
		{
			SetScanner(value, b);
		}
		MessageWriter messageWriter = AmongUsClient.Instance.StartRpc(NetId, 16);
		messageWriter.Write(value);
		messageWriter.Write(b);
		messageWriter.EndMessage();
	}

	public void RpcPlayAnimation(byte animType)
	{
		if (AmongUsClient.Instance.AmClient)
		{
			PlayAnimation(animType);
		}
		MessageWriter messageWriter = AmongUsClient.Instance.StartRpc(NetId, 0, SendOption.None);
		messageWriter.Write(animType);
		messageWriter.EndMessage();
	}

	public void RpcSetStartCounter(int secondsLeft)
	{
		int value = LastStartCounter++;
		MessageWriter messageWriter = AmongUsClient.Instance.StartRpc(NetId, 19);
		messageWriter.WritePacked(value);
		messageWriter.Write((sbyte)secondsLeft);
		messageWriter.EndMessage();
	}

	public void RpcCompleteTask(uint idx)
	{
		if (AmongUsClient.Instance.AmClient)
		{
			CompleteTask(idx);
		}
		MessageWriter messageWriter = AmongUsClient.Instance.StartRpc(NetId, 1);
		messageWriter.WritePacked(idx);
		messageWriter.EndMessage();
	}

	public void RpcSyncSettings(GameOptionsData gameOptions)
	{
		if (AmongUsClient.Instance.AmHost && !DestroyableSingleton<TutorialManager>.InstanceExists)
		{
			GameOptions = gameOptions;
			SaveManager.GameHostOptions = gameOptions;
			MessageWriter messageWriter = AmongUsClient.Instance.StartRpc(NetId, 2);
			messageWriter.WriteBytesAndSize(gameOptions.ToBytes());
			messageWriter.EndMessage();
		}
	}

	public void RpcSetInfected(GameData.PlayerInfo[] infected)
	{
		byte[] array = infected.Select((GameData.PlayerInfo p) => p.PlayerId).ToArray();
		if (AmongUsClient.Instance.AmClient)
		{
			SetInfected(array);
		}
		MessageWriter messageWriter = AmongUsClient.Instance.StartRpc(NetId, 3);
		messageWriter.WriteBytesAndSize(array);
		messageWriter.EndMessage();
	}

	public void CmdCheckName(string name)
	{
		if (AmongUsClient.Instance.AmHost)
		{
			CheckName(name);
			return;
		}
		MessageWriter messageWriter = AmongUsClient.Instance.StartRpcImmediately(NetId, 5, SendOption.Reliable, AmongUsClient.Instance.HostId);
		messageWriter.Write(name);
		AmongUsClient.Instance.FinishRpcImmediately(messageWriter);
	}

	public void RpcSetSkin(uint skinId)
	{
		if (AmongUsClient.Instance.AmClient)
		{
			SetSkin(skinId);
		}
		MessageWriter messageWriter = AmongUsClient.Instance.StartRpc(NetId, 10);
		messageWriter.WritePacked(skinId);
		messageWriter.EndMessage();
	}

	public void RpcSetHat(uint hatId)
	{
		if (AmongUsClient.Instance.AmClient)
		{
			SetHat(hatId);
		}
		MessageWriter messageWriter = AmongUsClient.Instance.StartRpc(NetId, 9);
		messageWriter.WritePacked(hatId);
		messageWriter.EndMessage();
	}

	public void RpcSetPet(uint petId)
	{
		if (AmongUsClient.Instance.AmClient)
		{
			SetPet(petId);
		}
		MessageWriter messageWriter = AmongUsClient.Instance.StartRpc(NetId, 18);
		messageWriter.WritePacked(petId);
		messageWriter.EndMessage();
	}

	public void RpcSetName(string name)
	{
		if (AmongUsClient.Instance.AmClient)
		{
			SetName(name);
		}
		MessageWriter messageWriter = AmongUsClient.Instance.StartRpc(NetId, 6);
		messageWriter.Write(name);
		messageWriter.EndMessage();
	}

	public void CmdCheckColor(byte bodyColor)
	{
		if (AmongUsClient.Instance.AmHost)
		{
			CheckColor(bodyColor);
			return;
		}
		MessageWriter messageWriter = AmongUsClient.Instance.StartRpcImmediately(NetId, 7, SendOption.Reliable, AmongUsClient.Instance.HostId);
		messageWriter.Write(bodyColor);
		AmongUsClient.Instance.FinishRpcImmediately(messageWriter);
	}

	public void RpcSetColor(byte bodyColor)
	{
		if (AmongUsClient.Instance.AmClient)
		{
			SetColor(bodyColor);
		}
		MessageWriter messageWriter = AmongUsClient.Instance.StartRpc(NetId, 8);
		messageWriter.Write(bodyColor);
		messageWriter.EndMessage();
	}

	public void RpcSetTimesImpostor(float percImpostor)
	{
		if (AmongUsClient.Instance.AmClient)
		{
			crewStreak = percImpostor;
		}
		MessageWriter messageWriter = AmongUsClient.Instance.StartRpc(NetId, 14, SendOption.None);
		messageWriter.Write(percImpostor);
		messageWriter.EndMessage();
	}

	public bool RpcSendChat(string chatText)
	{
		if (string.IsNullOrWhiteSpace(chatText))
		{
			return false;
		}
		if (AmongUsClient.Instance.AmClient && (bool)DestroyableSingleton<HudManager>.Instance)
		{
			DestroyableSingleton<HudManager>.Instance.Chat.AddChat(this, chatText);
		}
		if (chatText.IndexOf("who", StringComparison.OrdinalIgnoreCase) >= 0)
		{
			DestroyableSingleton<Telemetry>.Instance.SendWho();
		}
		MessageWriter messageWriter = AmongUsClient.Instance.StartRpc(NetId, 13);
		messageWriter.Write(chatText);
		messageWriter.EndMessage();
		return true;
	}

	public void RpcSendChatNote(byte srcPlayerId, ChatNoteTypes noteType)
	{
		if (AmongUsClient.Instance.AmClient)
		{
			GameData.PlayerInfo playerById = GameData.Instance.GetPlayerById(srcPlayerId);
			DestroyableSingleton<HudManager>.Instance.Chat.AddChatNote(playerById, noteType);
		}
		MessageWriter messageWriter = AmongUsClient.Instance.StartRpc(NetId, 17);
		messageWriter.Write(srcPlayerId);
		messageWriter.Write((byte)noteType);
		messageWriter.EndMessage();
	}

	public void CmdReportDeadBody(GameData.PlayerInfo target)
	{
		if (AmongUsClient.Instance.AmHost)
		{
			ReportDeadBody(target);
			return;
		}
		MessageWriter messageWriter = AmongUsClient.Instance.StartRpc(NetId, 11);
		messageWriter.Write(target?.PlayerId ?? byte.MaxValue);
		messageWriter.EndMessage();
	}

	public void RpcStartMeeting(GameData.PlayerInfo info)
	{
		if (AmongUsClient.Instance.AmClient)
		{
			StartCoroutine(CoStartMeeting(info));
		}
		MessageWriter messageWriter = AmongUsClient.Instance.StartRpcImmediately(NetId, 15, SendOption.Reliable);
		messageWriter.Write(info?.PlayerId ?? byte.MaxValue);
		AmongUsClient.Instance.FinishRpcImmediately(messageWriter);
	}

	public void RpcMurderPlayer(PlayerControl target)
	{
		if (AmongUsClient.Instance.AmClient)
		{
			MurderPlayer(target);
		}
		MessageWriter messageWriter = AmongUsClient.Instance.StartRpcImmediately(NetId, 12, SendOption.Reliable);
		messageWriter.WriteNetObject(target);
		AmongUsClient.Instance.FinishRpcImmediately(messageWriter);
	}

	public override void HandleRpc(byte callId, MessageReader reader)
	{
		switch ((RpcCalls)callId)
		{
		case RpcCalls.PlayAnimation:
			PlayAnimation(reader.ReadByte());
			break;
		case RpcCalls.Exiled:
			Exiled();
			break;
		case RpcCalls.CheckName:
			CheckName(reader.ReadString());
			break;
		case RpcCalls.SetName:
			SetName(reader.ReadString());
			break;
		case RpcCalls.CheckColor:
			CheckColor(reader.ReadByte());
			break;
		case RpcCalls.SetColor:
			SetColor(reader.ReadByte());
			break;
		case RpcCalls.SetSkin:
			SetSkin(reader.ReadPackedUInt32());
			break;
		case RpcCalls.SetHat:
			SetHat(reader.ReadPackedUInt32());
			break;
		case RpcCalls.SetPet:
			SetPet(reader.ReadPackedUInt32());
			break;
		case RpcCalls.SetInfected:
			SetInfected(reader.ReadBytesAndSize());
			break;
		case RpcCalls.SyncSettings:
			GameOptions = GameOptionsData.FromBytes(reader.ReadBytesAndSize());
			break;
		case RpcCalls.ReportDeadBody:
		{
			GameData.PlayerInfo playerById3 = GameData.Instance.GetPlayerById(reader.ReadByte());
			ReportDeadBody(playerById3);
			break;
		}
		case RpcCalls.StartMeeting:
		{
			GameData.PlayerInfo playerById2 = GameData.Instance.GetPlayerById(reader.ReadByte());
			StartCoroutine(CoStartMeeting(playerById2));
			break;
		}
		case RpcCalls.MurderPlayer:
		{
			PlayerControl target = reader.ReadNetObject<PlayerControl>();
			MurderPlayer(target);
			break;
		}
		case RpcCalls.SetStartCounter:
		{
			int num = reader.ReadPackedInt32();
			sbyte startCounter = reader.ReadSByte();
			if (DestroyableSingleton<GameStartManager>.InstanceExists && LastStartCounter < num)
			{
				LastStartCounter = num;
				DestroyableSingleton<GameStartManager>.Instance.SetStartCounter(startCounter);
			}
			break;
		}
		case RpcCalls.CompleteTask:
			CompleteTask(reader.ReadPackedUInt32());
			break;
		case RpcCalls.SendChat:
		{
			string chatText = reader.ReadString();
			if ((bool)DestroyableSingleton<HudManager>.Instance)
			{
				DestroyableSingleton<HudManager>.Instance.Chat.AddChat(this, chatText);
			}
			break;
		}
		case RpcCalls.SendChatNote:
		{
			GameData.PlayerInfo playerById = GameData.Instance.GetPlayerById(reader.ReadByte());
			DestroyableSingleton<HudManager>.Instance.Chat.AddChatNote(playerById, (ChatNoteTypes)reader.ReadByte());
			break;
		}
		case RpcCalls.TimesImpostor:
			crewStreak = reader.ReadSingle();
			break;
		case RpcCalls.SetScanner:
			SetScanner(reader.ReadBoolean(), reader.ReadByte());
			break;
		}
	}
}
