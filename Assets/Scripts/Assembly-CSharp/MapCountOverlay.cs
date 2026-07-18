using UnityEngine;

public class MapCountOverlay : MonoBehaviour
{
	public AlphaPulse BackgroundColor;

	public TextRenderer SabotageText;

	public CounterArea[] CountAreas;

	private Collider2D[] buffer = new Collider2D[20];

	private ContactFilter2D filter;

	private float timer;

	private bool isSab;

	public void Awake()
	{
		filter.useLayerMask = true;
		filter.layerMask = Constants.PlayersOnlyMask;
		filter.useTriggers = true;
	}

	public void OnEnable()
	{
		BackgroundColor.SetColor(PlayerTask.PlayerHasTaskOfType<IHudOverrideTask>(PlayerControl.LocalPlayer) ? Palette.DisabledGrey : Color.green);
		timer = 1f;
	}

	public void OnDisable()
	{
		for (int i = 0; i < CountAreas.Length; i++)
		{
			CountAreas[i].UpdateCount(0);
		}
	}

	public void Update()
	{
		timer += Time.deltaTime;
		if (timer < 0.1f)
		{
			return;
		}
		timer = 0f;
		if (!isSab && PlayerTask.PlayerHasTaskOfType<IHudOverrideTask>(PlayerControl.LocalPlayer))
		{
			isSab = true;
			BackgroundColor.SetColor(Palette.DisabledGrey);
			SabotageText.gameObject.SetActive(value: true);
			return;
		}
		if (isSab && !PlayerTask.PlayerHasTaskOfType<IHudOverrideTask>(PlayerControl.LocalPlayer))
		{
			isSab = false;
			BackgroundColor.SetColor(Color.green);
			SabotageText.gameObject.SetActive(value: false);
		}
		for (int i = 0; i < CountAreas.Length; i++)
		{
			CounterArea counterArea = CountAreas[i];
			if (!PlayerTask.PlayerHasTaskOfType<IHudOverrideTask>(PlayerControl.LocalPlayer))
			{
				int num = ShipStatus.Instance.FastRooms[counterArea.RoomType].roomArea.OverlapCollider(filter, buffer);
				int num2 = num;
				for (int j = 0; j < num; j++)
				{
					Collider2D collider2D = buffer[j];
					if (!(collider2D.tag == "DeadBody"))
					{
						PlayerControl component = collider2D.GetComponent<PlayerControl>();
						if (!component || component.Data == null || component.Data.Disconnected || component.Data.IsDead)
						{
							num2--;
						}
					}
				}
				counterArea.UpdateCount(num2);
			}
			else
			{
				counterArea.UpdateCount(0);
			}
		}
	}
}
