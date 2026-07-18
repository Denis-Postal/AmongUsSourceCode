using PowerTools;
using UnityEngine;

public class VirtualJoystick : MonoBehaviour, IVirtualJoystick
{
	public enum PlayerAnimationMode
	{
		SpriteFrames = 0,
		AnimationClips = 1
	}

	[Header("Joystick Settings")]
	public float InnerRadius = 0.64f;

	public float OuterRadius = 1.28f;

	[Header("Spawned Player")]
	public bool UseSpawnedLocalPlayer = true;

	public PlayerControl TargetPlayer;

	public SpriteRenderer TargetRenderer;

	public string TargetRendererName = "";

	public bool MoveTargetPlayerDirectly = true;

	public bool MoveTargetByPosition = true;

	public bool PreferPositionMovementForSpawnedPlayer = true;

	public bool RequireCanMoveForDirectMovement = false;

	public float DirectMoveSpeed = 4.5f;

	public bool UsePlayerPhysicsSpeed = true;

	public bool SnapNetworkTransformDuringJoystickMovement = false;

	[Header("Animation Mode")]
	public PlayerAnimationMode AnimationMode = PlayerAnimationMode.SpriteFrames;

	[Header("Animation Clip Mode")]
	public SpriteAnim TargetSpriteAnim;

	public string TargetSpriteAnimName = "";

	public AnimationClip IdleAnimationClip;

	public AnimationClip WalkAnimationClip;

	public float ClipAnimationSpeed = 1f;

	public bool DisablePlayerPhysicsWhenUsingJoystickAnimation = true;

	[Header("Frame Animation")]
	public bool UseFrameAnimation = true;

	public Sprite[] IdleFrames;

	public Sprite[] WalkFrames;

	public float AnimationFps = 12f;

	public bool AnimateIdle = true;

	public bool DisableSpriteAnimWhenUsingFrames = true;

	public bool AutoEnableFrameAnimationWhenFramesExist = true;

	[Header("References")]
	public CircleCollider2D Outer;

	public SpriteRenderer Inner;

	private int touchId = -1;

	private bool isDragging = false;

	private float frameTimer;

	private int frameIndex;

	private bool wasWalking;

	private SpriteAnim targetSpriteAnim;

	private SpriteAnim[] targetSpriteAnims;

	private Rigidbody2D targetBody;

	private SpriteRenderer[] targetRenderers;

	public Vector2 Delta { get; private set; }

	private void Start()
	{
		if (Outer == null)
		{
			Outer = GetComponent<CircleCollider2D>();
		}
		if (Inner == null)
		{
			Inner = GetComponentInChildren<SpriteRenderer>();
		}
		ResolveSpawnedPlayer();
		Delta = Vector2.zero;
	}

	private void Update()
	{
		HandleTouchInput();
		UpdateJoystickPosition();
		ResolveSpawnedPlayer();
		ResolveTargetRenderer();
	}

	protected virtual void FixedUpdate()
	{
		MoveTargetPlayer();
	}

	private void LateUpdate()
	{
		MoveTargetPlayerByPosition(Time.deltaTime);
		AnimateTargetPlayerByClip();
		AnimateTargetPlayer();
	}

	private void ResolveSpawnedPlayer()
	{
		if (!UseSpawnedLocalPlayer)
		{
			return;
		}
		PlayerControl localPlayer = PlayerControl.LocalPlayer;
		if (!localPlayer || TargetPlayer == localPlayer)
		{
			return;
		}
		TargetPlayer = localPlayer;
		targetRenderers = TargetPlayer.GetComponentsInChildren<SpriteRenderer>(true);
		TargetRenderer = FindAnimationRenderer();
		targetSpriteAnims = TargetPlayer.GetComponentsInChildren<SpriteAnim>(true);
		targetSpriteAnim = FindAnimationSpriteAnim();
		TargetSpriteAnim = targetSpriteAnim;
		targetBody = TargetPlayer.GetComponent<Rigidbody2D>();
		DisablePlayerPhysicsAnimationIfNeeded();
		DisableTargetSpriteAnimsIfNeeded();
		frameTimer = 0f;
		frameIndex = 0;
		wasWalking = false;
	}

