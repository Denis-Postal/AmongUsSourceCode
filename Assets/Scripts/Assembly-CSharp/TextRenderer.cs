using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteInEditMode]
[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(MeshFilter))]
public class TextRenderer : MonoBehaviour
{
	public TextAsset FontData;

	public TextMeshPro TextData;

	public TMP_InputField TextDataInputField;

	public float scale = 1f;

	public float TabWidth = 0.5f;

	public bool Centered;

	public bool RightAligned;

	public TextLink textLinkPrefab;

	[HideInInspector]
	private Mesh mesh;

	[HideInInspector]
	public MeshRenderer render;

	private TextMeshPro createdTmpText;

	private Material tmpMaterialInstance;

	private bool nativeTmpText;

	private bool tmpStateApplied;

	private const float LegacyTmpScaleBoost = 1.12f;

	[Multiline]
	public string Text;

	private string lastText;

	public Color Color = Color.white;

	private Color lastColor = Color.white;

	public bool BlackFont;

	private bool lastBlackFont;

	public Color OutlineColor = Color.black;

	private Color lastOutlineColor = Color.white;

	public float maxWidth = -1f;

	public bool scaleToFit;

	public bool paragraphSpacing;

	private Vector2 cursorLocation;

	public float RealHeight;

	public bool CalculateBounds;

	public float Width { get; private set; }

	public float Height { get; private set; }

	public Vector3 CursorPos
	{
		get
		{
			return new Vector3(cursorLocation.x / 100f * scale, cursorLocation.y / 100f * scale, -0.001f);
		}
	}

	public void Start()
	{
#if UNITY_EDITOR
		if (!Application.isPlaying && IsPrefabAssetObject())
		{
			return;
		}
#endif
		InitializeRenderer();
	}

	private void OnEnable()
	{
#if UNITY_EDITOR
		if (!Application.isPlaying && IsPrefabAssetObject())
		{
			return;
		}
#endif
		if (!Application.isPlaying)
		{
			InitializeRenderer();
		}
	}

#if UNITY_EDITOR
	private void OnValidate()
	{
		if (Application.isPlaying)
		{
			return;
		}
		EditorApplication.delayCall -= RefreshInEditor;
		EditorApplication.delayCall += RefreshInEditor;
	}

	private void RefreshInEditor()
	{
		if (!this)
		{
			return;
		}
		if (IsPrefabAssetObject())
		{
			return;
		}
		InitializeRenderer();
		lastText = null;
		tmpStateApplied = false;
		RefreshMesh();
		EditorUtility.SetDirty(this);
		if ((bool)TextData)
		{
			EditorUtility.SetDirty(TextData);
		}
	}
#endif

	private bool IsPrefabAssetObject()
	{
#if UNITY_EDITOR
		return PrefabUtility.IsPartOfPrefabAsset(base.gameObject);
#else
		return false;
#endif
	}

	private void InitializeRenderer()
	{
		render = GetComponent<MeshRenderer>();
		if (HasNativeTmpText())
		{
			return;
		}
		UseTmpFontForLegacyText();
		if (UseTextMeshPro())
		{
			return;
		}
		MeshFilter component = GetComponent<MeshFilter>();
		Mesh currentMesh = Application.isPlaying ? component.mesh : component.sharedMesh;
		if (!currentMesh)
		{
			mesh = new Mesh();
			mesh.name = "Text" + base.name;
			if (Application.isPlaying)
			{
				component.mesh = mesh;
			}
			else
			{
				component.sharedMesh = mesh;
			}
		}
		else
		{
			mesh = currentMesh;
		}
		UseDefaultFontMaterial();
		SetRendererMaterialColor("_OutlineColor", OutlineColor);
			SetRendererMaterialFloat("_Mask", 0f);
	}

