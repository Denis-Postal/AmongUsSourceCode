using UnityEngine;

public class LogEntryBubble : PoolableBehavior
{
	public SpriteRenderer HeadImage;

	public SpriteRenderer Background;

	public TextRenderer Text;

	public void PrepareForDisplay()
	{
		int layer = base.gameObject.layer;
		if ((bool)Background)
		{
			layer = Background.gameObject.layer;
			Background.sortingOrder = 0;
		}
		if ((bool)HeadImage)
		{
			HeadImage.gameObject.layer = layer;
			HeadImage.sortingOrder = 2;
		}
		if ((bool)Text)
		{
			Text.gameObject.layer = layer;
			MeshRenderer[] renderers = Text.GetComponentsInChildren<MeshRenderer>(true);
			for (int i = 0; i < renderers.Length; i++)
			{
				renderers[i].sortingOrder = 3;
			}
		}
	}
}
