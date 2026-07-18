using System.Collections;
using UnityEngine;

public class FreeWeekendShower : MonoBehaviour
{
	public TextRenderer Output;

	private void Start()
	{
		StartCoroutine(Check());
	}

	private IEnumerator Check()
	{
		WaitForSeconds wait = new WaitForSeconds(0.5f);
		while (true)
		{
			if (SaveManager.IsFreeWeekend == FreeWeekendState.FreeMIRA)
			{
				Output.Text = "MIRA Free Weekend!";
			}
			else
			{
				Output.Text = string.Empty;
			}
			yield return wait;
		}
	}
}