	[ContextMenu("Generate Mesh")]
	public void GenerateMesh()
	{
		render = GetComponent<MeshRenderer>();
		if (HasNativeTmpText())
		{
			return;
		}
		UseTmpFontForLegacyText();
		if (UseTextMeshPro())
		{
			lastText = null;
			lastColor = GetTmpColor();
			return;
		}
		MeshFilter component = GetComponent<MeshFilter>();
		if (!component.sharedMesh)
		{
			mesh = new Mesh();
			mesh.name = "Text" + base.name;
			component.mesh = mesh;
		}
		else
		{
			mesh = component.sharedMesh;
		}
		UseDefaultFontMaterial();
		SetRendererMaterialFloat("_Mask", 0f);
		lastText = null;
		lastOutlineColor = OutlineColor;
		Update();
	}

	private void UseDefaultFontMaterial()
	{
		if (!FontData || !FontCache.Instance)
		{
			return;
		}
		FontCache instance = FontCache.Instance;
		for (int i = 0; i < instance.DefaultFonts.Count && i < instance.DefaultFontMaterials.Count; i++)
		{
			if ((bool)instance.DefaultFonts[i] && (bool)instance.DefaultFontMaterials[i] && instance.DefaultFonts[i].name == FontData.name)
			{
				render.sharedMaterial = instance.DefaultFontMaterials[i];
				return;
			}
		}
	}

	private Material RendererMaterialForWrite()
	{
		if (!render)
		{
			return null;
		}
		return Application.isPlaying ? render.material : render.sharedMaterial;
	}

	private Material TmpMaterialForWrite(TextMeshPro textMeshPro)
	{
		if (!textMeshPro)
		{
			return null;
		}
		return Application.isPlaying ? textMeshPro.fontMaterial : textMeshPro.fontSharedMaterial;
	}

	private void SetRendererMaterialColor(string property, Color value)
	{
		Material material = RendererMaterialForWrite();
		if ((bool)material && material.HasProperty(property))
		{
			material.SetColor(property, value);
		}
	}

	private void SetRendererMaterialFloat(string property, float value)
	{
		Material material = RendererMaterialForWrite();
		if ((bool)material && material.HasProperty(property))
		{
			material.SetFloat(property, value);
		}
	}

	private void Update()
	{
		if (HasNativeTmpText())
		{
			return;
		}
		UseTmpFontForLegacyText();
		if (UseTextMeshPro())
		{
			return;
		}
		if (lastOutlineColor != OutlineColor)
		{
			lastOutlineColor = OutlineColor;
			SetRendererMaterialColor("_OutlineColor", OutlineColor);
		}
		if (lastText != Text || lastColor != GetTmpColor() || lastBlackFont != BlackFont)
		{
			RefreshMesh();
		}
	}