	private void ResolveTargetRenderer()
	{
		if (!TargetPlayer)
		{
			return;
		}
		if (!TargetRenderer)
		{
			targetRenderers = TargetPlayer.GetComponentsInChildren<SpriteRenderer>(true);
			TargetRenderer = FindAnimationRenderer();
		}
		if (!targetBody)
		{
			targetBody = TargetPlayer.GetComponent<Rigidbody2D>();
		}
		if (!targetSpriteAnim)
		{
			targetSpriteAnim = FindAnimationSpriteAnim();
			TargetSpriteAnim = targetSpriteAnim;
		}
		if (targetSpriteAnims == null || targetSpriteAnims.Length == 0)
		{
			targetSpriteAnims = TargetPlayer.GetComponentsInChildren<SpriteAnim>(true);
		}
		if ((!FrameAnimationEnabled() || ClipAnimationEnabled()) && (bool)targetSpriteAnim && !targetSpriteAnim.enabled)
		{
			targetSpriteAnim.enabled = true;
		}
		DisablePlayerPhysicsAnimationIfNeeded();
	}

	private void MoveTargetPlayer()
	{
		if (ShouldMoveByPosition() || !MoveTargetPlayerDirectly || !TargetPlayer || !targetBody)
		{
			return;
		}
		if ((RequireCanMoveForDirectMovement && !TargetPlayer.CanMove) || TargetPlayer.inVent)
		{
			targetBody.velocity = Vector2.zero;
			return;
		}
		float speed = GetTargetMoveSpeed();
		targetBody.velocity = Delta * speed;
	}

	private void MoveTargetPlayerByPosition(float dt)
	{
		if (!ShouldMoveByPosition() || !MoveTargetPlayerDirectly || !TargetPlayer)
		{
			return;
		}
		if ((RequireCanMoveForDirectMovement && !TargetPlayer.CanMove) || TargetPlayer.inVent)
		{
			if ((bool)targetBody)
			{
				targetBody.velocity = Vector2.zero;
			}
			return;
		}
		if (Delta.sqrMagnitude <= 0.0001f)
		{
			if ((bool)targetBody)
			{
				targetBody.velocity = Vector2.zero;
			}
			return;
		}
		float speed = GetTargetMoveSpeed();
		Vector3 position = TargetPlayer.transform.position;
		position += (Vector3)(Delta * speed * dt);
		position.z = position.y / 1000f;
		TargetPlayer.transform.position = position;
		if ((bool)targetBody)
		{
			targetBody.position = (Vector2)position;
			targetBody.velocity = Vector2.zero;
		}
		if (SnapNetworkTransformDuringJoystickMovement && (bool)TargetPlayer.NetTransform)
		{
			TargetPlayer.NetTransform.SnapTo((Vector2)position);
			Vector3 snappedPosition = TargetPlayer.transform.position;
			snappedPosition.z = snappedPosition.y / 1000f;
			TargetPlayer.transform.position = snappedPosition;
		}
	}

	private bool ShouldMoveByPosition()
	{
		return MoveTargetByPosition || (PreferPositionMovementForSpawnedPlayer && UseSpawnedLocalPlayer);
	}

	private float GetTargetMoveSpeed()
	{
		float speed = DirectMoveSpeed;
		if (UsePlayerPhysicsSpeed && (bool)TargetPlayer && (bool)TargetPlayer.MyPhysics && GameData.Instance)
		{
			GameData.PlayerInfo playerById = TargetPlayer.Data;
			speed = (playerById != null && playerById.IsDead) ? TargetPlayer.MyPhysics.TrueGhostSpeed : TargetPlayer.MyPhysics.TrueSpeed;
		}
		return speed;
	}

