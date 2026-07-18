using System;
using System.Collections;
using Hazel;
using UnityEngine;

public class DeconSystem : MonoBehaviour, ISystemType
{
	[Flags]
	public enum States : byte
	{
		Idle = 0,
		Enter = 1,
		Closed = 2,
		Exit = 4,
		HeadingUp = 8
	}

	private const byte HeadUpCmd = 1;

	private const byte HeadDownCmd = 2;

	private const byte HeadUpInsideCmd = 3;

	private const byte HeadDownInsideCmd = 4;

	public ManualDoor UpperDoor;

	public ManualDoor LowerDoor;

	public float DoorOpenTime = 5f;

	public float DeconTime = 5f;

	public AudioClip SpraySound;

	public ParticleSystem[] Particles;

	private float timer;

	public DecontamNumController FloorText;

	private Coroutine sprayers;

	public States CurState { get; private set; }

	public bool Detoriorate(float dt)
	{
		if (sprayers == null && CurState.HasFlag(States.Closed))
		{
			sprayers = StartCoroutine(CoRunSprayers());
		}
		int num = Mathf.CeilToInt(timer);
		timer = Mathf.Max(0f, timer - dt);
		int num2 = Mathf.CeilToInt(timer);
		if (num != num2)
		{
			if (num2 == 0)
			{
				if (CurState.HasFlag(States.Enter))
				{
					CurState = (CurState & ~States.Enter) | States.Closed;
					timer = DeconTime;
				}
				else if (CurState.HasFlag(States.Closed))
				{
					CurState = (CurState & ~States.Closed) | States.Exit;
					timer = DoorOpenTime;
				}
				else if (CurState.HasFlag(States.Exit))
				{
					CurState = States.Idle;
				}
			}
			UpdateDoorsViaState();
			return true;
		}
		return false;
	}

	private IEnumerator CoRunSprayers()
	{
		if (Constants.ShouldPlaySfx())
		{
			SoundManager.Instance.PlayDynamicSound("DeconSpray", SpraySound, loop: false, SoundDynamics, playAsSfx: true);
		}
		Particles.ForEach(delegate(ParticleSystem p)
		{
			p.Play();
		});
		yield return Effects.Wait(DeconTime);
		sprayers = null;
	}

	private void SoundDynamics(AudioSource source, float dt)
	{
		if (!PlayerControl.LocalPlayer)
		{
			source.volume = 0f;
			return;
		}
		Vector2 truePosition = PlayerControl.LocalPlayer.GetTruePosition();
		if (ShipStatus.Instance.FastRooms.TryGetValue(SystemTypes.Decontamination, out var value))
		{
			if (value.roomArea.OverlapPoint(truePosition))
			{
				float num = timer / DeconTime;
				source.volume = 1f - Mathf.Lerp(0f, 1f, (num - 0.75f) * 4f);
			}
			else
			{
				source.volume = 0f;
			}
		}
		else
		{
			source.volume = 0f;
		}
	}

	public void OpenDoor(bool upper)
	{
		if (CurState == States.Idle)
		{
			ShipStatus.Instance.RpcRepairSystem(SystemTypes.Decontamination, (!upper) ? 1 : 2);
		}
	}

	public void OpenFromInside(bool upper)
	{
		if (CurState == States.Idle)
		{
			ShipStatus.Instance.RpcRepairSystem(SystemTypes.Decontamination, upper ? 3 : 4);
		}
	}

	public void RepairDamage(PlayerControl player, byte amount)
	{
		if (CurState == States.Idle)
		{
			switch (amount)
			{
			case 1:
				CurState = States.Enter | States.HeadingUp;
				timer = DoorOpenTime;
				break;
			case 2:
				CurState = States.Enter;
				timer = DoorOpenTime;
				break;
			case 3:
				CurState = States.Exit | States.HeadingUp;
				timer = DoorOpenTime;
				break;
			case 4:
				CurState = States.Exit;
				timer = DoorOpenTime;
				break;
			}
			UpdateDoorsViaState();
		}
	}

	public void Serialize(MessageWriter writer, bool initialState)
	{
		writer.Write((byte)Mathf.CeilToInt(timer));
		writer.Write((byte)CurState);
	}

	public void Deserialize(MessageReader reader, bool initialState)
	{
		timer = (int)reader.ReadByte();
		CurState = (States)reader.ReadByte();
		UpdateDoorsViaState();
	}

	private void UpdateDoorsViaState()
	{
		int num = Mathf.CeilToInt(timer);
		if (CurState.HasFlag(States.Enter))
		{
			bool flag = CurState.HasFlag(States.HeadingUp);
			LowerDoor.SetDoorway(flag);
			UpperDoor.SetDoorway(!flag);
			FloorText.SetSecond(num, DoorOpenTime);
		}
		else if (CurState.HasFlag(States.Closed) || CurState == States.Idle)
		{
			LowerDoor.SetDoorway(open: false);
			UpperDoor.SetDoorway(open: false);
			FloorText.SetSecond(DeconTime - (float)num, DeconTime);
		}
		else if (CurState.HasFlag(States.Exit))
		{
			bool flag2 = CurState.HasFlag(States.HeadingUp);
			LowerDoor.SetDoorway(!flag2);
			UpperDoor.SetDoorway(flag2);
			FloorText.SetSecond(num, DoorOpenTime);
		}
		else
		{
			Debug.LogWarning("What is this state: " + CurState);
		}
	}
}