	public void RefreshMesh()
	{
		if (HasNativeTmpText())
		{
			return;
		}
		if (render == null)
		{
			render = GetComponent<MeshRenderer>();
		}
		UseTmpFontForLegacyText();
		if (UseTextMeshPro())
		{
			return;
		}
		if (mesh == null)
		{
			MeshFilter component = GetComponent<MeshFilter>();
			if ((bool)component)
			{
				mesh = component.mesh;
				if (!mesh)
				{
					mesh = new Mesh();
					mesh.name = "Text" + base.name;
					component.mesh = mesh;
				}
			}
		}
		if (mesh == null)
		{
			return;
		}
		if (Text != null && Text.Any((char c) => c > '✐'))
		{
			FontCache.Instance.SetFont(this, "Korean");
			SetRendererMaterialColor("_OutlineColor", OutlineColor);
		}
		if (Text == null)
		{
			Text = string.Empty;
		}
		FontData fontData = (FontCache.Instance ? FontCache.Instance.LoadFont(FontData) : FontCache.LoadFontUncached(FontData));
		lastText = Text;
		lastColor = Color;
		if (maxWidth > 0f)
		{
			lastText = (Text = WrapText(fontData, lastText, maxWidth));
		}
		List<Vector3> list = new List<Vector3>(lastText.Length * 4);
		List<Vector2> list2 = new List<Vector2>(lastText.Length * 4);
		List<Vector4> list3 = new List<Vector4>(lastText.Length * 4);
		List<Color> list4 = new List<Color>(lastText.Length * 4);
		int[] array = new int[lastText.Length * 6];
		Width = 0f;
		cursorLocation.x = (cursorLocation.y = 0f);
		int num = -1;
		Vector2 vector = default(Vector2);
		string text = null;
		int lineStart = 0;
		int num2 = 0;
		Color item = Color;
		int? num3 = null;
		for (int num4 = 0; num4 < lastText.Length; num4++)
		{
			int num5 = lastText[num4];
			if (num5 == 91)
			{
				num3 = 0;
				num = num5;
				continue;
			}
			if (num3.HasValue)
			{
				switch (num5)
				{
				case 93:
					if (num != 91)
					{
						item = new Color32((byte)((num3 >> 24) & 0xFF).Value, (byte)((num3 >> 16) & 0xFF).Value, (byte)((num3 >> 8) & 0xFF).Value, (byte)(num3 & 0xFF).Value);
						item.a *= Color.a;
					}
					else
					{
						item = Color;
					}
					num = -1;
					num3 = null;
					if (text != null)
					{
						TextLink textLink = Object.Instantiate(textLinkPrefab, base.transform);
						textLink.transform.localScale = Vector3.one;
						Vector3 vector2 = list.Last();
						textLink.Set(vector, vector2, text);
						text = null;
					}
					break;
				case 104:
				{
					int num6 = lastText.IndexOf(']', num4);
					text = lastText.Substring(num4, num6 - num4);
					vector = list[list.Count - 2];
					item = new Color(0.5f, 0.5f, 1f);
					num = -1;
					num3 = null;
					num4 = num6;
					break;
				}
				default:
					num3 = (num3 << 4) | CharToInt(num5);
					break;
				}
				num = num5;
				continue;
			}
			switch (num5)
			{
			case 10:
				if (Centered)
				{
					CenterVerts(list, cursorLocation.x, lineStart);
				}
				else if (RightAligned)
				{
					RightAlignVerts(list, cursorLocation.x, lineStart);
				}
				cursorLocation.x = 0f;
				cursorLocation.y -= fontData.LineHeight;
				lineStart = list.Count;
				continue;
			case 13:
				continue;
			}
			int value;
			if (!fontData.charMap.TryGetValue(num5, out value))
			{
				num5 = -1;
				value = fontData.charMap[-1];
			}
			Vector4 vector3 = fontData.bounds[value];
			Vector2 textureSize = fontData.TextureSize;
			Vector3 vector4 = fontData.offsets[value];
			float kerning = fontData.GetKerning(num, num5);
			float num7 = cursorLocation.x + vector4.x + kerning;
			float num8 = cursorLocation.y - vector4.y;
			list.Add(new Vector3(num7, num8 - vector3.w) / 100f * scale);
			list.Add(new Vector3(num7, num8) / 100f * scale);
			list.Add(new Vector3(num7 + vector3.z, num8) / 100f * scale);
			list.Add(new Vector3(num7 + vector3.z, num8 - vector3.w) / 100f * scale);
			list4.Add(item);
			list4.Add(item);
			list4.Add(item);
			list4.Add(item);
			list2.Add(new Vector2(vector3.x / textureSize.x, 1f - (vector3.y + vector3.w) / textureSize.y));
			list2.Add(new Vector2(vector3.x / textureSize.x, 1f - vector3.y / textureSize.y));
			list2.Add(new Vector2((vector3.x + vector3.z) / textureSize.x, 1f - vector3.y / textureSize.y));
			list2.Add(new Vector2((vector3.x + vector3.z) / textureSize.x, 1f - (vector3.y + vector3.w) / textureSize.y));
			Vector4 item2 = fontData.Channels[value];
			list3.Add(item2);
			list3.Add(item2);
			list3.Add(item2);
			list3.Add(item2);
			array[num2 * 6] = num2 * 4;
			array[num2 * 6 + 1] = num2 * 4 + 1;
			array[num2 * 6 + 2] = num2 * 4 + 2;
			array[num2 * 6 + 3] = num2 * 4;
			array[num2 * 6 + 4] = num2 * 4 + 2;
			array[num2 * 6 + 5] = num2 * 4 + 3;
			cursorLocation.x += vector4.z + kerning;
			float num9 = cursorLocation.x / 100f * scale;
			if (Width < num9)
			{
				Width = num9;
			}
			num = num5;
			num2++;
		}
		if (Centered)
		{
			CenterVerts(list, cursorLocation.x, lineStart);
			cursorLocation.x /= 2f;
			Width /= 2f;
		}
		else if (RightAligned)
		{
			RightAlignVerts(list, cursorLocation.x, lineStart);
		}
		Height = (0f - (cursorLocation.y - fontData.LineHeight)) / 100f * scale;
		RealHeight = Height;
		mesh.Clear();
		if (list.Count > 0)
		{
			mesh.SetVertices(list);
			mesh.SetColors(list4);
			mesh.SetUVs(0, list2);
			mesh.SetUVs(1, list3);
			mesh.SetIndices(array, MeshTopology.Triangles, 0);
		}
	}

