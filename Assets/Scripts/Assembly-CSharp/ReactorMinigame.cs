using UnityEngine;

public class ReactorMinigame : Minigame
{
	private Color bad = new Color(1f, 0.16078432f, 0f);

	private Color good = new Color(0.3019608f, 0.8862745f, 71f / 85f);

	private ReactorSystemType reactor;

	public TextRenderer statusText;

	public SpriteRenderer hand;

	private FloatRange YSweep = new FloatRange(-2.15f, 1.56f);

	public SpriteRenderer sweeper;

	public AudioClip HandSound;

	private bool isButtonDown;

	public override void Begin(PlayerTask task)
	{
		base.Begin(task);
		reactor = ShipStatus.Instance.Systems[SystemTypes.Reactor] as ReactorSystemType;
		hand.color = bad;
	}

	public void ButtonDown()
	{
		if (!reactor.IsActive)
		{
			return;
		}
		isButtonDown = !isButtonDown;
		if (isButtonDown)
		{
			if (Constants.ShouldPlaySfx())
			{
				SoundManager.Instance.PlaySound(HandSound, loop: true);
			}
			ShipStatus.Instance.RpcRepairSystem(SystemTypes.Reactor, (byte)(0x40 | base.ConsoleId));
		}
		else
		{
			SoundManager.Instance.StopSound(HandSound);
			ShipStatus.Instance.RpcRepairSystem(SystemTypes.Reactor, (byte)(0x20 | base.ConsoleId));
		}
		try
		{
			((SabotageTask)MyTask).MarkContributed();
		}
		catch
		{
		}
	}

	public void FixedUpdate()
	{
		if (!reactor.IsActive)
		{
			if (amClosing == CloseState.None)
			{
				hand.color = good;
				statusText.Text = DestroyableSingleton<TranslationController>.Instance.GetString(StringNames.ReactorNominal);
				sweeper.enabled = false;
				SoundManager.Instance.StopSound(HandSound);
				StartCoroutine(CoStartClose());
			}
		}
		else if (!isButtonDown)
		{
			statusText.Text = DestroyableSingleton<TranslationController>.Instance.GetString(StringNames.ReactorHoldToStop);
			sweeper.enabled = false;
		}
		else
		{
			statusText.Text = DestroyableSingleton<TranslationController>.Instance.GetString(StringNames.ReactorWaiting);
			Vector3 localPosition = sweeper.transform.localPosition;
			localPosition.y = YSweep.Lerp(Mathf.Sin(Time.time) * 0.5f + 0.5f);
			sweeper.transform.localPosition = localPosition;
			sweeper.enabled = true;
		}
	}

	public override void Close()
	{
		SoundManager.Instance.StopSound(HandSound);
		if ((bool)ShipStatus.Instance)
		{
			ShipStatus.Instance.RpcRepairSystem(SystemTypes.Reactor, (byte)(0x20 | base.ConsoleId));
		}
		base.Close();
	}
}
