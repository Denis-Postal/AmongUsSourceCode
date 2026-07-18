using UnityEngine;

public class ParallaxChild : MonoBehaviour
{
	[HideInInspector]
	public Vector3 BasePosition;

	public void Awake()
	{
		BasePosition = base.transform.localPosition;
	}
}
