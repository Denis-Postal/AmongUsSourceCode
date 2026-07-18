using System.Collections;
using UnityEngine;

public class TextMarquee : MonoBehaviour
{
	public TextRenderer Target;

	private string targetText;

	public float ScrollSpeed = 1f;

	public float PauseTime = 1f;

	public float AreaWidth = 3f;

	public void Start()
	{
		StartCoroutine(Run());
	}

	private IEnumerator Run()
	{
		yield return null;
		Target.render.material.SetInt("_Mask", 4);
		int i = 0;
		while (i < 1000)
		{
			Vector4 temp = default(Vector4);
			targetText = Target.Text;
			Target.render.material.SetVector("_Offset", temp);
			for (float timer = 0f; timer < PauseTime; timer += Time.deltaTime)
			{
				if (targetText != Target.Text)
				{
					break;
				}
				yield return null;
			}
			for (float timer = 0f; timer < 100f; timer += Time.deltaTime)
			{
				if (targetText != Target.Text)
				{
					break;
				}
				temp.x -= ScrollSpeed * Time.deltaTime;
				Target.render.material.SetVector("_Offset", temp);
				if (Target.Width + temp.x < AreaWidth)
				{
					break;
				}
				yield return null;
			}
			for (float timer = 0f; timer < PauseTime; timer += Time.deltaTime)
			{
				if (targetText != Target.Text)
				{
					break;
				}
				yield return null;
			}
			yield return null;
			int num = i + 1;
			i = num;
		}
	}
}
