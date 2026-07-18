using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class DecontamNumController : MonoBehaviour
{
	public SpriteRenderer[] Images;

	public Sprite[] NumImages;

	[ContextMenu("Space Evenly")]
	public void SpaceEvenly()
	{
		List<float> list = FloatRange.SpreadToEdges(-2.9f, 2.9f, Images.Length).ToList();
		for (int i = 0; i < Images.Length; i++)
		{
			Images[i].transform.localPosition = new Vector3(0f, list[i], 0.1f);
		}
	}

	internal void SetSecond(float curSecond, float maxSecond)
	{
		for (int i = 0; i < Images.Length; i++)
		{
			int num = Mathf.CeilToInt(Mathf.Lerp(0f, NumImages.Length - 1, curSecond / maxSecond));
			Images[i].sprite = NumImages[num];
		}
	}
}
