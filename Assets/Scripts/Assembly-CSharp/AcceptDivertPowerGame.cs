using System.Collections;
using UnityEngine;

public class AcceptDivertPowerGame : Minigame
{
	private LineRenderer[] LeftWires;

	private LineRenderer[] RightWires;

	public GameObject RightWireParent;

	public GameObject LeftWireParent;

	public SpriteRenderer Switch;

	public AudioClip SwitchSound;

	private bool done;

	public void Start()
	{
		LeftWires = LeftWireParent.GetComponentsInChildren<LineRenderer>();
		RightWires = RightWireParent.GetComponentsInChildren<LineRenderer>();
		for (int i = 0; i < LeftWires.Length; i++)
		{
			LeftWires[i].material.SetColor("_Color", Color.yellow);
		}
	}

	public void DoSwitch()
	{
		if (!done)
		{
			done = true;
			if (Constants.ShouldPlaySfx())
			{
				SoundManager.Instance.PlaySound(SwitchSound, loop: false);
			}
			StartCoroutine(CoDoSwitch());
		}
	}

	private IEnumerator CoDoSwitch()
	{
		yield return new WaitForLerp(0.25f, delegate(float t)
		{
			Switch.transform.localEulerAngles = new Vector3(0f, 0f, Mathf.Lerp(0f, 90f, t));
		});
		LeftWires[0].SetPosition(1, new Vector3(1.265f, 0f, 0f));
		for (int num = 0; num < RightWires.Length; num++)
		{
			RightWires[num].enabled = true;
			RightWires[num].material.SetColor("_Color", Color.yellow);
		}
		for (int num2 = 0; num2 < LeftWires.Length; num2++)
		{
			LeftWires[num2].material.SetColor("_Color", Color.yellow);
		}
		if ((bool)MyNormTask)
		{
			MyNormTask.NextStep();
		}
		StartCoroutine(CoStartClose());
	}

	public void Update()
	{
		for (int i = 0; i < LeftWires.Length; i++)
		{
			Vector2 textureOffset = LeftWires[i].material.GetTextureOffset("_MainTex");
			textureOffset.x -= Time.deltaTime * 3f;
			LeftWires[i].material.SetTextureOffset("_MainTex", textureOffset);
		}
		for (int j = 0; j < RightWires.Length; j++)
		{
			Vector2 textureOffset2 = RightWires[j].material.GetTextureOffset("_MainTex");
			textureOffset2.x += Time.deltaTime * 3f;
			RightWires[j].material.SetTextureOffset("_MainTex", textureOffset2);
		}
	}
}
