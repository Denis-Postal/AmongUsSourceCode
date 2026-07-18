using System;
using UnityEngine;

public class AlphaPulse : MonoBehaviour
{
	public float Offset = 1f;

	public float Duration = 2.5f;

	private SpriteRenderer rend;

	private MeshRenderer mesh;

	public FloatRange AlphaRange = new FloatRange(0.2f, 0.5f);

	public Color baseColor = Color.white;

	public void SetColor(Color c)
	{
		Start();
		baseColor = c;
		Update();
	}

	private void Start()
	{
		mesh = GetComponent<MeshRenderer>();
		rend = GetComponent<SpriteRenderer>();
	}

	public void Update()
	{
		float v = Mathf.Abs(Mathf.Cos((Offset + Time.time) * (float)Math.PI / Duration));
		if ((bool)rend)
		{
			rend.color = new Color(baseColor.r, baseColor.g, baseColor.b, AlphaRange.Lerp(v));
		}
		if ((bool)mesh)
		{
			mesh.material.SetColor("_Color", new Color(baseColor.r, baseColor.g, baseColor.b, AlphaRange.Lerp(v)));
		}
	}
}