	private bool UseTextMeshPro()
	{
		if (!TextData)
		{
			return false;
		}
		if ((bool)render)
		{
			render.enabled = false;
		}
		if (HasManagedTmpText())
		{
			EnsureTmpFontMatchesText();
			EnsureTmpMaterialInstance(TextData);
			ResetTmpMaterialFaceColor(tmpMaterialInstance);
		}
		if (string.IsNullOrEmpty(Text) && !string.IsNullOrEmpty(TextData.text))
		{
			Text = TextData.text;
		}
		if (lastText != Text)
		{
			lastText = Text;
			TextData.richText = true;
			TextData.text = ConvertLegacyColorTagsToTmp(Text);
		}
		Color tmpColor = GetTmpColor();
		TextData.color = tmpColor;
		if (!tmpStateApplied || lastColor != tmpColor || lastBlackFont != BlackFont)
		{
			lastColor = tmpColor;
			lastBlackFont = BlackFont;
		}
		if (!tmpStateApplied || lastOutlineColor != OutlineColor)
		{
			lastOutlineColor = OutlineColor;
			ApplyTmpOutline(TextData);
		}
		tmpStateApplied = true;
		TextData.alignment = RightAligned ? TextAlignmentOptions.Right : (Centered ? TextAlignmentOptions.Center : TextAlignmentOptions.Left);
		TextData.enableWordWrapping = maxWidth > 0f;
		if (HasManagedTmpText())
		{
			ApplyLegacyTmpLayout(TextData);
		}
		TextData.ForceMeshUpdate();
		Width = TextData.renderedWidth;
		Height = TextData.renderedHeight;
		RealHeight = Height;
		return true;
	}

	private void EnsureTmpFontMatchesText()
	{
		TMP_FontAsset fontAsset = GetTmpFontAsset();
		if (!fontAsset || TextData.font == fontAsset)
		{
			return;
		}
		Material material = GetTmpFontMaterial(fontAsset);
		if (!material)
		{
			return;
		}
		if (!TryAssignTmpFont(TextData, fontAsset))
		{
			return;
		}
		SetTmpMaterialInstance(material);
		ApplyTmpOutline(TextData);
	}

	private void UseTmpFontForLegacyText()
	{
		if (HasNativeTmpText() || (bool)TextData || (bool)createdTmpText || !ShouldUseTmpFontForLegacyText())
		{
			return;
		}
#if UNITY_EDITOR
		if (!Application.isPlaying && PrefabUtility.IsPartOfPrefabAsset(base.gameObject))
		{
			return;
		}
#endif
		TMP_FontAsset fontAsset = GetTmpFontAsset();
		Material material = GetTmpFontMaterial(fontAsset);
		if (!fontAsset || !material)
		{
			return;
		}
		GameObject gameObject = new GameObject("TMP Legacy Text");
		gameObject.layer = base.gameObject.layer;
		gameObject.transform.SetParent(base.transform, worldPositionStays: false);
		gameObject.transform.localPosition = Vector3.zero;
		gameObject.transform.localRotation = Quaternion.identity;
		gameObject.transform.localScale = Vector3.one;
		TextMeshPro textMeshPro = gameObject.AddComponent<TextMeshPro>();
		if (!TryAssignTmpFont(textMeshPro, fontAsset))
		{
			DestroyTmpObject(gameObject);
			return;
		}
		SetTmpMaterialInstance(textMeshPro, material);
		textMeshPro.richText = true;
		textMeshPro.color = GetTmpColor();
		ApplyTmpOutline(textMeshPro);
		ApplyLegacyTmpLayout(textMeshPro);
		TextData = textMeshPro;
		createdTmpText = textMeshPro;
		lastOutlineColor = OutlineColor;
		lastColor = GetTmpColor();
		lastBlackFont = BlackFont;
		tmpStateApplied = true;
		if ((bool)render)
		{
			render.enabled = false;
		}
	}

