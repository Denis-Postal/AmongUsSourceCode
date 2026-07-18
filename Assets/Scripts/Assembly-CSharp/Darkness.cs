using UnityEngine;

public class Darkness : MonoBehaviour
{
	public float fallSpeed = 10f;

	private void Update()
	{
		Vector3 position = base.transform.position;
		position.y += fallSpeed * Time.deltaTime;
		base.transform.position = position;
	}
}
