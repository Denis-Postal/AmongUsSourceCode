using UnityEngine;

public class DevFakePlayer : MonoBehaviour
{
	private DummyBehaviour dummy;

	private void Awake()
	{
		dummy = GetComponent<DummyBehaviour>();
		if ((bool)dummy)
		{
			dummy.enabled = false;
		}
	}

	private void Update()
	{
		if ((bool)dummy && !dummy.enabled && (bool)AmongUsClient.Instance && AmongUsClient.Instance.IsGameStarted && (bool)ShipStatus.Instance)
		{
			dummy.enabled = true;
		}
	}
}
