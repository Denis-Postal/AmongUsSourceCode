using UnityEngine;

public class CreateStoreButton : MonoBehaviour
{
	public Transform Target;

	public StoreMenu StorePrefab;

	public void Click()
	{
		StoreMenu store = Object.Instantiate(StorePrefab, Target);
		store.GetComponent<TransitionOpen>().OnClose.AddListener(delegate
		{
			Object.Destroy(store.gameObject);
		});
		store.transform.localPosition = new Vector3(0f, 0f, -100f);
		store.transform.localScale = Vector3.zero;
	}
}
