using UnityEngine;

public class SensorDoor : MonoBehaviour
{
	public SpriteRenderer LeftSide;

	public SpriteRenderer RightSide;

	public Collider2D Sensor;

	public float ActivationDistance = 2f;

	public bool Opening;

	public float OpenDuration;

	public float SlideDistance = 0.65f;

	private float openTimer;

	private ContactFilter2D playerFilter;

	private Collider2D[] playerHits = new Collider2D[8];

	private int playerLayer = -1;

	private Vector3 leftClosedPosition;

	private Vector3 rightClosedPosition;

	private Vector3 leftClosedScale;

	private Vector3 rightClosedScale;

	private bool hasClosedPositions;

	public void OnEnable()
	{
		SetupSensor();
		CaptureClosedPositions();
		InvokeRepeating("CheckDoor", 0.1f, 0.1f);
		if ((bool)LeftSide)
		{
			LeftSide.SetCooldownNormalizedUvs();
		}
		if ((bool)RightSide)
		{
			RightSide.SetCooldownNormalizedUvs();
		}
	}

	private void OnDisable()
	{
		CancelInvoke("CheckDoor");
	}

	private void OnValidate()
	{
		SetupSensor();
	}

	[ContextMenu("Set Right Uvs")]
	public void SetUvs()
	{
		if ((bool)RightSide)
		{
			RightSide.SetCooldownNormalizedUvs();
		}
	}

	private void Update()
	{
		CheckDoor();
		if (Opening && openTimer < OpenDuration)
		{
			openTimer += Time.deltaTime;
			float value = Mathf.SmoothStep(0f, 1f, openTimer / OpenDuration);
			SetDoorPercent(value);
		}
		else if (!Opening && openTimer > 0f)
		{
			openTimer -= Time.deltaTime;
			float value2 = Mathf.SmoothStep(0f, 1f, openTimer / OpenDuration);
			SetDoorPercent(value2);
		}
	}

	private void CheckDoor()
	{
		Opening = HasPlayerInSensor();
	}

	private void SetupSensor()
	{
		if (!(bool)Sensor)
		{
			Sensor = GetComponent<Collider2D>();
		}
		if ((bool)Sensor)
		{
			Sensor.isTrigger = true;
		}
		playerFilter.useLayerMask = true;
		playerFilter.useTriggers = true;
		playerLayer = LayerMask.NameToLayer("Players");
		playerFilter.layerMask = (playerLayer >= 0) ? (1 << playerLayer) : Constants.PlayersOnlyMask;
	}

	private bool HasPlayerInSensor()
	{
		if ((bool)Sensor && Sensor.enabled)
		{
			if (IsPlayerInsideSensor(PlayerControl.LocalPlayer))
			{
				return true;
			}
			for (int i = 0; i < PlayerControl.AllPlayerControls.Count; i++)
			{
				if (IsPlayerInsideSensor(PlayerControl.AllPlayerControls[i]))
				{
					return true;
				}
			}
			int count = Sensor.OverlapCollider(playerFilter, playerHits);
			for (int j = 0; j < count; j++)
			{
				Collider2D collider2D = playerHits[j];
				if ((bool)collider2D && IsPlayerCollider(collider2D))
				{
					return true;
				}
			}
			return false;
		}
		return PhysicsHelpers.CircleContains(base.transform.position, ActivationDistance, LayerMask.GetMask("Players"));
	}

	private bool IsPlayerInsideSensor(PlayerControl player)
	{
		if (!(bool)player || !(bool)Sensor)
		{
			return false;
		}
		if (playerLayer >= 0 && player.gameObject.layer != playerLayer)
		{
			Collider2D playerCollider = player.Collider;
			if (!(bool)playerCollider || playerCollider.gameObject.layer != playerLayer)
			{
				return false;
			}
		}
		Vector2 truePosition = player.GetTruePosition();
		if (Sensor.bounds.Contains(truePosition) || Sensor.OverlapPoint(truePosition))
		{
			return true;
		}
		if ((bool)player.Collider && player.Collider.enabled)
		{
			ColliderDistance2D distance = Sensor.Distance(player.Collider);
			if (distance.isOverlapped || distance.distance <= 0.01f)
			{
				return true;
			}
			if (Sensor.bounds.Intersects(player.Collider.bounds))
			{
				return true;
			}
		}
		return false;
	}

	private bool IsPlayerCollider(Collider2D collider2D)
	{
		if (!(bool)collider2D)
		{
			return false;
		}
		if (playerLayer >= 0 && collider2D.gameObject.layer == playerLayer)
		{
			return true;
		}
		return (bool)collider2D.GetComponent<PlayerControl>() || (bool)collider2D.GetComponentInParent<PlayerControl>();
	}

	private void SetDoorPercent(float value)
	{
		value = Mathf.Clamp01(value);
		if ((bool)LeftSide)
		{
			LeftSide.material.SetFloat("_Percent", value);
			Vector3 localPosition = leftClosedPosition;
			localPosition.x -= SlideDistance * value;
			LeftSide.transform.localPosition = localPosition;
			Vector3 localScale = leftClosedScale;
			localScale.x = leftClosedScale.x * (1f - value);
			LeftSide.transform.localScale = localScale;
		}
		if ((bool)RightSide)
		{
			RightSide.material.SetFloat("_Percent", value);
			Vector3 localPosition2 = rightClosedPosition;
			localPosition2.x += SlideDistance * value;
			RightSide.transform.localPosition = localPosition2;
			Vector3 localScale2 = rightClosedScale;
			localScale2.x = rightClosedScale.x * (1f - value);
			RightSide.transform.localScale = localScale2;
		}
	}

	private void CaptureClosedPositions()
	{
		if (hasClosedPositions)
		{
			return;
		}
		if ((bool)LeftSide)
		{
			leftClosedPosition = LeftSide.transform.localPosition;
			leftClosedScale = LeftSide.transform.localScale;
		}
		if ((bool)RightSide)
		{
			rightClosedPosition = RightSide.transform.localPosition;
			rightClosedScale = RightSide.transform.localScale;
		}
		hasClosedPositions = true;
	}
}