	private void ApplyLegacyTmpLayout(TextMeshPro textMeshPro)
	{
		textMeshPro.fontSize = Mathf.Max(0.1f, 2.2f * scale * LegacyTmpScaleBoost);
		textMeshPro.fontStyle = FontStyles.Normal;
		textMeshPro.enableKerning = true;
		textMeshPro.enableWordWrapping = maxWidth > 0f;
		ApplyTmpOutline(textMeshPro);
		textMeshPro.alignment = RightAligned ? TextAlignmentOptions.TopRight : (Centered ? TextAlignmentOptions.Top : TextAlignmentOptions.TopLeft);
		RectTransform rectTransform = textMeshPro.rectTransform;
		float width = (maxWidth > 0f) ? maxWidth : Mathf.Max(0.1f, textMeshPro.GetPreferredValues(textMeshPro.text).x);
		float height = Mathf.Max(0.1f, textMeshPro.GetPreferredValues(textMeshPro.text, width, 0f).y);
		float pivotX = RightAligned ? 1f : (Centered ? 0.5f : 0f);
		rectTransform.pivot = new Vector2(pivotX, 1f);
		rectTransform.anchorMin = new Vector2(pivotX, 1f);
		rectTransform.anchorMax = new Vector2(pivotX, 1f);
		rectTransform.sizeDelta = new Vector2(width, height);
		rectTransform.localPosition = Vector3.zero;
		rectTransform.localRotation = Quaternion.identity;
		rectTransform.localScale = Vector3.one;
		SyncTmpRenderer(textMeshPro);
	}

	private bool ShouldUseTmpFontForLegacyText()
	{
		return (bool)FontData && !TextDataInputField;
	}

	private TMP_FontAsset GetTmpFontAsset()
	{
		string fontName = FontData ? FontData.name : string.Empty;
		if (IsLegacyMonoFont(fontName))
		{
			TMP_FontAsset monoFont = Resources.Load<TMP_FontAsset>("fonts & materials/ConsolaMono-Book SDF");
			if (IsUsableTmpFontAsset(monoFont) && FontCanRenderCurrentText(monoFont))
			{
				return monoFont;
			}
			TMP_FontAsset fallbackFont = LoadLiberationTmpFont();
			if (IsUsableTmpFontAsset(fallbackFont))
			{
				return fallbackFont;
			}
		}
		if (fontName == "Korean" || fontName == "NotoSansSC-VariableFont_wght" || TextNeedsNotoFont())
		{
			TMP_FontAsset notoFont = Resources.Load<TMP_FontAsset>("fonts & materials/NotoSansRU SDF");
			if (IsUsableTmpFontAsset(notoFont) && FontCanRenderCurrentText(notoFont))
			{
				return notoFont;
			}
		}
		TMP_FontAsset liberationFont = LoadLiberationTmpFont();
		return IsUsableTmpFontAsset(liberationFont) ? liberationFont : null;
	}

	private bool TryAssignTmpFont(TextMeshPro textMeshPro, TMP_FontAsset fontAsset)
	{
		if (!textMeshPro || !IsUsableTmpFontAsset(fontAsset))
		{
			return false;
		}
		try
		{
			textMeshPro.font = fontAsset;
			return true;
		}
		catch (System.Exception)
		{
			return false;
		}
	}

	private void DestroyTmpObject(GameObject target)
	{
		if (!target)
		{
			return;
		}
		if (Application.isPlaying)
		{
			Destroy(target);
		}
		else
		{
			DestroyImmediate(target);
		}
	}

	private bool IsUsableTmpFontAsset(TMP_FontAsset fontAsset)
	{
		return fontAsset && fontAsset.atlasTexture && fontAsset.material && fontAsset.material.GetTexture(ShaderUtilities.ID_MainTex);
	}

