using System.Collections.Generic;
using System.Linq;
using Hazel;

public class SabotageSystemType : ISystemType
{
	public class DummySab : IActivatable
	{
		public float timer;

		public bool IsActive => timer > 0f;
	}

	public const float SpecialSabDelay = 30f;

	private List<IActivatable> specials;

	private bool dirty;

	private DummySab dummy = new DummySab();

	public float Timer { get; set; }

	public float PercentCool => Timer / 30f;

	public bool AnyActive => specials.Any((IActivatable s) => s.IsActive);

	public SabotageSystemType(IActivatable[] specials)
	{
		this.specials = new List<IActivatable>(specials);
		this.specials.Add(dummy);
	}

	public bool Detoriorate(float deltaTime)
	{
		dummy.timer -= deltaTime;
		if (Timer > 0f && !AnyActive)
		{
			Timer -= deltaTime;
			if (Timer <= 0f)
			{
				return true;
			}
		}
		return dirty;
	}

	public void ForceSabTime(float t)
	{
		dummy.timer = t;
	}

	public void RepairDamage(PlayerControl player, byte amount)
	{
		dirty = true;
		if (Timer > 0f || (bool)MeetingHud.Instance)
		{
			return;
		}
		if (AmongUsClient.Instance.AmHost)
		{
			switch ((SystemTypes)amount)
			{
			case SystemTypes.Reactor:
				ShipStatus.Instance.RepairSystem(SystemTypes.Reactor, player, 128);
				break;
			case SystemTypes.LifeSupp:
				ShipStatus.Instance.RepairSystem(SystemTypes.LifeSupp, player, 128);
				break;
			case SystemTypes.Comms:
				ShipStatus.Instance.RepairSystem(SystemTypes.Comms, player, 128);
				break;
			case SystemTypes.Electrical:
			{
				byte b = 4;
				for (int i = 0; i < 5; i++)
				{
					if (BoolRange.Next())
					{
						b |= (byte)(1 << i);
					}
				}
				ShipStatus.Instance.RpcRepairSystem(SystemTypes.Electrical, (byte)(b | 0x80));
				break;
			}
			}
		}
		Timer = 30f;
	}

	public void Serialize(MessageWriter writer, bool initialState)
	{
		writer.Write(Timer);
		if (!initialState)
		{
			dirty = false;
		}
	}

	public void Deserialize(MessageReader reader, bool initialState)
	{
		Timer = reader.ReadSingle();
	}
}
