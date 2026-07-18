using UnityEngine;

public class PoolablePlayer : MonoBehaviour
{
	public SpriteRenderer Body;

	public SpriteRenderer[] Hands;

	public SpriteRenderer HatSlot;

	public SpriteRenderer SkinSlot;

	public SpriteRenderer PetSlot;

	public TextRenderer NameText;

	public void SetFlipX(bool flipped)
	{
		Body.flipX = flipped;
		SkinSlot.flipX = !flipped;
		PetSlot.flipX = flipped;
		HatSlot.flipX = !flipped;
		if (flipped)
		{
			Vector3 localPosition = HatSlot.transform.localPosition;
			localPosition.x = 0f - localPosition.x;
			HatSlot.transform.localPosition = localPosition;
		}
	}

	public void SetDeadFlipX(bool flipped)
	{
		Body.flipX = flipped;
		PetSlot.flipX = flipped;
		HatSlot.flipX = flipped;
		if (flipped)
		{
			Vector3 localPosition = HatSlot.transform.localPosition;
			localPosition.x = 0f - localPosition.x;
			localPosition.y = 0.725f;
			HatSlot.transform.localPosition = localPosition;
		}
	}
}
