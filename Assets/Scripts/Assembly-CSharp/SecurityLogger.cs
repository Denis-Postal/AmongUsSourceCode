using System.Collections;
using System.Linq;
using UnityEngine;

public class SecurityLogger : MonoBehaviour
{
	private static Collider2D[] hits = new Collider2D[10];

	public SecurityLogBehaviour LogParent;

	public SecurityLogBehaviour.SecurityLogLocations MyLocation;

	public float Cooldown = 5f;

	public SpriteRenderer Image;

	public BoxCollider2D Sensor;

	private float[] Timers = new float[10];

	private ContactFilter2D filter;

	private void Awake()
	{
		filter = default(ContactFilter2D);
		filter.useLayerMask = true;
		filter.layerMask = Constants.PlayersOnlyMask;
	}

	public void FixedUpdate()
	{
		for (int i = 0; i < Timers.Length; i++)
		{
			Timers[i] -= Time.deltaTime;
		}
		int num = Sensor.OverlapCollider(filter, hits);
		int i2 = 0;
		while (i2 < num)
		{
			PlayerControl playerControl = PlayerControl.AllPlayerControls.FirstOrDefault((PlayerControl p) => p.Collider == hits[i2]);
			if ((bool)playerControl && playerControl.Data != null && !playerControl.Data.IsDead && Timers[playerControl.PlayerId] < 0f)
			{
				Timers[playerControl.PlayerId] = Cooldown;
				LogParent.LogPlayer(playerControl, MyLocation);
				StopAllCoroutines();
				StartCoroutine(BlinkSensor());
			}
			int num2 = i2 + 1;
			i2 = num2;
		}
	}

	private IEnumerator BlinkSensor()
	{
		yield return Effects.Wait(0.1f);
		Image.color = LogParent.BarColors[(byte)MyLocation];
		yield return Effects.Wait(0.1f);
		Image.color = new Color(1f, 1f, 1f, 0.5f);
	}
}
