using UnityEngine;

public class AspectSize : MonoBehaviour
{
	public Sprite Background;

	public SpriteRenderer Renderer;

	public float PercentWidth = 0.95f;

	public void OnEnable()
	{
		Camera main = Camera.main;
		float num = main.orthographicSize * main.aspect;
		float num2 = (Background ? Background : Renderer.sprite).bounds.size.x / 2f;
		float num3 = num / num2 * PercentWidth;
		if (num3 < 1f)
		{
			base.transform.localScale = new Vector3(num3, num3, num3);
		}
	}

	public static float CalculateSize(Vector3 parentPos, Sprite sprite)
	{
		Camera main = Camera.main;
		float num = main.orthographicSize * main.aspect + parentPos.x;
		float x = sprite.bounds.size.x;
		float b = num / x * 0.98f;
		return Mathf.Min(1f, b);
	}
}
