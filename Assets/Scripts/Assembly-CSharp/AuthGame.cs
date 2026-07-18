using System.Collections;
using UnityEngine;

public class AuthGame : Minigame
{
	public TextRenderer TargetText;

	public TextRenderer NumberText;

	public TextRenderer OtherStatusText;

	public int number;

	public string numString = string.Empty;

	private bool animating;

	private HqHudSystemType system;

	public SpriteRenderer OurLight;

	public SpriteRenderer TheirLight;

	public SpriteRenderer TimeBar;

	public AudioClip ButtonSound;

	public AudioClip AcceptSound;

	public AudioClip RejectSound;

	private int OtherConsoleId;

	private bool evenColor;

	public override void Begin(PlayerTask task)
	{
		OtherConsoleId = (base.ConsoleId + 1) % 2;
		base.Begin(task);
		system = ShipStatus.Instance.Systems[SystemTypes.Comms] as HqHudSystemType;
		ShipStatus.Instance.RpcRepairSystem(SystemTypes.Comms, 0x40 | base.ConsoleId);
	}

	public override void Close()
	{
		base.Close();
		ShipStatus.Instance.RpcRepairSystem(SystemTypes.Comms, 0x20 | base.ConsoleId);
	}

	public void Update()
	{
		evenColor = (int)Time.time * 2 % 2 == 0;
		Vector3 localScale = TimeBar.transform.localScale;
		localScale.x = system.PercentActive;
		TimeBar.transform.localScale = localScale;
		if (system.PercentActive < 0.25f)
		{
			TimeBar.color = new Color(1f, 0.45f, 0.25f);
		}
		else if ((double)system.PercentActive < 0.5)
		{
			TimeBar.color = Color.yellow;
		}
		else
		{
			TimeBar.color = Color.white;
		}
		TargetText.Text = system.TargetNumber.ToString("D5");
		if (system.IsConsoleOkay(base.ConsoleId))
		{
			OurLight.color = Color.green;
		}
		else
		{
			OurLight.color = (evenColor ? Color.white : Color.yellow);
		}
		if (amClosing == CloseState.None && !system.IsActive)
		{
			StartCoroutine(CoStartClose());
		}
		if (system.IsConsoleOkay(OtherConsoleId))
		{
			TheirLight.color = Color.green;
			StringNames id = ((OtherConsoleId == 1) ? StringNames.AuthOfficeOkay : StringNames.AuthCommsOkay);
			OtherStatusText.Text = DestroyableSingleton<TranslationController>.Instance.GetString(id);
		}
		else if (system.IsConsoleActive(OtherConsoleId))
		{
			TheirLight.color = (evenColor ? Color.white : Color.yellow);
			StringNames id2 = ((OtherConsoleId == 1) ? StringNames.AuthOfficeActive : StringNames.AuthCommsActive);
			OtherStatusText.Text = DestroyableSingleton<TranslationController>.Instance.GetString(id2);
		}
		else
		{
			TheirLight.color = Color.red;
			StringNames id3 = ((OtherConsoleId == 1) ? StringNames.AuthOfficeNotActive : StringNames.AuthCommsNotActive);
			OtherStatusText.Text = DestroyableSingleton<TranslationController>.Instance.GetString(id3);
		}
	}

	public void ClickNumber(int i)
	{
		if (!animating && NumberText.Text.Length < 5)
		{
			if (Constants.ShouldPlaySfx())
			{
				SoundManager.Instance.PlaySound(ButtonSound, loop: false);
			}
			numString += i;
			number = number * 10 + i;
			NumberText.Text = numString;
		}
	}

	public void ClearEntry()
	{
		if (!animating)
		{
			if (Constants.ShouldPlaySfx())
			{
				SoundManager.Instance.PlaySound(ButtonSound, loop: false);
			}
			number = 0;
			numString = string.Empty;
			NumberText.Text = string.Empty;
		}
	}

	public void Enter()
	{
		if (!animating)
		{
			StartCoroutine(Animate());
		}
	}

	private IEnumerator Animate()
	{
		animating = true;
		WaitForSeconds wait = new WaitForSeconds(0.1f);
		yield return wait;
		NumberText.Text = string.Empty;
		yield return wait;
		if (system.TargetNumber == number)
		{
			if (Constants.ShouldPlaySfx())
			{
				SoundManager.Instance.PlaySound(AcceptSound, loop: false);
			}
			ShipStatus.Instance.RpcRepairSystem(SystemTypes.Comms, 0x10 | base.ConsoleId);
			try
			{
				((SabotageTask)MyTask).MarkContributed();
			}
			catch
			{
			}
			NumberText.Text = "OK";
			yield return wait;
			NumberText.Text = string.Empty;
			yield return wait;
			NumberText.Text = "OK";
			yield return wait;
			NumberText.Text = string.Empty;
			yield return wait;
			NumberText.Text = "OK";
		}
		else
		{
			if (Constants.ShouldPlaySfx())
			{
				SoundManager.Instance.PlaySound(RejectSound, loop: false);
			}
			NumberText.Text = "Bad";
			yield return wait;
			NumberText.Text = string.Empty;
			yield return wait;
			NumberText.Text = "Bad";
			yield return wait;
			numString = string.Empty;
			number = 0;
			NumberText.Text = numString;
		}
		number = 0;
		numString = string.Empty;
		NumberText.Text = string.Empty;
		animating = false;
	}
}
