using System.Collections;
using UnityEngine;

public class DiagnosticGame : Minigame
{
	public VerticalGauge Gauge;

	public SpriteRenderer StartButton;

	public float TimePerStep = 90f;

	public TextRenderer Text;

	private int TargetNum = -1;

	public SpriteRenderer[] Targets;

	private Color goodBarColor = new Color32(100, 193, byte.MaxValue, byte.MaxValue);

	public AudioClip StartSound;

	public AudioClip CorrectSound;

	public AudioClip TickSound;

	private int lastPercent;

	public override void Begin(PlayerTask task)
	{
		base.Begin(task);
		if (MyNormTask.TimerStarted == NormalPlayerTask.TimerState.NotStarted)
		{
			StartCoroutine(BlinkButton());
		}
	}

	private IEnumerator BlinkButton()
	{
		while (true)
		{
			StartButton.color = Color.red;
			yield return Effects.Wait(0.5f);
			StartButton.color = Color.white;
			yield return Effects.Wait(0.5f);
		}
	}

	public void PickAnomaly(int num)
	{
		if (amClosing == CloseState.None && MyNormTask.TimerStarted == NormalPlayerTask.TimerState.Finished && num == TargetNum)
		{
			if (Constants.ShouldPlaySfx())
			{
				SoundManager.Instance.PlaySound(CorrectSound, loop: false);
			}
			Targets[TargetNum].color = goodBarColor;
			MyNormTask.NextStep();
			StartCoroutine(CoStartClose());
		}
	}

	public void StartDiagnostic()
	{
		if (MyNormTask.TimerStarted == NormalPlayerTask.TimerState.NotStarted)
		{
			StartButton.GetComponent<PassiveButton>().enabled = false;
			StopAllCoroutines();
			StartButton.color = Color.white;
			if (Constants.ShouldPlaySfx())
			{
				SoundManager.Instance.PlaySound(StartSound, loop: false);
			}
			MyNormTask.TaskTimer = TimePerStep;
			MyNormTask.TimerStarted = NormalPlayerTask.TimerState.Started;
		}
	}

	public void Update()
	{
		switch (MyNormTask.TimerStarted)
		{
		case NormalPlayerTask.TimerState.Started:
		{
			Gauge.gameObject.SetActive(value: true);
			Gauge.MaxValue = TimePerStep;
			Gauge.value = MyNormTask.TaskTimer;
			int num = (int)(100f * MyNormTask.TaskTimer / TimePerStep);
			if (num != lastPercent && Constants.ShouldPlaySfx())
			{
				lastPercent = num;
				SoundManager.Instance.PlaySound(TickSound, loop: false, 0.8f);
			}
			Text.Text = num + "%";
			Targets.ForEach(delegate(SpriteRenderer f)
			{
				f.gameObject.SetActive(value: false);
			});
			break;
		}
		case NormalPlayerTask.TimerState.NotStarted:
			Gauge.gameObject.SetActive(value: false);
			Targets.ForEach(delegate(SpriteRenderer f)
			{
				f.gameObject.SetActive(value: false);
			});
			break;
		case NormalPlayerTask.TimerState.Finished:
			Gauge.gameObject.SetActive(value: true);
			Gauge.MaxValue = 1f;
			Gauge.value = 1f;
			if (TargetNum == -1)
			{
				Text.Text = DestroyableSingleton<TranslationController>.Instance.GetString(StringNames.PickAnomaly);
				Targets.ForEach(delegate(SpriteRenderer f)
				{
					f.gameObject.SetActive(value: true);
					f.color = goodBarColor;
				});
				TargetNum = Targets.RandomIdx();
				Targets[TargetNum].color = Color.red;
			}
			break;
		}
	}
}
