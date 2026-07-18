using System.Collections.Generic;

public abstract class SabotageTask : PlayerTask
{
	protected bool didContribute;

	public ArrowBehaviour[] Arrows;

	public void MarkContributed()
	{
		didContribute = true;
	}

	protected void SetupArrows()
	{
		if (base.Owner.AmOwner)
		{
			List<Console> list = FindConsoles();
			for (int i = 0; i < list.Count; i++)
			{
				int consoleId = list[i].ConsoleId;
				Arrows[consoleId].target = list[i].transform.position;
				Arrows[consoleId].gameObject.SetActive(value: true);
			}
		}
		else
		{
			for (int j = 0; j < Arrows.Length; j++)
			{
				Arrows[j].gameObject.SetActive(value: false);
			}
		}
	}
}
