using System;
using System.Collections;
using Hazel;
using PowerTools;
using UnityEngine;

public class AutoOpenDoor : MonoBehaviour
{
	private const float ClosedDuration = 10f;

	public const float CooldownDuration = 30f;

	public SystemTypes Room;

	public float ClosedTimer;

	public float CooldownTimer;

	public bool Open;

	public BoxCollider2D myCollider;

	public SpriteAnim animator;

	public AnimationClip OpenDoorAnim;

	public AnimationClip CloseDoorAnim;

	public AudioClip OpenSound;

	public AudioClip CloseSound;

	private float size;

	private void Awake()
	{
		Vector2 vector = myCollider.size;
		size = ((vector.x > vector.y) ? vector.y : vector.x);
	}

	public virtual void SetDoorway(bool open)
	{
		if (!open)
		{
			ClosedTimer = 10f;
			CooldownTimer = 30f;
		}
		Open = open;
		myCollider.isTrigger = open;
		animator.Play(open ? OpenDoorAnim : CloseDoorAnim);
		StopAllCoroutines();
		if (!open)
		{
			Vector2 vector = myCollider.size;
			StartCoroutine(CoCloseDoorway(vector.x > vector.y));
			if (Constants.ShouldPlaySfx())
			{
				SoundManager.Instance.PlayDynamicSound(base.name, CloseSound, loop: false, DoorDynamics, playAsSfx: true);
			}
		}
		else if (Constants.ShouldPlaySfx())
		{
			SoundManager.Instance.PlayDynamicSound(base.name, OpenSound, loop: false, DoorDynamics, playAsSfx: true);
		}
	}

	private IEnumerator CoCloseDoorway(bool isHort)
	{
		Vector2 s = myCollider.size;
		float i = 0f;
		if (isHort)
		{
			while (i < 0.1f)
			{
				i += Time.deltaTime;
				s.y = Mathf.Lerp(0.0001f, size, i / 0.1f);
				myCollider.size = s;
				yield return null;
			}
		}
		else
		{
			while (i < 0.1f)
			{
				i += Time.deltaTime;
				s.x = Mathf.Lerp(0.0001f, size, i / 0.1f);
				myCollider.size = s;
				yield return null;
			}
		}
	}

	private void DoorDynamics(AudioSource source, float dt)
	{
		if (!PlayerControl.LocalPlayer)
		{
			source.volume = 0f;
			return;
		}
		Vector2 a = base.transform.position;
		Vector2 truePosition = PlayerControl.LocalPlayer.GetTruePosition();
		float num = Vector2.Distance(a, truePosition);
		if (num > 4f)
		{
			source.volume = 0f;
			return;
		}
		float b = 1f - num / 4f;
		source.volume = Mathf.Lerp(source.volume, b, dt);
	}

	public virtual void Serialize(MessageWriter writer)
	{
		writer.Write(Open);
	}

	public virtual void Deserialize(MessageReader reader)
	{
		SetDoorway(reader.ReadBoolean());
	}

	public bool DoUpdate(float dt)
	{
		CooldownTimer = Math.Max(CooldownTimer - dt, 0f);
		if (ClosedTimer > 0f)
		{
			ClosedTimer = Math.Max(ClosedTimer - dt, 0f);
			if (ClosedTimer == 0f)
			{
				SetDoorway(open: true);
				return true;
			}
		}
		return false;
	}
}
