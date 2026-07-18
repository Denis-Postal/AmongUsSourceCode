using System.Linq;
using System.Text;
using UnityEngine;

public class ReactorTask : SabotageTask
{
	private bool isComplete;

	private ReactorSystemType reactor;

	private bool even;

	public override int TaskStep => reactor.UserCount;

	public override bool IsComplete => isComplete;

	public override void Initialize()
	{
		ShipStatus instance = ShipStatus.Instance;
		reactor = (ReactorSystemType)instance.Systems[SystemTypes.Reactor];
		DestroyableSingleton<HudManager>.Instance.StartReactorFlash();
		ReactorShipRoom reactorShipRoom = instance.AllRooms.FirstOrDefault((ShipRoom r) => r.RoomId == SystemTypes.Reactor) as ReactorShipRoom;
		if ((bool)reactorShipRoom)
		{
			reactorShipRoom.StartMeltdown();
		}
		SetupArrows();
	}

	private void FixedUpdate()
	{
		if (!isComplete && !reactor.IsActive)
		{
			Complete();
		}
	}

	public override bool ValidConsole(Console console)
	{
		return console.TaskTypes.Contains(TaskTypes.ResetReactor);
	}

	public override void OnRemove()
	{
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
		sb.Append(DestroyableSingleton<TranslationController>.Instance.GetString(TaskTypes.ResetReactor));
		sb.Append(" ");
		sb.Append((int)reactor.Countdown);
		sb.AppendLine($" ({reactor.UserCount}/{(byte)2})[]");
		for (int i = 0; i < Arrows.Length; i++)
		{
			Arrows[i].image.color = color;
		}
	}
}
