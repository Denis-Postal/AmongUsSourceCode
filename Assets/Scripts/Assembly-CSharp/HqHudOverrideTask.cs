using System.Linq;
using System.Text;
using UnityEngine;

public class HqHudOverrideTask : SabotageTask, IHudOverrideTask
{
	private bool isComplete;

	private HqHudSystemType system;

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
		system = instance.Systems[SystemTypes.Comms] as HqHudSystemType;
		SetupArrows();
	}

	private void FixedUpdate()
	{
		if (!isComplete && !system.IsActive)
		{
			Complete();
		}
	}

	public override bool ValidConsole(Console console)
	{
		return console.TaskTypes.Contains(TaskTypes.FixComms);
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
		sb.Append(DestroyableSingleton<TranslationController>.Instance.GetString(TaskTypes.FixComms));
		sb.Append($" ({system.NumComplete}/2)");
		sb.Append("[]");
		for (int i = 0; i < Arrows.Length; i++)
		{
			Arrows[i].image.color = color;
		}
	}
}
