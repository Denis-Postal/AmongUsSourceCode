using UnityEngine;

public class ShadowCamera : MonoBehaviour
{
	public Shader Shadozer;

	private Camera shadowCamera;

	private RenderTexture originalTargetTexture;

	private RenderTexture qualityTargetTexture;

	private int appliedQuality = -1;

	public void OnEnable()
	{
		shadowCamera = GetComponent<Camera>();
		if (!originalTargetTexture)
		{
			originalTargetTexture = shadowCamera.targetTexture;
		}
		SetupCamera();
		shadowCamera.SetReplacementShader(Shadozer, "RenderType");
	}

	public void LateUpdate()
	{
		SetupCamera();
	}

	private void SetupCamera()
	{
		if (!shadowCamera)
		{
			shadowCamera = GetComponent<Camera>();
		}
		Camera component = shadowCamera;
		component.clearFlags = CameraClearFlags.SolidColor;
		component.backgroundColor = new Color(0f, 0f, 0f, 0f);
		component.depth = -2f;
		ApplyQuality(component);
		if ((bool)component.targetTexture)
		{
			RenderTexture targetTexture = component.targetTexture;
			component.aspect = (float)targetTexture.width / (float)targetTexture.height;
		}
		component.orthographic = true;
		component.orthographicSize = 7f;
		component.cullingMask = LayerMask.GetMask("Shadow", "Objects", "ShortObjects", "LightChild");
	}

	public void OnDisable()
	{
		GetComponent<Camera>().ResetReplacementShader();
	}

	private void OnDestroy()
	{
		if ((bool)qualityTargetTexture)
		{
			qualityTargetTexture.Release();
			Destroy(qualityTargetTexture);
		}
	}

	public void ApplySavedQuality()
	{
		appliedQuality = -1;
		SetupCamera();
	}

	public static void ApplySavedQualityToAll()
	{
		ShadowCamera[] shadowCameras = FindObjectsOfType<ShadowCamera>();
		for (int i = 0; i < shadowCameras.Length; i++)
		{
			shadowCameras[i].ApplySavedQuality();
		}
	}

	private void ApplyQuality(Camera component)
	{
		if (!originalTargetTexture)
		{
			originalTargetTexture = component.targetTexture;
		}
		int quality = SaveManager.ShadowQuality;
		RenderTexture targetTexture = GetTargetTextureForQuality(quality);
		if (component.targetTexture != targetTexture)
		{
			component.targetTexture = targetTexture;
			UpdateShadowMaterials(targetTexture, quality);
			UpdateLightCutawayMaterials(quality);
		}
		if (appliedQuality != quality)
		{
			appliedQuality = quality;
			UpdateShadowMaterials(targetTexture, quality);
			UpdateLightCutawayMaterials(quality);
		}
	}

	private RenderTexture GetTargetTextureForQuality(int quality)
	{
		if (quality >= 2 || !originalTargetTexture)
		{
			if ((bool)originalTargetTexture)
			{
				originalTargetTexture.filterMode = FilterMode.Bilinear;
			}
			return originalTargetTexture;
		}
		int divisor = (quality == 1) ? 2 : 4;
		int width = Mathf.Max(128, originalTargetTexture.width / divisor);
		int height = Mathf.Max(128, originalTargetTexture.height / divisor);
		if ((bool)qualityTargetTexture && (qualityTargetTexture.width != width || qualityTargetTexture.height != height))
		{
			qualityTargetTexture.Release();
			Destroy(qualityTargetTexture);
			qualityTargetTexture = null;
		}
		if (!qualityTargetTexture)
		{
			qualityTargetTexture = new RenderTexture(width, height, 0, originalTargetTexture.format);
			qualityTargetTexture.name = "ShadowTexture " + ((quality == 1) ? "Medium" : "Low");
			qualityTargetTexture.antiAliasing = 1;
			qualityTargetTexture.useMipMap = false;
			qualityTargetTexture.wrapMode = TextureWrapMode.Clamp;
			qualityTargetTexture.filterMode = FilterMode.Point;
			qualityTargetTexture.Create();
		}
		qualityTargetTexture.filterMode = FilterMode.Point;
		return qualityTargetTexture;
	}

	private static void UpdateShadowMaterials(Texture texture, int quality)
	{
		if (!texture)
		{
			return;
		}
		MeshRenderer[] renderers = FindObjectsOfType<MeshRenderer>();
		for (int i = 0; i < renderers.Length; i++)
		{
			MeshRenderer meshRenderer = renderers[i];
			if (!meshRenderer || meshRenderer.name != "ShadowQuad")
			{
				continue;
			}
			Material material = meshRenderer.sharedMaterial;
			if ((bool)material)
			{
				material.SetTexture("_MainTex", texture);
				if (material.HasProperty("_ShadowPixelSize"))
				{
					float pixelSize = 0f;
					pixelSize = GetPixelSize(quality);
					material.SetFloat("_ShadowPixelSize", pixelSize);
				}
			}
		}
	}

	private static void UpdateLightCutawayMaterials(int quality)
	{
		float pixelSize = GetPixelSize(quality);
		LightSource[] lights = FindObjectsOfType<LightSource>();
		for (int i = 0; i < lights.Length; i++)
		{
			LightSource lightSource = lights[i];
			if (!lightSource || !lightSource.Material || !lightSource.Material.HasProperty("_ShadowPixelSize"))
			{
				continue;
			}
			lightSource.Material.SetFloat("_ShadowPixelSize", pixelSize);
		}
	}

	private static float GetPixelSize(int quality)
	{
		if (quality <= 0)
		{
			return 8f;
		}
		if (quality == 1)
		{
			return 32f;
		}
		return 0f;
	}
}
