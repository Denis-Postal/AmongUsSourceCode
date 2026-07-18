using System.Collections;
using UnityEngine;

public class BanButton : MonoBehaviour
{
	public TextRenderer NameText;

	public SpriteRenderer Background;

	public int TargetClientId;

	public int numVotes;

	public BanMenu Parent { get; set; }

	public void Start()
	{
		Background.SetCooldownNormalizedUvs();
	}

	public void Select()
	{
		Background.color = new Color(1f, 1f, 1f, 1f);
		Parent.Select(TargetClientId);
	}

	public void Unselect()
	{
		Background.color = new Color(0.3f, 0.3f, 0.3f, 0.5f);
	}

	public void SetVotes(int newVotes)
	{
		StopAllCoroutines();
		StartCoroutine(CoSetVotes(numVotes, newVotes));
		numVotes = newVotes;
	}

	private IEnumerator CoSetVotes(int oldNum, int newNum)
	{
		_ = (float)oldNum / 3f;
		float end = (float)newNum / 3f;
		for (float timer = 0f; timer < 0.2f; timer += Time.deltaTime)
		{
			Background.material.SetFloat("_Percent", Mathf.SmoothStep(end, end, timer / 0.2f));
			yield return null;
		}
		Background.material.SetFloat("_Percent", end);
	}
}
