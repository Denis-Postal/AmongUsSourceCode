using System;
using System.Collections.Generic;
using System.Linq;
using Hazel;

internal class HqHudSystemType : ISystemType, IActivatable
{
	public enum Tags
	{
		DamageBit = 128,
		ActiveBit = 64,
		DeactiveBit = 32,
		FixBit = 16
	}

	public const byte TagMask = 240;

	public const byte IdMask = 15;

	private HashSet<Tuple<byte, byte>> ActiveConsoles = new HashSet<Tuple<byte, byte>>();

	private HashSet<byte> CompletedConsoles = new HashSet<byte>();

	private const float ActiveTime = 10f;

	private float Timer;

	public int TargetNumber;

	public bool IsActive => CompletedConsoles.Count < 2;

	public float NumComplete => CompletedConsoles.Count;

	public float PercentActive => Timer / 10f;

	public HqHudSystemType()
	{
		CompletedConsoles.Add(0);
		CompletedConsoles.Add(1);
	}

	public bool Detoriorate(float deltaTime)
	{
		if (IsActive)
		{
			Timer -= deltaTime;
			if (Timer <= 0f)
			{
				TargetNumber = IntRange.Next(0, 99999);
				Timer = 10f;
				CompletedConsoles.Clear();
			}
			if (!PlayerTask.PlayerHasTaskOfType<IHudOverrideTask>(PlayerControl.LocalPlayer))
			{
				PlayerControl.LocalPlayer.AddSystemTask(SystemTypes.Comms);
			}
		}
		return false;
	}

	internal bool IsConsoleActive(int consoleId)
	{
		return ActiveConsoles.Any((Tuple<byte, byte> s) => s.Item2 == (byte)consoleId);
	}

	internal bool IsConsoleOkay(int consoleId)
	{
		return CompletedConsoles.Contains((byte)consoleId);
	}

	public void RepairDamage(PlayerControl player, byte amount)
	{
		byte b = (byte)(amount & 0xF);
		switch ((Tags)(amount & 0xF0))
		{
		case Tags.DamageBit:
			Timer = -1f;
			CompletedConsoles.Clear();
			ActiveConsoles.Clear();
			break;
		case Tags.ActiveBit:
			ActiveConsoles.Add(new Tuple<byte, byte>(player.PlayerId, b));
			break;
		case Tags.DeactiveBit:
			ActiveConsoles.Remove(new Tuple<byte, byte>(player.PlayerId, b));
			break;
		case Tags.FixBit:
			Timer = 10f;
			CompletedConsoles.Add(b);
			break;
		}
	}

	public void Serialize(MessageWriter writer, bool initialState)
	{
		writer.WritePacked(ActiveConsoles.Count);
		foreach (Tuple<byte, byte> activeConsole in ActiveConsoles)
		{
			writer.Write(activeConsole.Item1);
			writer.Write(activeConsole.Item2);
		}
		writer.WritePacked(CompletedConsoles.Count);
		foreach (byte completedConsole in CompletedConsoles)
		{
			writer.Write(completedConsole);
		}
	}

	public void Deserialize(MessageReader reader, bool initialState)
	{
		int num = reader.ReadPackedInt32();
		ActiveConsoles.Clear();
		for (int i = 0; i < num; i++)
		{
			ActiveConsoles.Add(new Tuple<byte, byte>(reader.ReadByte(), reader.ReadByte()));
		}
		int num2 = reader.ReadPackedInt32();
		CompletedConsoles.Clear();
		for (int j = 0; j < num2; j++)
		{
			CompletedConsoles.Add(reader.ReadByte());
		}
	}
}
