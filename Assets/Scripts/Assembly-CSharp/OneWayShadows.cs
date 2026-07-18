using UnityEngine;

public class OneWayShadows : MonoBehaviour
{
	public Collider2D RoomCollider;

	public void OnEnable()
	{
		LightSource.OneWayShadows.Add(base.gameObject, this);
	}

	public void OnDisable()
	{
		LightSource.OneWayShadows.Remove(base.gameObject);
	}

	public bool IsIgnored(LightSource lightSource)
	{
		return RoomCollider.OverlapPoint(lightSource.transform.position);
	}
}
