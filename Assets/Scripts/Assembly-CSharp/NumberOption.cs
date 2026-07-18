using TMPro;
using UnityEngine;

public class NumberOption : OptionBehaviour
{
	public TextRenderer TitleText;

	public TextRenderer ValueText;

	public float Value = 1f;

	private float oldValue = float.MaxValue;

	public float Increment;

	public FloatRange ValidRange = new FloatRange(0f, 2f);

	public string FormatString = "{0:0.0}x";

	public bool ZeroIsInfinity;

	private bool buttonsConvertedToTmp;

	public void OnEnable()
	{
		ConvertButtonsToTmp();
		TitleText.Text = DestroyableSingleton<TranslationController>.Instance.GetString(Title);
		ValueText.Text = string.Format(FormatString, Value);
		GameOptionsData gameOptions = PlayerControl.GameOptions;
		switch (Title)
		{
		case StringNames.GamePlayerSpeed:
			Value = gameOptions.PlayerSpeedMod;
			break;
		case StringNames.GameCrewLight:
			Value = gameOptions.CrewLightMod;
			break;
		case StringNames.GameImpostorLight:
			Value = gameOptions.ImpostorLightMod;
			break;
		case StringNames.GameKillCooldown:
			Value = gameOptions.KillCooldown;
			break;
		case StringNames.GameCommonTasks:
			Value = gameOptions.NumCommonTasks;
			break;
		case StringNames.GameLongTasks:
			Value = gameOptions.NumLongTasks;
			break;
		case StringNames.GameShortTasks:
			Value = gameOptions.NumShortTasks;
			break;
		case StringNames.GameNumImpostors:
			Value = gameOptions.NumImpostors;
			break;
		case StringNames.GameNumMeetings:
			Value = gameOptions.NumEmergencyMeetings;
			break;
		case StringNames.GameEmergencyCooldown:
			Value = gameOptions.EmergencyCooldown;
			break;
		case StringNames.GameDiscussTime:
			Value = gameOptions.DiscussionTime;
			break;
		case StringNames.GameVotingTime:
			Value = gameOptions.VotingTime;
			break;
		default:
			Debug.Log("Ono, unrecognized setting: " + Title);
			break;
		}
	}

	private void FixedUpdate()
	{
		if (oldValue != Value)
		{
			oldValue = Value;
			if (ZeroIsInfinity && Mathf.Abs(Value) < 0.0001f)
			{
				ValueText.Text = string.Format(FormatString, "∞");
			}
			else
			{
				ValueText.Text = string.Format(FormatString, Value);
			}
		}
	}

	public void Increase()
	{
		Value = ValidRange.Clamp(Value + Increment);
		OnValueChanged(this);
	}

	public void Decrease()
	{
		Value = ValidRange.Clamp(Value - Increment);
		OnValueChanged(this);
	}

	public override float GetFloat()
	{
		return Value;
	}

	public override int GetInt()
	{
		return (int)Value;
	}

	private void ConvertButtonsToTmp()
	{
		buttonsConvertedToTmp = true;
		Transform[] transforms = GetComponentsInChildren<Transform>(true);
		for (int i = 0; i < transforms.Length; i++)
		{
			Transform child = transforms[i];
			if (child.name == "Plus")
			{
				ConvertButtonToTmp(child, "+");
			}
			else if (child.name == "Minus")
			{
				ConvertButtonToTmp(child, "-");
			}
		}
	}

	public static void ConvertButtonToTmp(Transform button, string text)
	{
		MeshRenderer meshRenderer = button.GetComponent<MeshRenderer>();
		MeshFilter meshFilter = button.GetComponent<MeshFilter>();
		Vector3 textPosition = Vector3.zero;
		if ((bool)meshFilter && (bool)meshFilter.sharedMesh)
		{
			textPosition = meshFilter.sharedMesh.bounds.center;
		}
		else
		{
			BoxCollider2D collider = button.GetComponent<BoxCollider2D>();
			if ((bool)collider)
			{
				textPosition = collider.offset;
			}
		}
		textPosition.z = -0.01f;
		TextMeshPro tmp = button.GetComponentInChildren<TextMeshPro>(true);
		if (!tmp)
		{
			GameObject gameObject = new GameObject("TMP Button Text");
			gameObject.layer = button.gameObject.layer;
			gameObject.transform.SetParent(button, worldPositionStays: false);
			gameObject.transform.localRotation = Quaternion.identity;
			tmp = gameObject.AddComponent<TextMeshPro>();
		}
		float scaleX = Mathf.Abs(button.localScale.x) > 0.0001f ? button.localScale.x : 1f;
		float scaleY = Mathf.Abs(button.localScale.y) > 0.0001f ? button.localScale.y : 1f;
		tmp.transform.localPosition = textPosition;
		tmp.transform.localRotation = Quaternion.identity;
		tmp.transform.localScale = new Vector3(1f / scaleX, 1f / scaleY, 1f);
		TMP_FontAsset font = Resources.Load<TMP_FontAsset>("fonts & materials/LiberationSans SDF");
		Material material = Resources.Load<Material>("fonts & materials/LiberationSans SDF RadialMenu Material");
		if ((bool)font)
		{
			tmp.font = font;
		}
		if ((bool)material)
		{
			Material materialInstance = new Material(material);
			materialInstance.name = material.name + " (" + button.name + ")";
			if (materialInstance.HasProperty("_FaceColor"))
			{
				materialInstance.SetColor("_FaceColor", Color.white);
			}
			if (materialInstance.HasProperty("_OutlineColor"))
			{
				materialInstance.SetColor("_OutlineColor", Color.black);
			}
			if (materialInstance.HasProperty("_OutlineWidth"))
			{
				materialInstance.SetFloat("_OutlineWidth", 0.18f);
			}
			tmp.fontSharedMaterial = materialInstance;
			tmp.fontMaterial = materialInstance;
		}
		tmp.text = text;
		tmp.richText = false;
		tmp.color = Color.white;
		tmp.fontSize = 2.05f;
		tmp.alignment = TextAlignmentOptions.Center;
		tmp.enableWordWrapping = false;
		tmp.rectTransform.pivot = new Vector2(0.5f, 0.5f);
		tmp.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
		tmp.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
		tmp.rectTransform.sizeDelta = new Vector2(0.7f, 0.7f);
		tmp.ForceMeshUpdate();
		MeshRenderer tmpRenderer = tmp.GetComponent<MeshRenderer>();
		if ((bool)tmpRenderer && (bool)meshRenderer)
		{
			tmpRenderer.sortingLayerID = meshRenderer.sortingLayerID;
			tmpRenderer.sortingOrder = meshRenderer.sortingOrder;
		}
		if ((bool)meshRenderer)
		{
			meshRenderer.enabled = false;
		}
	}
}
