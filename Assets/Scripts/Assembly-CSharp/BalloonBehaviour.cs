using UnityEngine;

public class BalloonBehaviour : MonoBehaviour
{
	public Vector2 Origin;

	public float PeriodX = 4f;

	public float PeriodY = 4f;

	public float MagnitudeX = 3f;

	public float MagnitudeY = 3f;

	public void Update()
	{
		base.transform.localPosition = Origin + new Vector2(Mathf.PerlinNoise(Time.time * PeriodX, 1f) * MagnitudeX, Mathf.Sin(Time.time * PeriodY) * MagnitudeY);
	}
}