	private Material GetTmpFontMaterial(TMP_FontAsset fontAsset)
	{
		string fontName = FontData ? FontData.name : string.Empty;
		if ((bool)fontAsset && fontAsset.name == "ConsolaMono-Book SDF" && IsLegacyMonoFont(fontName))
		{
			Material monoMaterial = Resources.Load<Material>("fonts & materials/ConsolaMono-Book Atlas Material");
			if ((bool)monoMaterial)
			{
				return monoMaterial;
			}
		}
		if (fontName == "Korean" || fontName == "NotoSansSC-VariableFont_wght" || TextNeedsNotoFont())
		{
			Material notoMaterial = Resources.Load<Material>("fonts & materials/NotoSans-VariableFont_wdth_wght Atlas Material");
			if ((bool)notoMaterial)
			{
				return notoMaterial;
			}
		}
		Material liberationMaterial = Resources.Load<Material>("fonts & materials/LiberationSans SDF RadialMenu Material");
		if ((bool)liberationMaterial)
		{
			return liberationMaterial;
		}
		liberationMaterial = Resources.Load<Material>("fonts & materials/LiberationSans SDF Material");
		if ((bool)liberationMaterial)
		{
			return liberationMaterial;
		}
		return fontAsset ? fontAsset.material : null;
	}

	private TMP_FontAsset LoadLiberationTmpFont()
	{
		return Resources.Load<TMP_FontAsset>("fonts & materials/LiberationSans SDF");
	}

	private static bool IsLegacyMonoFont(string fontName)
	{
		return fontName == "Consolas" || fontName == "Digital7" || fontName == "OCR-A-Extended";
	}

	private bool FontCanRenderCurrentText(TMP_FontAsset fontAsset)
	{
		if (!fontAsset)
		{
			return false;
		}
		string value = !string.IsNullOrEmpty(Text) ? Text : (TextData ? TextData.text : string.Empty);
		if (string.IsNullOrEmpty(value))
		{
			return true;
		}
		value = Regex.Replace(value, "\\[([0-9a-fA-F]{6}|[0-9a-fA-F]{8})\\]|\\[\\]", string.Empty);
		value = Regex.Replace(value, "<font=\"LiberationSans SDF\">\\*+</font>", string.Empty);
		value = Regex.Replace(value, "<[^>]+>", string.Empty);
		for (int i = 0; i < value.Length; i++)
		{
			char c = value[i];
			if (char.IsWhiteSpace(c))
			{
				continue;
			}
			if (!fontAsset.HasCharacter(c, searchFallbacks: false))
			{
				return false;
			}
		}
		return true;
	}

	private Material CreateTmpMaterialInstance(Material source)
	{
		Material material = new Material(source);
		material.name = source.name + " (" + base.name + ")";
		ResetTmpMaterialFaceColor(material);
		ApplyTmpOutline(material);
		return material;
	}

	private void SetTmpMaterialInstance(Material source)
	{
		SetTmpMaterialInstance(TextData, source);
	}

	private void SetTmpMaterialInstance(TextMeshPro textMeshPro, Material source)
	{
		if (!textMeshPro || !source)
		{
			return;
		}
		tmpMaterialInstance = CreateTmpMaterialInstance(source);
		textMeshPro.fontSharedMaterial = tmpMaterialInstance;
		if (Application.isPlaying)
		{
			textMeshPro.fontMaterial = tmpMaterialInstance;
		}
	}

	private void EnsureTmpMaterialInstance(TextMeshPro textMeshPro)
	{
		if (!textMeshPro)
		{
			return;
		}
		Material material = GetTmpFontMaterial(textMeshPro.font);
		if (!material)
		{
			material = textMeshPro.fontSharedMaterial;
		}
		if (!material)
		{
			return;
		}
		if ((bool)tmpMaterialInstance && MaterialComesFrom(tmpMaterialInstance, material) && tmpMaterialInstance.shader == material.shader)
		{
			SyncTmpRenderer(textMeshPro);
			return;
		}
		SetTmpMaterialInstance(textMeshPro, material);
	}

