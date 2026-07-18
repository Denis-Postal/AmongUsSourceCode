using UnityEngine;

public class DetectTamper
{
	public static bool Detect()
	{
		if (Application.genuineCheckAvailable)
		{
			return Application.genuine;
		}
		return true;
	}
}
