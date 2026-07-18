using System.Collections.Generic;
using Hazel;

public class SecurityCameraSystemType : ISystemType
{
	public const byte IncrementOp = 1;

	public const byte DecrementOp = 2;

	private HashSet<byte> PlayersUsing = new HashSet<byte>();

	public bool InUse => PlayersUsing.Count > 0;

	public bool Detoriorate(float deltaTime)
	{
		return false;
	}

	public void RepairDamage(PlayerControl player, byte amount)
	{
		if (amount == 1)
		{
			PlayersUsing.Add(player.PlayerId);
		}
		else
		{
			PlayersUsing.Remove(player.PlayerId);
		}
		UpdateCameras();
	}

	private void UpdateCameras()
	{
		for (int i = 0; i < ShipStatus.Instance.AllRooms.Length; i++)
		{
			ShipRoom shipRoom = ShipStatus.Instance.AllRooms[i];
			if ((bool)shipRoom.survCamera)
			{
				if (InUse)
				{
					shipRoom.survCamera.Image.Play(shipRoom.survCamera.OnAnim);
				}
				else
				{
					shipRoom.survCamera.Image.Play(shipRoom.survCamera.OffAnim);
				}
			}
		}
	}

	public void Serialize(MessageWriter writer, bool initialState)
	{
		writer.WritePacked(PlayersUsing.Count);
		foreach (byte item in PlayersUsing)
		{
			writer.Write(item);
		}
	}

	public void Deserialize(MessageReader reader, bool initialState)
	{
		PlayersUsing.Clear();
		int num = reader.ReadPackedInt32();
		for (int i = 0; i < num; i++)
		{
			PlayersUsing.Add(reader.ReadByte());
		}
		UpdateCameras();
	}
}