	private bool MaterialComesFrom(Material instance, Material source)
	{
		if (!instance || !source)
		{
			return false;
		}
		return instance.name == source.name || instance.name.StartsWith(source.name + " (");
	}

	private void SyncTmpRenderer(TextMeshPro textMeshPro)
	{
		MeshRenderer tmpRenderer = textMeshPro ? textMeshPro.GetComponent<MeshRenderer>() : null;
		if (!tmpRenderer)
		{
			return;
		}
		tmpRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
		tmpRenderer.receiveShadows = false;
		if ((bool)render)
		{
			tmpRenderer.sortingLayerID = render.sortingLayerID;
			tmpRenderer.sortingOrder = render.sortingOrder;
		}
	}

	private void ApplyTmpOutline(TextMeshPro textMeshPro)
	{
		if (!textMeshPro)
		{
			return;
		}
		if (Application.isPlaying)
		{
			textMeshPro.outlineColor = OutlineColor;
			textMeshPro.outlineWidth = GetTmpOutlineWidth();
		}
		if ((bool)textMeshPro.fontSharedMaterial)
		{
			if (textMeshPro.fontSharedMaterial == tmpMaterialInstance)
			{
				ApplyTmpOutline(tmpMaterialInstance);
			}
		}
	}

	private void ApplyTmpOutline(Material material)
	{
		if (!material)
		{
			return;
		}
		ResetTmpMaterialFaceColor(material);
		if (material.HasProperty("_OutlineColor"))
		{
			material.SetColor("_OutlineColor", OutlineColor);
		}
		if (material.HasProperty("_OutlineWidth"))
		{
			material.SetFloat("_OutlineWidth", GetTmpOutlineWidth());
		}
	}

	private void ResetTmpMaterialFaceColor(Material material)
	{
		if (!material)
		{
			return;
		}
		if (material.HasProperty("_FaceColor"))
		{
			material.SetColor("_FaceColor", Color.white);
		}
		if (material.HasProperty("_Color"))
		{
			material.SetColor("_Color", Color.white);
		}
		material.SetFloat("_ZWrite", 1f);
	}

	private float GetTmpOutlineWidth()
	{
		return (OutlineColor.a <= 0f) ? 0f : 0.18f;
	}

	private Color GetTmpColor()
	{
		if (BlackFont)
		{
			return new Color(0f, 0f, 0f, Color.a);
		}
		return Color;
	}

	private bool TextNeedsNotoFont()
	{
		if (string.IsNullOrEmpty(Text))
		{
			return false;
		}
		for (int i = 0; i < Text.Length; i++)
		{
			char c = Text[i];
			if ((c >= '\u0400' && c <= '\u04FF') || (c >= '\u1100' && c <= '\u11FF') || (c >= '\u3040' && c <= '\u30FF') || (c >= '\u3400' && c <= '\u9FFF') || (c >= '\uAC00' && c <= '\uD7AF'))
			{
				return true;
			}
		}
		return false;
	}

	private bool HasNativeTmpText()
	{
		if (nativeTmpText)
		{
			return true;
		}
		TextMeshPro component = GetComponent<TextMeshPro>();
		nativeTmpText = (bool)component && component != createdTmpText && TextData == null;
		return nativeTmpText;
	}

	private bool HasManagedTmpText()
	{
		return (bool)TextData && ((bool)createdTmpText || (bool)FontData);
	}

