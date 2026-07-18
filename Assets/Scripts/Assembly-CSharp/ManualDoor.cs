using System.Collections;
using Hazel;
using UnityEngine;

public class ManualDoor : MonoBehaviour
{
	public bool Opening;

	public BoxCollider2D myCollider;

	public SpriteRenderer image;

	private float size;

	public float OpenDuration = 0.3f;

	public float SlideDistance = 0.85f;

	public bool SlideDown = true;

	private float openTimer;

	public AudioClip OpenSound;

	public AudioClip CloseSound;

	private Vector3 closedPosition;

	private Vector3 closedScale;

	private bool hasClosedTransform;

	private void Awake()
	{
		Vector2 vector = myCollider.size;
		size = ((vector.x > vector.y) ? vector.y : vector.x);
		image.SetCooldownNormalizedUvs();
		CaptureClosedTransform();
	}

	private void Update()
	{
		if (Opening && openTimer < OpenDuration)
		{
			openTimer += Time.deltaTime;
			float value = Mathf.SmoothStep(0f, 1f, openTimer / OpenDuration);
			image.material.SetFloat("_PercentY", value);
			SetDoorVisual(value);
		}
		else if (!Opening && openTimer > 0f)
		{
			openTimer -= Time.deltaTime;
			float value2 = Mathf.SmoothStep(0f, 1f, openTimer / OpenDuration);
			image.material.SetFloat("_PercentY", value2);
			SetDoorVisual(value2);
		}
	}

	public virtual void SetDoorway(bool open)
	{
		if (Opening == open)
		{
			return;
		}
		Opening = open;
		myCollider.isTrigger = open;
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

	public virtual void Serialize(MessageWriter writer)
	{
		writer.Write(Opening);
	}

	public virtual void Deserialize(MessageReader reader)
	{
		SetDoorway(reader.ReadBoolean());
	}

	private void CaptureClosedTransform()
	{
		if (hasClosedTransform || !(bool)image)
		{
			return;
		}
		closedPosition = image.transform.localPosition;
		closedScale = image.transform.localScale;
		hasClosedTransform = true;
	}

	private void SetDoorVisual(float value)
	{
		if (!(bool)image)
		{
			return;
		}
		CaptureClosedTransform();
		value = Mathf.Clamp01(value);
		Vector3 localPosition = closedPosition;
		localPosition.y += (SlideDown ? -SlideDistance : SlideDistance) * value;
		image.transform.localPosition = localPosition;
		Vector3 localScale = closedScale;
		localScale.y = closedScale.y * (1f - value);
		image.transform.localScale = localScale;
	}
}