	private void AnimateTargetPlayer()
	{
		if (!FrameAnimationEnabled() || !TargetPlayer || !TargetRenderer)
		{
			return;
		}
		DisableTargetSpriteAnimsIfNeeded();
		bool walking = Delta.sqrMagnitude > 0.01f;
		Sprite[] frames = walking ? WalkFrames : IdleFrames;
		if ((!walking && !AnimateIdle) || frames == null || frames.Length == 0 || AnimationFps <= 0f)
		{
			return;
		}
		if (walking != wasWalking)
		{
			frameTimer = 0f;
			frameIndex = 0;
			wasWalking = walking;
		}
		frameTimer += Time.deltaTime;
		float frameDuration = 1f / AnimationFps;
		while (frameTimer >= frameDuration)
		{
			frameTimer -= frameDuration;
			frameIndex++;
		}
		TargetRenderer.sprite = frames[frameIndex % frames.Length];
		if (walking)
		{
			if (Delta.x > 0.1f)
			{
				TargetRenderer.flipX = false;
			}
			else if (Delta.x < -0.1f)
			{
				TargetRenderer.flipX = true;
			}
		}
	}

	private void AnimateTargetPlayerByClip()
	{
		if (!ClipAnimationEnabled() || !TargetPlayer)
		{
			return;
		}
		if (!TargetSpriteAnim)
		{
			TargetSpriteAnim = FindAnimationSpriteAnim();
			targetSpriteAnim = TargetSpriteAnim;
		}
		if (!TargetSpriteAnim)
		{
			return;
		}
		if (!TargetSpriteAnim.enabled)
		{
			TargetSpriteAnim.enabled = true;
		}
		bool walking = Delta.sqrMagnitude > 0.01f;
		AnimationClip clip = walking ? WalkAnimationClip : IdleAnimationClip;
		if (!clip)
		{
			clip = walking ? IdleAnimationClip : WalkAnimationClip;
		}
		if (!clip)
		{
			return;
		}
		if (TargetSpriteAnim.Clip != clip)
		{
			TargetSpriteAnim.Play(clip, ClipAnimationSpeed);
		}
		else
		{
			TargetSpriteAnim.Speed = ClipAnimationSpeed;
		}
		FlipTargetRenderer(walking);
	}

	private SpriteRenderer FindAnimationRenderer()
	{
		if (!TargetPlayer)
		{
			return null;
		}
		if (!string.IsNullOrEmpty(TargetRendererName))
		{
			SpriteRenderer[] renderersByName = targetRenderers ?? TargetPlayer.GetComponentsInChildren<SpriteRenderer>(true);
			for (int i = 0; i < renderersByName.Length; i++)
			{
				if ((bool)renderersByName[i] && renderersByName[i].name == TargetRendererName)
				{
					return renderersByName[i];
				}
			}
		}
		SpriteRenderer rootRenderer = TargetPlayer.GetComponent<SpriteRenderer>();
		if ((bool)rootRenderer)
		{
			return rootRenderer;
		}
		SpriteRenderer[] renderers = targetRenderers ?? TargetPlayer.GetComponentsInChildren<SpriteRenderer>(true);
		for (int j = 0; j < renderers.Length; j++)
		{
			if (!(bool)renderers[j])
			{
				continue;
			}
			string rendererName = renderers[j].name;
			if (rendererName == "HatSlot" || rendererName == "SkinLayer" || rendererName == "NameText")
			{
				continue;
			}
			return renderers[j];
		}
		return null;
	}

	private SpriteAnim FindAnimationSpriteAnim()
	{
		if (!TargetPlayer)
		{
			return null;
		}
		if ((bool)TargetSpriteAnim)
		{
			return TargetSpriteAnim;
		}
		if (!string.IsNullOrEmpty(TargetSpriteAnimName))
		{
			SpriteAnim[] animsByName = targetSpriteAnims ?? TargetPlayer.GetComponentsInChildren<SpriteAnim>(true);
			for (int i = 0; i < animsByName.Length; i++)
			{
				if ((bool)animsByName[i] && animsByName[i].name == TargetSpriteAnimName)
				{
					return animsByName[i];
				}
			}
		}
		if ((bool)TargetRenderer)
		{
			SpriteAnim rendererAnim = TargetRenderer.GetComponent<SpriteAnim>();
			if ((bool)rendererAnim)
			{
				return rendererAnim;
			}
		}
		SpriteAnim rootAnim = TargetPlayer.GetComponent<SpriteAnim>();
		if ((bool)rootAnim)
		{
			return rootAnim;
		}
		SpriteAnim[] anims = targetSpriteAnims ?? TargetPlayer.GetComponentsInChildren<SpriteAnim>(true);
		return (anims != null && anims.Length > 0) ? anims[0] : null;
	}

