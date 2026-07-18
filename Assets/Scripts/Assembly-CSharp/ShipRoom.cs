using UnityEngine;

public class ShipRoom : MonoBehaviour
{
	public SystemTypes RoomId;

	public SurvCamera survCamera;

	public Collider2D roomArea;

	public AudioClip AmbientSound;

	public float AmbientVolume = 0.7f;

	public float AmbientMaxDist = 8f;

	public Vector2 AmbientOffset;

	public SoundGroup FootStepSounds;
}
