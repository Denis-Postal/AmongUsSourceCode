using UnityEngine;

public class AmbientSoundPlayer : MonoBehaviour
{
	public AudioClip AmbientSound;

	public Collider2D[] HitAreas;

	public float MaxVolume = 1f;

	public void Start()
	{
		SoundManager.Instance.PlayDynamicSound(base.name, AmbientSound, loop: true, Dynamics);
	}

	private void Dynamics(AudioSource source, float dt)
	{
		if (!PlayerControl.LocalPlayer)
		{
			source.volume = 0f;
			return;
		}
		Vector2 truePosition = PlayerControl.LocalPlayer.GetTruePosition();
		bool flag = false;
		for (int i = 0; i < HitAreas.Length; i++)
		{
			if (HitAreas[i].OverlapPoint(truePosition))
			{
				flag = true;
				break;
			}
		}
		float num = (flag ? 1 : 0);
		source.volume = Mathf.Lerp(source.volume, num * MaxVolume, dt);
	}

	public void OnDestroy()
	{
		SoundManager.Instance.StopSound(AmbientSound);
	}
}
