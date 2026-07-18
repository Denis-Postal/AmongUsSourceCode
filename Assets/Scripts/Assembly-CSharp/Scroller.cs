using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class Scroller : MonoBehaviour
{
	public Transform Inner;

	public Collider2D HitBox;

	private Controller myController = new Controller();

	private Vector3 origin;

	public bool allowX;

	public FloatRange XBounds = new FloatRange(-10f, 10f);

	public bool allowY;

	public FloatRange YBounds = new FloatRange(-10f, 10f);

	public float YBoundPerItem;

	public FloatRange ScrollerYRange;

	public SpriteRenderer ScrollerY;

	private readonly Dictionary<SpriteRenderer, Material> clippedSpriteMaterials = new Dictionary<SpriteRenderer, Material>();

	private Shader clippedSpriteShader;

	private Material clippedSpriteTemplate;

	public bool AtTop => Inner.localPosition.y <= YBounds.min + 0.25f;

	public bool AtBottom => Inner.localPosition.y >= YBounds.max - 0.25f;

	private void OnDestroy()
	{
		foreach (Material material in clippedSpriteMaterials.Values)
		{
			if ((bool)material)
			{
				Object.Destroy(material);
			}
		}
		clippedSpriteMaterials.Clear();
	}

	public void FixedUpdate()
	{
		if ((bool)Inner)
		{
			Vector2 mouseScrollDelta = Input.mouseScrollDelta;
			mouseScrollDelta.y = 0f - mouseScrollDelta.y;
			DoScroll(Inner.transform.localPosition, mouseScrollDelta);
			myController.Update();
			switch (myController.CheckDrag(HitBox))
			{
			case DragState.TouchStart:
				origin = Inner.transform.localPosition;
				break;
			case DragState.Dragging:
			{
				Vector2 del = myController.DragPosition - myController.DragStartPosition;
				DoScroll(origin, del);
				break;
			}
			}
		}
	}

	private void LateUpdate()
	{
		UpdateTextClip();
	}

	private void UpdateTextClip()
	{
		if (!Inner || !HitBox)
		{
			return;
		}
		Bounds bounds = HitBox.bounds;
		Vector4 clipRect = new Vector4(bounds.min.x, bounds.min.y, bounds.max.x, bounds.max.y);
		TextRenderer[] textRenderers = Inner.GetComponentsInChildren<TextRenderer>(true);
		for (int i = 0; i < textRenderers.Length; i++)
		{
			textRenderers[i].SetClipRect(enabled: true, clipRect);
		}
		TextMeshPro[] tmps = Inner.GetComponentsInChildren<TextMeshPro>(true);
		for (int j = 0; j < tmps.Length; j++)
		{
			TextRenderer.ApplyClipRect(tmps[j].fontMaterial, enabled: true, clipRect);
		}
		SpriteRenderer[] sprites = Inner.GetComponentsInChildren<SpriteRenderer>(true);
		for (int k = 0; k < sprites.Length; k++)
		{
			ApplySpriteClipRect(sprites[k], clipRect);
		}
	}

	private void ApplySpriteClipRect(SpriteRenderer spriteRenderer, Vector4 clipRect)
	{
		if (!spriteRenderer)
		{
			return;
		}
		Material material = GetClippedSpriteMaterial(spriteRenderer);
		TextRenderer.ApplyClipRect(material, enabled: true, clipRect);
	}

	private Material GetClippedSpriteMaterial(SpriteRenderer spriteRenderer)
	{
		Material cachedMaterial;
		if (clippedSpriteMaterials.TryGetValue(spriteRenderer, out cachedMaterial) && (bool)cachedMaterial)
		{
			return cachedMaterial;
		}
		Material source = spriteRenderer.sharedMaterial;
		if ((bool)source && source.HasProperty("_UseClipRect") && source.HasProperty("_ClipRect"))
		{
			cachedMaterial = spriteRenderer.material;
		}
		else
		{
			if (!clippedSpriteShader)
			{
				clippedSpriteShader = ResolveClippedSpriteShader();
			}
			if (!clippedSpriteShader)
			{
				return source;
			}
			if ((bool)source)
			{
				cachedMaterial = new Material(source);
			}
			else
			{
				Material template = ResolveClippedSpriteTemplate();
				if ((bool)template)
				{
					cachedMaterial = new Material(template);
				}
				else
				{
					Shader defaultShader = Shader.Find("Sprites/Default");
					if (!defaultShader)
					{
						return null;
					}
					cachedMaterial = new Material(defaultShader);
				}
			}
			cachedMaterial.shader = clippedSpriteShader;
			cachedMaterial.name = spriteRenderer.name + " ClipRect";
			if (cachedMaterial.HasProperty("_Color"))
			{
				cachedMaterial.SetColor("_Color", Color.white);
			}
			if ((bool)source && source.HasProperty("_BodyColor") && cachedMaterial.HasProperty("_UsePlayerColors"))
			{
				cachedMaterial.SetFloat("_UsePlayerColors", 1f);
			}
			spriteRenderer.material = cachedMaterial;
		}
		clippedSpriteMaterials[spriteRenderer] = cachedMaterial;
		return cachedMaterial;
	}

	private Shader ResolveClippedSpriteShader()
	{
		Material template = ResolveClippedSpriteTemplate();
		if ((bool)template && (bool)template.shader)
		{
			return template.shader;
		}
		return Shader.Find("Sprites/ClipRect");
	}

	private Material ResolveClippedSpriteTemplate()
	{
		if (!clippedSpriteTemplate)
		{
			clippedSpriteTemplate = Resources.Load<Material>("SpriteClipRectRuntime");
		}
		return clippedSpriteTemplate;
	}

	public void ScrollDown()
	{
		float num = HitBox.bounds.max.y - HitBox.bounds.min.y;
		DoScroll(Inner.transform.localPosition, new Vector2(0f, num * 0.75f));
	}

	public void ScrollUp()
	{
		float num = HitBox.bounds.max.y - HitBox.bounds.min.y;
		DoScroll(Inner.transform.localPosition, new Vector2(0f, num * -0.75f));
	}

	public void ScrollPercentY(float p)
	{
		Vector3 localPosition = Inner.transform.localPosition;
		Mathf.Max(YBounds.min, YBounds.max);
		localPosition.y = YBounds.Lerp(p);
		Inner.transform.localPosition = localPosition;
		UpdateScrollBars(localPosition);
	}

	private void DoScroll(Vector3 origin, Vector2 del)
	{
		if (!(del.magnitude < 0.05f))
		{
			if (!allowX)
			{
				del.x = 0f;
			}
			if (!allowY)
			{
				del.y = 0f;
			}
			Vector3 vector = origin + (Vector3)del;
			vector.x = XBounds.Clamp(vector.x);
			int childCount = Inner.transform.childCount;
			float max = Mathf.Max(YBounds.min, YBounds.max + YBoundPerItem * (float)childCount);
			vector.y = Mathf.Clamp(vector.y, YBounds.min, max);
			Inner.transform.localPosition = vector;
			UpdateScrollBars(vector);
		}
	}

	private void UpdateScrollBars(Vector3 pos)
	{
		if ((bool)ScrollerY)
		{
			if (YBounds.min == YBounds.max)
			{
				ScrollerY.enabled = false;
				return;
			}
			ScrollerY.enabled = true;
			float num = YBounds.ReverseLerp(pos.y);
			Vector3 localPosition = ScrollerY.transform.localPosition;
			localPosition.y = ScrollerYRange.Lerp(1f - num);
			ScrollerY.transform.localPosition = localPosition;
		}
	}
}
