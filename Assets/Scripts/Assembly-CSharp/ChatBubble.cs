using TMPro;
using UnityEngine;

internal class ChatBubble : PoolableBehavior
{
	public SpriteRenderer ChatFace;

	public SpriteRenderer Xmark;

	public SpriteRenderer votedMark;

	public TextRenderer NameText;

	public TextRenderer TextArea;

	public SpriteRenderer Background;

	private Material playerMaterialInstance;

	public void SetText(string chatText)
	{
		TextArea.Text = chatText;
		TextArea.RefreshMesh();
		Background.size = new Vector2(5.52f, 0.2f + GetTextHeight(NameText) + GetTextHeight(TextArea));
	}

	public void AlignChildren()
	{
		Vector3 localPosition = Background.transform.localPosition;
		localPosition.y = NameText.transform.localPosition.y - Background.size.y / 2f + 0.05f;
		Background.transform.localPosition = localPosition;
	}

	public void SetLeft()
	{
		base.transform.localPosition = new Vector3(-3f, 0f, 0f);
		ChatFace.flipX = false;
		ChatFace.transform.localScale = new Vector3(1f, 1f, 1f);
		ChatFace.transform.localPosition = new Vector3(0f, 0.07f, 0f);
		Xmark.transform.localPosition = new Vector3(-0.15f, -0.13f, -0.0001f);
		votedMark.transform.localPosition = new Vector3(-0.15f, -0.13f, -0.0001f);
		NameText.transform.localPosition = new Vector3(0.5f, 0.34f, 0f);
		NameText.RightAligned = false;
		TextArea.transform.localPosition = new Vector3(0.5f, 0.09f, 0f);
		TextArea.RightAligned = false;
	}

	public void SetNotification()
	{
		base.transform.localPosition = new Vector3(-2.75f, 0f, 0f);
		ChatFace.flipX = false;
		ChatFace.transform.localScale = new Vector3(0.75f, 0.75f, 1f);
		ChatFace.transform.localPosition = new Vector3(0f, 0.18f, 0f);
		Xmark.transform.localPosition = new Vector3(-0.15f, -0.13f, -0.0001f);
		votedMark.transform.localPosition = new Vector3(-0.15f, -0.13f, -0.0001f);
		NameText.transform.localPosition = new Vector3(0.5f, 0.34f, 0f);
		NameText.RightAligned = false;
		TextArea.transform.localPosition = new Vector3(0.5f, 0.09f, 0f);
		TextArea.RightAligned = false;
		SetText(string.Empty);
	}

	public void SetRight()
	{
		base.transform.localPosition = new Vector3(-2.35f, 0f, 0f);
		ChatFace.flipX = true;
		ChatFace.transform.localScale = new Vector3(1f, 1f, 1f);
		ChatFace.transform.localPosition = new Vector3(4.75f, 0.07f, 0f);
		Xmark.transform.localPosition = new Vector3(0.15f, -0.13f, -0.0001f);
		votedMark.transform.localPosition = new Vector3(0.15f, -0.13f, -0.0001f);
		NameText.transform.localPosition = new Vector3(4.35f, 0.34f, 0f);
		NameText.RightAligned = true;
		TextArea.transform.localPosition = new Vector3(4.35f, 0.09f, 0f);
		TextArea.RightAligned = true;
	}

	public void SetName(string playerName, bool isDead, bool voted, Color color)
	{
		NameText.Text = playerName ?? "...";
		NameText.Color = color;
		NameText.RefreshMesh();
		if (isDead)
		{
			Xmark.enabled = true;
			Background.color = Palette.HalfWhite;
		}
		if (voted)
		{
			votedMark.enabled = true;
		}
	}

	public void SetFaceColor(int colorId)
	{
		if (!ChatFace)
		{
			return;
		}
		if (colorId < 0 || colorId >= Palette.PlayerColors.Length)
		{
			colorId = 0;
		}
		EnsurePlayerMaterial();
		PlayerControl.SetPlayerMaterialColors(colorId, ChatFace);
		Material material = ChatFace.material;
		if (!material)
		{
			return;
		}
		material.SetColor("_BackColor", Palette.ShadowColors[colorId]);
		material.SetColor("_BodyColor", Palette.PlayerColors[colorId]);
		material.SetColor("_VisorColor", Palette.VisorColor);
		if (material.HasProperty("_UsePlayerColors"))
		{
			material.SetFloat("_UsePlayerColors", 1f);
		}
	}

	private void EnsurePlayerMaterial()
	{
		if (!ChatFace)
		{
			return;
		}
		Material current = ChatFace.sharedMaterial;
		if ((bool)current && current.HasProperty("_BodyColor") && current.HasProperty("_BackColor") && current.HasProperty("_VisorColor"))
		{
			return;
		}
		Shader playerShader = Shader.Find("Unlit/PlayerShader");
		if (!playerShader)
		{
			return;
		}
		if (!playerMaterialInstance)
		{
			playerMaterialInstance = new Material(playerShader);
			playerMaterialInstance.name = "ChatFace PlayerMaterial";
		}
		ChatFace.sharedMaterial = playerMaterialInstance;
	}

	private float GetTextHeight(TextRenderer textRenderer)
	{
		if (!textRenderer)
		{
			return 0f;
		}
		TextMeshPro tmp = textRenderer.GetComponentInChildren<TextMeshPro>();
		if ((bool)tmp)
		{
			tmp.ForceMeshUpdate();
			return Mathf.Max(textRenderer.Height, tmp.renderedHeight);
		}
		return textRenderer.Height;
	}

	public override void Reset()
	{
		Xmark.enabled = false;
		votedMark.enabled = false;
		Background.color = Color.white;
	}
}
