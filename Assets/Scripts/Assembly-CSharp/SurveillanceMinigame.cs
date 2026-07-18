using System.Collections;
using System.Linq;
using UnityEngine;

public class SurveillanceMinigame : Minigame
{
	public Camera CameraPrefab;

	public GameObject Viewables;

	public MeshRenderer[] ViewPorts;

	public TextRenderer[] SabText;

	private ShipRoom[] FilteredRooms;

	private RenderTexture[] textures;

	public MeshRenderer FillQuad;

	public Material DefaultMaterial;

	public Material StaticMaterial;

	private bool isStatic;

	public override void Begin(PlayerTask task)
	{
		base.Begin(task);
		FilteredRooms = ShipStatus.Instance.AllRooms.Where((ShipRoom i) => i.survCamera).ToArray();
		textures = new RenderTexture[FilteredRooms.Length];
		for (int num = 0; num < FilteredRooms.Length; num++)
		{
			ShipRoom shipRoom = FilteredRooms[num];
			Camera camera = Object.Instantiate(CameraPrefab);
			camera.transform.SetParent(base.transform);
			camera.transform.position = shipRoom.transform.position + shipRoom.survCamera.Offset;
			camera.orthographicSize = shipRoom.survCamera.CamSize;
			RenderTexture temporary = RenderTexture.GetTemporary((int)(256f * shipRoom.survCamera.CamAspect), 256, 16, RenderTextureFormat.ARGB32);
			textures[num] = temporary;
			camera.targetTexture = temporary;
			ViewPorts[num].material.SetTexture("_MainTex", temporary);
		}
		if (!PlayerControl.LocalPlayer.Data.IsDead)
		{
			ShipStatus.Instance.RpcRepairSystem(SystemTypes.Security, 1);
		}
	}

	public void Update()
	{
		if (isStatic && !PlayerTask.PlayerHasTaskOfType<IHudOverrideTask>(PlayerControl.LocalPlayer))
		{
			isStatic = false;
			for (int i = 0; i < ViewPorts.Length; i++)
			{
				ViewPorts[i].sharedMaterial = DefaultMaterial;
				ViewPorts[i].material.SetTexture("_MainTex", textures[i]);
				SabText[i].gameObject.SetActive(value: false);
			}
		}
		else if (!isStatic && PlayerTask.PlayerHasTaskOfType<HudOverrideTask>(PlayerControl.LocalPlayer))
		{
			isStatic = true;
			for (int j = 0; j < ViewPorts.Length; j++)
			{
				ViewPorts[j].sharedMaterial = StaticMaterial;
				SabText[j].gameObject.SetActive(value: true);
			}
		}
	}

	protected override IEnumerator CoAnimateOpen()
	{
		Viewables.SetActive(value: false);
		FillQuad.material.SetFloat("_Center", -5f);
		FillQuad.material.SetColor("_Color2", Color.clear);
		for (float timer = 0f; timer < 0.25f; timer += Time.deltaTime)
		{
			FillQuad.material.SetColor("_Color2", Color.Lerp(Color.clear, Color.black, timer / 0.25f));
			yield return null;
		}
		FillQuad.material.SetColor("_Color2", Color.black);
		Viewables.SetActive(value: true);
		for (float timer = 0f; timer < 0.1f; timer += Time.deltaTime)
		{
			FillQuad.material.SetFloat("_Center", Mathf.Lerp(-5f, 0f, timer / 0.1f));
			yield return null;
		}
		for (float timer = 0f; timer < 0.15f; timer += Time.deltaTime)
		{
			FillQuad.material.SetFloat("_Center", Mathf.Lerp(-3f, 0.4f, timer / 0.15f));
			yield return null;
		}
		FillQuad.material.SetFloat("_Center", 0.4f);
	}

	private IEnumerator CoAnimateClose()
	{
		for (float timer = 0f; timer < 0.1f; timer += Time.deltaTime)
		{
			FillQuad.material.SetFloat("_Center", Mathf.Lerp(0.4f, -5f, timer / 0.1f));
			yield return null;
		}
		Viewables.SetActive(value: false);
		for (float timer = 0f; timer < 0.3f; timer += Time.deltaTime)
		{
			FillQuad.material.SetColor("_Color2", Color.Lerp(Color.black, Color.clear, timer / 0.3f));
			yield return null;
		}
		FillQuad.material.SetColor("_Color2", Color.clear);
	}

	protected override IEnumerator CoDestroySelf()
	{
		yield return CoAnimateClose();
		Object.Destroy(base.gameObject);
	}

	public override void Close()
	{
		ShipStatus.Instance.RpcRepairSystem(SystemTypes.Security, 2);
		base.Close();
	}

	public void OnDestroy()
	{
		for (int i = 0; i < textures.Length; i++)
		{
			textures[i].Release();
		}
	}
}
