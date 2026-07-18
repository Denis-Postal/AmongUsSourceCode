using UnityEngine;

public class DemoKeyboardStick : VirtualJoystick
{
	public SpriteRenderer UpKey;

	public SpriteRenderer DownKey;

	public SpriteRenderer LeftKey;

	public SpriteRenderer RightKey;

	private void Update()
	{
		UpKey.enabled = Delta.y > 0.1f;
		DownKey.enabled = Delta.y < -0.1f;
		RightKey.enabled = Delta.x > 0.1f;
		LeftKey.enabled = Delta.x < -0.1f;
	}
}
