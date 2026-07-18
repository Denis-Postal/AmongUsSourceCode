using System.Linq;
using System.Text;
using UnityEngine;

public class ElectricTask : SabotageTask
{
	private bool isComplete;

	private SwitchSystem system;

	private bool even;

	public override int TaskStep
	{
		get
		{
			if (!isComplete)
			{
				return 0;
			}
			return 1;
		}
	}

	public override bool IsComplete => isComplete;

	public override void Initialize()
	{
		ShipStatus instance = ShipStatus.Instance;
		system = (SwitchSystem)instance.Systems[SystemTypes.Electrical];
		SetupArrows();
	}

	private void FixedUpdate()
	{
		if (!isComplete && system.ExpectedSwitches == system.ActualSwitches)
		{
			Complete();
		}
	}

	public override bool ValidConsole(Console console)
	{
		return console.TaskTypes.Contains(TaskTypes.FixLights);
	}

	public override void Complete()
	{
		isComplete = true;
		PlayerControl.LocalPlayer.RemoveTask(this);
		if (didContribute)
		{
			StatsManager.Instance.SabsFixed++;
		}
	}

	public override void AppendTaskText(StringBuilder sb)
	{
		even = !even;
		Color color = (even ? Color.yellow : Color.red);
		sb.Append(color.ToTextColor());
		sb.Append(DestroyableSingleton<TranslationController>.Instance.GetString(TaskTypes.FixLights));
		sb.AppendLine(" (%" + (int)(system.Level * 100f) + ")[]");
		for (int i = 0; i < Arrows.Length; i++)
		{
			Arrows[i].image.color = color;
		}
	}
}