	public void SetShadowClipEnabled(bool enabled)
	{
		float value = enabled ? 1f : 0f;
		if ((bool)render)
		{
			SetRendererMaterialFloat("_PlayerShadowClipEnabled", value);
		}
		if ((bool)tmpMaterialInstance)
		{
			tmpMaterialInstance.SetFloat("_PlayerShadowClipEnabled", value);
		}
		if ((bool)TextData && (bool)TextData.fontSharedMaterial && TextData.fontSharedMaterial == tmpMaterialInstance)
		{
			TextData.fontSharedMaterial.SetFloat("_PlayerShadowClipEnabled", value);
		}
		TextMeshPro[] componentsInChildren = GetComponentsInChildren<TextMeshPro>(true);
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			Material fontMaterial = TmpMaterialForWrite(componentsInChildren[i]);
			if ((bool)fontMaterial)
			{
				fontMaterial.SetFloat("_PlayerShadowClipEnabled", value);
			}
		}
	}

	public void SetClipRect(bool enabled, Vector4 clipRect)
	{
		ApplyClipRect(RendererMaterialForWrite(), enabled, clipRect);
		ApplyClipRect(tmpMaterialInstance, enabled, clipRect);
		if ((bool)TextData)
		{
			ApplyClipRect(TmpMaterialForWrite(TextData), enabled, clipRect);
			if (TextData.fontSharedMaterial == tmpMaterialInstance)
			{
				ApplyClipRect(TextData.fontSharedMaterial, enabled, clipRect);
			}
		}
		TextMeshPro[] componentsInChildren = GetComponentsInChildren<TextMeshPro>(true);
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			ApplyClipRect(TmpMaterialForWrite(componentsInChildren[i]), enabled, clipRect);
			if (componentsInChildren[i].fontSharedMaterial == tmpMaterialInstance)
			{
				ApplyClipRect(componentsInChildren[i].fontSharedMaterial, enabled, clipRect);
			}
		}
	}

	public static void ApplyClipRect(Material material, bool enabled, Vector4 clipRect)
	{
		if (!material)
		{
			return;
		}
		if (material.HasProperty("_UseClipRect"))
		{
			material.SetFloat("_UseClipRect", enabled ? 1f : 0f);
		}
		if (material.HasProperty("_ClipRect"))
		{
			material.SetVector("_ClipRect", clipRect);
		}
	}

	private static string ConvertLegacyColorTagsToTmp(string text)
	{
		if (string.IsNullOrEmpty(text))
		{
			return string.Empty;
		}
		text = Regex.Replace(text, "\\[([0-9a-fA-F]{6}|[0-9a-fA-F]{8})\\]", "<color=#$1>");
		text = text.Replace("[]", "</color>");
		text = text.Replace("\u2022", "\u00B7");
		text = text.Replace("●", "·");
		return text;
	}

	private void RightAlignVerts(List<Vector3> verts, float baseX, int lineStart)
	{
		for (int i = lineStart; i < verts.Count; i++)
		{
			Vector3 value = verts[i];
			value.x -= baseX / 100f * scale;
			verts[i] = value;
		}
	}

	private void CenterVerts(List<Vector3> verts, float baseX, int lineStart)
	{
		for (int i = lineStart; i < verts.Count; i++)
		{
			Vector3 value = verts[i];
			value.x -= baseX / 200f * scale;
			verts[i] = value;
		}
	}

	private int CharToInt(int c)
	{
		if (c < 65)
		{
			return c - 48;
		}
		if (c < 97)
		{
			return 10 + (c - 65);
		}
		return 10 + (c - 97);
	}

	public static string WrapText(FontData data, string displayTxt, float maxWidth)
	{
		float num = 0f;
		int num2 = -1;
		int last = -1;
		bool flag = false;
		int num3 = 0;
		for (int i = 0; i < displayTxt.Length && num3++ <= 1000; i++)
		{
			int num4 = displayTxt[i];
			switch (num4)
			{
			case 91:
				flag = true;
				break;
			case 93:
				flag = false;
				continue;
			}
			if (flag)
			{
				continue;
			}
			switch (num4)
			{
			case 10:
				num2 = -1;
				last = -1;
				num = 0f;
				continue;
			case 13:
				continue;
			}
			int value;
			if (!data.charMap.TryGetValue(num4, out value))
			{
				num4 = -1;
				value = data.charMap[-1];
			}
			if (num4 == 32)
			{
				num2 = i;
			}
			num += data.offsets[value].z + data.GetKerning(last, num4);
			if (num > maxWidth * 100f)
			{
				if (num2 != -1)
				{
					displayTxt = displayTxt.Substring(0, num2) + "\n" + displayTxt.Substring(num2 + 1);
					i = num2;
				}
				else
				{
					displayTxt = displayTxt.Substring(0, i) + "\n" + displayTxt.Substring(i);
				}
				num2 = -1;
				last = -1;
				num = 0f;
			}
			last = num4;
		}
		return displayTxt;
	}
}
