using PowerTools;
using UnityEngine;

public class PetBehaviour : MonoBehaviour, IBuyable, ISteamBuyable
{
	private const float SnapDistance = 2f;

	public bool Free;

	public string ProductId;

	public string StoreName;

	public uint SteamId;

	public int ItchId;

	public string ItchUrl;

	public PlayerControl Source;

	public const float MinDistance = 0.2f;

	public const float damping = 0.7f;

	public const float Easing = 0.2f;

	public const float Speed = 5f;

	public float YOffset = -0.25f;

	public SpriteAnim animator;

	public SpriteRenderer rend;

	public SpriteRenderer shadowRend;

	public Rigidbody2D body;

	public Collider2D Collider;

	public AnimationClip idleClip;

	public AnimationClip sadClip;

	public AnimationClip scaredClip;

	public AnimationClip walkClip;

	public string ProdId => ProductId;

	public string SteamPrice => "$2.99";

	public uint SteamAppId => SteamId;

	public bool Visible
	{
		set
		{
			if ((bool)rend)
			{
				rend.enabled = value;
			}
			if ((bool)shadowRend)
			{
				shadowRend.enabled = value;
			}
		}
	}

	private Vector2 GetTruePosition()
	{
		return (Vector2)base.transform.position + Collider.offset * 0.7f;
	}

	public void FixedUpdate()
	{
		if (!Source)
		{
			body.velocity = Vector2.zero;
			return;
		}
		Vector2 truePosition = Source.GetTruePosition();
		Vector2 truePosition2 = GetTruePosition();
		Vector2 vector = body.velocity;
		Vector2 vector2 = truePosition - truePosition2;
		float num = 0f;
		if (Source.CanMove)
		{
			num = 0.2f;
		}
		if (vector2.sqrMagnitude > num)
		{
			if (vector2.sqrMagnitude > 2f)
			{
				base.transform.position = truePosition;
				return;
			}
			vector2 *= 5f * PlayerControl.GameOptions.PlayerSpeedMod;
			vector = vector * 0.8f + vector2 * 0.2f;
		}
		else
		{
			vector *= 0.7f;
		}
		AnimationClip currentAnimation = animator.GetCurrentAnimation();
		if (vector.sqrMagnitude > 0.01f)
		{
			if (currentAnimation != walkClip)
			{
				animator.Play(walkClip);
			}
			if (vector.x < -0.01f)
			{
				rend.flipX = true;
			}
			else if (vector.x > 0.01f)
			{
				rend.flipX = false;
			}
		}
		else if (currentAnimation == walkClip)
		{
			animator.Play(idleClip);
		}
		body.velocity = vector;
	}

	private void LateUpdate()
	{
		Vector3 localPosition = base.transform.localPosition;
		localPosition.z = (localPosition.y + YOffset) / 1000f + 0.0002f;
		base.transform.localPosition = localPosition;
	}

	public void SetMourning()
	{
		Source = null;
		body.velocity = Vector2.zero;
		animator.Play(sadClip);
	}
}
