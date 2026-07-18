using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChatController : MonoBehaviour
{
	public ObjectPoolBehavior chatBubPool;

	public Transform TypingArea;

	public SpriteRenderer TextBubble;

	public TextBox TextArea;

	public TextRenderer CharCount;

	public int MaxChat = 15;

	public Scroller scroller;

	public GameObject Content;

	public SpriteRenderer BackgroundImage;

	public SpriteRenderer ChatNotifyDot;

	public TextRenderer SendRateMessage;

	public Vector3 SourcePos = new Vector3(0f, 0f, -10f);

	public Vector3 TargetPos = new Vector3(-0.35f, 0.02f, -10f);

	private const float MaxChatSendRate = 3f;

	private float TimeSinceLastMessage = 3f;

	public AudioClip MessageSound;

	private bool animating;

	private Coroutine notificationRoutine;

	public BanMenu BanButton;

	private readonly List<PendingChatMessage> pendingMessages = new List<PendingChatMessage>();

	private bool flushingPendingMessages;

	public bool IsOpen => Content.activeInHierarchy;

	private struct PendingChatMessage
	{
		public PlayerControl SourcePlayer;

		public string Text;

		public PendingChatMessage(PlayerControl sourcePlayer, string text)
		{
			SourcePlayer = sourcePlayer;
			Text = text;
		}
	}

	public void Toggle()
	{
		CustomNetworkTransform customNetworkTransform = PlayerControl.LocalPlayer?.NetTransform;
		if (!animating && (bool)customNetworkTransform)
		{
			StopAllCoroutines();
			if (IsOpen)
			{
				StartCoroutine(CoClose());
				return;
			}
			Content.SetActive(value: true);
			customNetworkTransform.Halt();
			StartCoroutine(CoOpen());
		}
	}

	public void SetVisible(bool visible)
	{
		Debug.Log("Chat is hidden: " + visible);
		ForceClosed();
		base.gameObject.SetActive(visible);
	}

	public void ForceClosed()
	{
		StopAllCoroutines();
		Content.SetActive(value: false);
		animating = false;
	}

	public IEnumerator CoOpen()
	{
		animating = true;
		Vector3 scale = Vector3.one;
		BanButton.Hide();
		BanButton.SetVisible(show: true);
		float targetScale = AspectSize.CalculateSize(base.transform.localPosition, BackgroundImage.sprite);
		float timer = 0f;
		while (timer < 0.15f)
		{
			timer += Time.deltaTime;
			float num = Mathf.SmoothStep(0f, 1f, timer / 0.15f);
			scale.y = (scale.x = Mathf.Lerp(0.1f, targetScale, num));
			Content.transform.localScale = scale;
			Content.transform.localPosition = Vector3.Lerp(SourcePos, TargetPos, num) * targetScale;
			BanButton.transform.localPosition = new Vector3(0f, (0f - num) * 0.75f, -20f);
			yield return null;
		}
		ChatNotifyDot.enabled = false;
		animating = false;
		FlushPendingMessages();
		GiveFocus();
	}

	public IEnumerator CoClose()
	{
		animating = true;
		BanButton.Hide();
		Vector3 scale = Vector3.one;
		float targetScale = AspectSize.CalculateSize(base.transform.localPosition, BackgroundImage.sprite);
		for (float timer = 0f; timer < 0.15f; timer += Time.deltaTime)
		{
			float num = 1f - Mathf.SmoothStep(0f, 1f, timer / 0.15f);
			scale.y = (scale.x = Mathf.Lerp(0.1f, targetScale, num));
			Content.transform.localScale = scale;
			Content.transform.localPosition = Vector3.Lerp(SourcePos, TargetPos, num) * targetScale;
			BanButton.transform.localPosition = new Vector3(0f, (0f - num) * 0.75f, -20f);
			yield return null;
		}
		BanButton.SetVisible(show: false);
		Content.SetActive(value: false);
		animating = false;
	}

	public void SetPosition(MeetingHud meeting)
	{
		if ((bool)meeting)
		{
			base.transform.SetParent(meeting.transform);
			base.transform.localPosition = new Vector3(3.1f, 2.2f, -10f);
		}
		else
		{
			base.transform.SetParent(DestroyableSingleton<HudManager>.Instance.transform);
			GetComponent<AspectPosition>().AdjustPosition();
		}
	}

	public void UpdateCharCount()
	{
		Vector2 size = TextBubble.size;
		size.y = Math.Max(0.62f, TextArea.TextHeight + 0.2f);
		TextBubble.size = size;
		Vector3 localPosition = TextBubble.transform.localPosition;
		localPosition.y = (0.62f - size.y) / 2f;
		TextBubble.transform.localPosition = localPosition;
		Vector3 localPosition2 = TypingArea.localPosition;
		localPosition2.y = -2.08f - localPosition.y * 2f;
		TypingArea.localPosition = localPosition2;
		int length = TextArea.text.Length;
		CharCount.Text = $"{length}/100";
		if (length < 75)
		{
			CharCount.Color = Color.black;
		}
		else if (length < 100)
		{
			CharCount.Color = new Color(1f, 1f, 0f, 1f);
		}
		else
		{
			CharCount.Color = Color.red;
		}
	}

	private void Update()
	{
		TimeSinceLastMessage += Time.deltaTime;
		if (SendRateMessage.isActiveAndEnabled)
		{
			float num = 3f - TimeSinceLastMessage;
			if (num < 0f)
			{
				SendRateMessage.gameObject.SetActive(value: false);
			}
			else
			{
				SendRateMessage.Text = $"Too fast. Wait {Mathf.CeilToInt(num)} seconds";
			}
		}
	}

	public void SendChat()
	{
		float num = 3f - TimeSinceLastMessage;
		if (num > 0f)
		{
			SendRateMessage.gameObject.SetActive(value: true);
			SendRateMessage.Text = $"Too fast. Wait {Mathf.CeilToInt(num)} seconds";
		}
		else if (PlayerControl.LocalPlayer.RpcSendChat(TextArea.text))
		{
			TimeSinceLastMessage = 0f;
			TextArea.Clear();
		}
	}

	public void AddChatNote(GameData.PlayerInfo srcPlayer, ChatNoteTypes noteType)
	{
		if (srcPlayer != null)
		{
			if (chatBubPool.NotInUse == 0)
			{
				chatBubPool.ReclaimOldest();
			}
			ChatBubble chatBubble = chatBubPool.Get<ChatBubble>();
			chatBubble.SetFaceColor(srcPlayer.ColorId);
			chatBubble.transform.SetParent(scroller.Inner);
			chatBubble.transform.localScale = Vector3.one;
			chatBubble.SetNotification();
			if (noteType == ChatNoteTypes.DidVote)
			{
				int votesRemaining = MeetingHud.Instance.GetVotesRemaining();
				chatBubble.SetName(DestroyableSingleton<TranslationController>.Instance.GetString(StringNames.MeetingHasVoted, srcPlayer.PlayerName, votesRemaining), isDead: false, voted: true, Color.green);
			}
			chatBubble.SetText(string.Empty);
			chatBubble.AlignChildren();
			AlignAllBubbles();
			ShowChatNotificationIfClosed();
			if (srcPlayer.Object != PlayerControl.LocalPlayer)
			{
				SoundManager.Instance.PlaySound(MessageSound, loop: false).pitch = 0.5f + (float)(int)srcPlayer.PlayerId / 10f;
			}
		}
	}

	public void AddChat(PlayerControl sourcePlayer, string chatText)
	{
		if (!sourcePlayer || !PlayerControl.LocalPlayer)
		{
			return;
		}
		if (!IsOpen && !flushingPendingMessages)
		{
			QueuePendingChat(sourcePlayer, chatText);
			if (sourcePlayer != PlayerControl.LocalPlayer)
			{
				SoundManager.Instance.PlaySound(MessageSound, loop: false).pitch = 0.5f + (float)(int)sourcePlayer.PlayerId / 10f;
			}
			return;
		}
		TryAddChat(sourcePlayer, chatText, allowQueue: true);
	}

	private bool TryAddChat(PlayerControl sourcePlayer, string chatText, bool allowQueue)
	{
		GameData.PlayerInfo data = PlayerControl.LocalPlayer.Data;
		GameData.PlayerInfo data2 = sourcePlayer.Data;
		if (data2 == null || data == null || (data2.IsDead && !data.IsDead))
		{
			return false;
		}
		if (chatBubPool.NotInUse == 0)
		{
			chatBubPool.ReclaimOldest();
		}
		ChatBubble chatBubble = chatBubPool.Get<ChatBubble>();
		try
		{
			chatBubble.transform.SetParent(scroller.Inner);
			chatBubble.transform.localScale = Vector3.one;
			bool num = sourcePlayer == PlayerControl.LocalPlayer;
			if (num)
			{
				chatBubble.SetRight();
			}
			else
			{
				chatBubble.SetLeft();
			}
			bool flag = data.IsImpostor && data2.IsImpostor;
			bool voted = (bool)MeetingHud.Instance && MeetingHud.Instance.DidVote(sourcePlayer.PlayerId);
			chatBubble.SetFaceColor(data2.ColorId);
			chatBubble.SetName(data2.PlayerName, data2.IsDead, voted, flag ? Palette.ImpostorRed : Color.white);
			if (SaveManager.CensorChat)
			{
				chatText = BlockedWords.CensorWords(chatText);
				chatText = BlockedWords.UseTmpFontForCensorMarks(chatText);
			}
			chatBubble.SetText(chatText);
			chatBubble.AlignChildren();
			AlignAllBubbles();
			ShowChatNotificationIfClosed();
			if (!num)
			{
				SoundManager.Instance.PlaySound(MessageSound, loop: false).pitch = 0.5f + (float)(int)sourcePlayer.PlayerId / 10f;
			}
			return true;
		}
		catch (Exception exception)
		{
			Debug.LogException(exception);
			chatBubPool.Reclaim(chatBubble);
			if (allowQueue)
			{
				QueuePendingChat(sourcePlayer, chatText);
			}
			return false;
		}
	}

	private void QueuePendingChat(PlayerControl sourcePlayer, string chatText)
	{
		pendingMessages.Add(new PendingChatMessage(sourcePlayer, chatText));
		while (pendingMessages.Count > MaxChat)
		{
			pendingMessages.RemoveAt(0);
		}
		ShowChatNotificationIfClosed();
	}

	private void FlushPendingMessages()
	{
		if (flushingPendingMessages || pendingMessages.Count == 0)
		{
			return;
		}
		flushingPendingMessages = true;
		List<PendingChatMessage> list = new List<PendingChatMessage>(pendingMessages);
		pendingMessages.Clear();
		for (int i = 0; i < list.Count; i++)
		{
			TryAddChat(list[i].SourcePlayer, list[i].Text, allowQueue: false);
		}
		flushingPendingMessages = false;
	}

	private void AlignAllBubbles()
	{
		float num = 0f;
		List<PoolableBehavior> activeChildren = chatBubPool.activeChildren;
		for (int num2 = activeChildren.Count - 1; num2 >= 0; num2--)
		{
			ChatBubble chatBubble = activeChildren[num2] as ChatBubble;
			num += chatBubble.Background.size.y;
			Vector3 localPosition = chatBubble.transform.localPosition;
			localPosition.y = -1.85f + num;
			chatBubble.transform.localPosition = localPosition;
			num += 0.1f;
		}
		scroller.YBounds.min = Mathf.Min(0f, 0f - num + scroller.HitBox.bounds.size.y);
	}

	private IEnumerator BounceDot()
	{
		ChatNotifyDot.enabled = true;
		yield return Effects.Bounce(ChatNotifyDot.transform);
		notificationRoutine = null;
	}

	private void ShowChatNotificationIfClosed()
	{
		if (IsOpen)
		{
			return;
		}
		ChatNotifyDot.enabled = true;
		if (notificationRoutine != null)
		{
			return;
		}
		if (DestroyableSingleton<HudManager>.InstanceExists)
		{
			notificationRoutine = DestroyableSingleton<HudManager>.Instance.StartCoroutine(BounceDot());
		}
		else if (base.gameObject.activeInHierarchy)
		{
			notificationRoutine = StartCoroutine(BounceDot());
		}
	}

	public void GiveFocus()
	{
		TextArea.GiveFocus();
	}
}
