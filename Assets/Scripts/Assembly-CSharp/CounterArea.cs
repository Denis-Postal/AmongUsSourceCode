using System.Collections.Generic;
using UnityEngine;

public class CounterArea : MonoBehaviour
{
	public SystemTypes RoomType;

	public ObjectPoolBehavior pool;

	private List<PoolableBehavior> myIcons = new List<PoolableBehavior>();

	public float XOffset;

	public float YOffset;

	public int MaxWidth = 5;

	public void UpdateCount(int cnt)
	{
		bool flag = myIcons.Count != cnt;
		while (myIcons.Count < cnt)
		{
			PoolableBehavior item = pool.Get<PoolableBehavior>();
			myIcons.Add(item);
		}
		while (myIcons.Count > cnt)
		{
			PoolableBehavior poolableBehavior = myIcons[myIcons.Count - 1];
			myIcons.RemoveAt(myIcons.Count - 1);
			poolableBehavior.OwnerPool.Reclaim(poolableBehavior);
		}
		if (flag)
		{
			for (int i = 0; i < myIcons.Count; i++)
			{
				int num = i % 5;
				int num2 = i / 5;
				float num3 = (float)(Mathf.Min(cnt - num2 * 5, 5) - 1) * XOffset / -2f;
				myIcons[i].transform.position = base.transform.position + new Vector3(num3 + (float)num * XOffset, (float)num2 * YOffset, -1f);
			}
		}
	}
}
