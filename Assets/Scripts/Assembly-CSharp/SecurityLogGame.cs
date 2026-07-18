using UnityEngine;

public class SecurityLogGame : Minigame
{
	private SecurityLogBehaviour Logger;

	public ObjectPoolBehavior EntryPool;

	public Scroller scroller;

	public float ScreenHeight = 4f;

	public float EntryHeight = 0.4f;

	public Sprite[] LocationBgs;

	public void Awake()
	{
		Logger = ShipStatus.Instance.GetComponent<SecurityLogBehaviour>();
		RefreshScreen();
	}

	public void Update()
	{
		if (Logger.HasNew)
		{
			Logger.HasNew = false;
			RefreshScreen();
		}
	}

	private void RefreshScreen()
	{
		EntryPool.ReclaimAll();
		int num = 0;
		for (int i = 0; i < Logger.LogEntries.Count; i++)
		{
			SecurityLogBehaviour.SecurityLogEntry securityLogEntry = Logger.LogEntries[i];
			LogEntryBubble logEntryBubble = EntryPool.Get<LogEntryBubble>();
			GameData.PlayerInfo playerById = GameData.Instance.GetPlayerById(securityLogEntry.PlayerId);
			if (playerById == null)
			{
				Debug.Log($"Couldn't find player {securityLogEntry.PlayerId} for log");
				continue;
			}
			PlayerControl.SetPlayerMaterialColors(playerById.ColorId, logEntryBubble.HeadImage);
			string text = DestroyableSingleton<TranslationController>.Instance.GetString((StringNames)(201 + securityLogEntry.Location));
			logEntryBubble.Text.Text = DestroyableSingleton<TranslationController>.Instance.GetString(StringNames.SecLogEntry, playerById.PlayerName, text);
			logEntryBubble.Text.RefreshMesh();
			logEntryBubble.PrepareForDisplay();
			logEntryBubble.Background.sprite = LocationBgs[(byte)securityLogEntry.Location];
			logEntryBubble.transform.localPosition = new Vector3(0f, (float)num * (0f - EntryHeight), 0f);
			num++;
		}
		float max = Mathf.Max(0f, (float)num * EntryHeight - ScreenHeight);
		scroller.YBounds = new FloatRange(0f, max);
		scroller.ScrollPercentY(1f);
	}
}
