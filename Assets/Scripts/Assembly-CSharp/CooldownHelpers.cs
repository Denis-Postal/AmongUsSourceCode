using UnityEngine;

public static class CooldownHelpers
{
	public static void SetCooldownNormalizedUvs(this SpriteRenderer myRend)
	{
		Vector2[] uv = myRend.sprite.uv;
		Vector4 value = new Vector4(2f, -1f, 2f, -1f);
		for (int i = 0; i < uv.Length; i++)
		{
			if (value.x > uv[i].x)
			{
				value.x = uv[i].x;
			}
			if (value.y < uv[i].x)
			{
				value.y = uv[i].x;
			}
			if (value.z > uv[i].y)
			{
				value.z = uv[i].y;
			}
			if (value.w < uv[i].y)
			{
				value.w = uv[i].y;
			}
		}
		value.y -= value.x;
		value.w -= value.z;
		myRend.material.SetVector("_NormalizedUvs", value);
	}
}