	private bool FrameAnimationEnabled()
	{
		if (AnimationMode != PlayerAnimationMode.SpriteFrames)
		{
			return false;
		}
		if (UseFrameAnimation)
		{
			return true;
		}
		return AutoEnableFrameAnimationWhenFramesExist && ((IdleFrames != null && IdleFrames.Length > 0) || (WalkFrames != null && WalkFrames.Length > 0));
	}

	private bool ClipAnimationEnabled()
	{
		return AnimationMode == PlayerAnimationMode.AnimationClips && ((bool)IdleAnimationClip || (bool)WalkAnimationClip);
	}

	private void DisablePlayerPhysicsAnimationIfNeeded()
	{
		if (!DisablePlayerPhysicsWhenUsingJoystickAnimation || !TargetPlayer || !TargetPlayer.MyPhysics)
		{
			return;
		}
		if ((FrameAnimationEnabled() || ClipAnimationEnabled()) && TargetPlayer.MyPhysics.enabled)
		{
			TargetPlayer.MyPhysics.enabled = false;
		}
	}

	private void FlipTargetRenderer(bool walking)
	{
		if (!walking || !TargetRenderer)
		{
			return;
		}
		if (Delta.x > 0.1f)
		{
			TargetRenderer.flipX = false;
		}
		else if (Delta.x < -0.1f)
		{
			TargetRenderer.flipX = true;
		}
	}

	private void DisableTargetSpriteAnimsIfNeeded()
	{
		if (!DisableSpriteAnimWhenUsingFrames || !FrameAnimationEnabled() || targetSpriteAnims == null)
		{
			return;
		}
		for (int i = 0; i < targetSpriteAnims.Length; i++)
		{
			if ((bool)targetSpriteAnims[i] && targetSpriteAnims[i].enabled)
			{
				targetSpriteAnims[i].enabled = false;
			}
		}
	}

	private void HandleTouchInput()
	{
		if (Application.isEditor || Application.platform != RuntimePlatform.Android)
		{
			Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
			if (Input.GetMouseButtonDown(0) && (bool)Outer && Outer.OverlapPoint(mousePos))
			{
				touchId = -2;
				isDragging = true;
			}
			else if (Input.GetMouseButtonUp(0) && touchId == -2)
			{
				touchId = -1;
				isDragging = false;
				Delta = Vector2.zero;
			}
		}
		for (int i = 0; i < Input.touchCount; i++)
		{
			Touch touch = Input.GetTouch(i);
			Vector2 worldPos = Camera.main.ScreenToWorldPoint(touch.position);
			if (touchId == -1)
			{
				if (touch.phase == TouchPhase.Began && (bool)Outer && Outer.OverlapPoint(worldPos))
				{
					touchId = touch.fingerId;
					isDragging = true;
				}
			}
			else if (touch.fingerId == touchId && (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled))
			{
				touchId = -1;
				isDragging = false;
				Delta = Vector2.zero;
			}
		}
	}

	private void UpdateJoystickPosition()
	{
		if (!Inner)
		{
			return;
		}
		if (!isDragging)
		{
			Inner.transform.localPosition = new Vector3(0f, 0f, -1f);
			return;
		}
		Vector2 touchPos = Vector2.zero;
		if (touchId == -2)
		{
			touchPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
		}
		else
		{
			foreach (Touch touch in Input.touches)
			{
				if (touch.fingerId == touchId)
				{
					touchPos = Camera.main.ScreenToWorldPoint(touch.position);
				}
			}
		}
		Vector2 direction = touchPos - (Vector2)base.transform.position;
		float maxDistance = Mathf.Max(0.01f, OuterRadius - InnerRadius);
		Vector2 clampedPosition = Vector2.ClampMagnitude(direction, maxDistance);
		Inner.transform.localPosition = new Vector3(clampedPosition.x, clampedPosition.y, -1f);
		Delta = clampedPosition / maxDistance;
	}

	public virtual void UpdateJoystick(FingerBehaviour finger, Vector2 velocity, bool syncFinger)
	{
	}
}
