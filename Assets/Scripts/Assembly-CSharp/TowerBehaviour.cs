using UnityEngine;

public class TowerBehaviour : MonoBehaviour
{
	public float timer;

	public float frameTime = 0.2f;

	public SpriteRenderer circle;

	public SpriteRenderer middle1;

	public SpriteRenderer middle2;

	public SpriteRenderer outer1;

	public SpriteRenderer outer2;

	public void Update()
	{
		timer += Time.deltaTime;
		if (timer < frameTime)
		{
			circle.color = Color.white;
			SpriteRenderer spriteRenderer = middle1;
			SpriteRenderer spriteRenderer2 = middle2;
			SpriteRenderer spriteRenderer3 = outer1;
			Color color = (outer2.color = Color.black);
			Color color2 = (spriteRenderer3.color = color);
			Color color4 = (spriteRenderer2.color = color2);
			spriteRenderer.color = color4;
		}
		else if (timer < 2f * frameTime)
		{
			SpriteRenderer spriteRenderer4 = middle1;
			Color color4 = (middle2.color = Color.white);
			spriteRenderer4.color = color4;
			SpriteRenderer spriteRenderer5 = circle;
			SpriteRenderer spriteRenderer6 = outer1;
			Color color2 = (outer2.color = Color.black);
			color4 = (spriteRenderer6.color = color2);
			spriteRenderer5.color = color4;
		}
		else if (timer < 3f * frameTime)
		{
			SpriteRenderer spriteRenderer7 = outer1;
			Color color4 = (outer2.color = Color.white);
			spriteRenderer7.color = color4;
			SpriteRenderer spriteRenderer8 = middle1;
			SpriteRenderer spriteRenderer9 = middle2;
			Color color2 = (circle.color = Color.black);
			color4 = (spriteRenderer9.color = color2);
			spriteRenderer8.color = color4;
		}
		else
		{
			timer = 0f;
		}
	}
}
