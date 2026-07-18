using UnityEngine;

public class AlphaBlink : MonoBehaviour
{
	public float Period = 1f;

	public float Ratio = 0.5f;

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
		float num = Time.time % Period / Period;
		num = ((num < Ratio) ? 1 : 0);
		if ((bool)rend)
		{
			rend.color = new Color(baseColor.r, baseColor.g, baseColor.b, AlphaRange.Lerp(num));
		}
		if ((bool)mesh)
		{
			mesh.material.SetColor("_Color", new Color(baseColor.r, baseColor.g, baseColor.b, AlphaRange.Lerp(num)));
		}
	}
}
