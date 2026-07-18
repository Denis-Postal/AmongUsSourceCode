using UnityEngine;

public class ParallaxController : MonoBehaviour
{
	public ParallaxChild[] Children;

	public float Scale = 1f;

	public void Start()
	{
		Children = GetComponentsInChildren<ParallaxChild>();
	}

	public void SetParallax(float x)
	{
		for (int i = 0; i < Children.Length; i++)
		{
			ParallaxChild obj = Children[i];
			Vector3 basePosition = obj.BasePosition;
			_ = Scale;
			if (basePosition.z >= 0f)
			{
				basePosition.x += x / (basePosition.z * Scale + 1f);
			}
			else
			{
				basePosition.x += x * ((0f - basePosition.z) * Scale + 1f);
			}
			obj.transform.localPosition = basePosition;
		}
	}
}
