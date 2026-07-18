using System.Collections.Generic;
using UnityEngine;

public class LightSource : MonoBehaviour
{
	private class VertInfo
	{
		public float Angle;

		public Vector3 Position;

		internal void Complete(float x, float y)
		{
			Position.x = x;
			Position.y = y;
			Angle = pseudoAngle(y, x);
		}

		internal void Complete(Vector2 point)
		{
			Position.x = point.x;
			Position.y = point.y;
			Angle = pseudoAngle(point.y, point.x);
		}
	}

	private class AngleComparer : IComparer<VertInfo>
	{
		public static readonly AngleComparer Instance = new AngleComparer();

		public int Compare(VertInfo x, VertInfo y)
		{
			if (!(x.Angle > y.Angle))
			{
				if (!(x.Angle < y.Angle))
				{
					return 0;
				}
				return -1;
			}
			return 1;
		}
	}

	private class HitDepthComparer : IComparer<RaycastHit2D>
	{
		public static readonly HitDepthComparer Instance = new HitDepthComparer();

		public int Compare(RaycastHit2D x, RaycastHit2D y)
		{
			if (!(x.fraction > y.fraction))
			{
				return -1;
			}
			return 1;
		}
	}

	public static Dictionary<GameObject, NoShadowBehaviour> NoShadows = new Dictionary<GameObject, NoShadowBehaviour>();

	public static Dictionary<GameObject, OneWayShadows> OneWayShadows = new Dictionary<GameObject, OneWayShadows>();

	[HideInInspector]
	private GameObject child;

	[HideInInspector]
	private Vector2[] requiredDels;

	[HideInInspector]
	private Mesh myMesh;

	public int MinRays = 24;

	public float LightRadius = 3f;

	public Material Material;

	[HideInInspector]
	private List<VertInfo> verts = new List<VertInfo>(256);

	[HideInInspector]
	private int vertCount;

	private RaycastHit2D[] buffer = new RaycastHit2D[128];

	private Collider2D[] hits = new Collider2D[512];

	private ContactFilter2D filter;

	private Vector3[] vec;

	private Vector2[] uvs;

	private int[] triangles = new int[900];

	public float tol = 0.05f;

	private Vector2 del;

	private Vector2 tan;

	private Vector2 side;

	private List<RaycastHit2D> lightHits = new List<RaycastHit2D>();

	public Camera camera;

	private Vector3 lastRenderedPosition = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);

	private float lastRenderedLightRadius = -1f;

	private int lastRenderedFrame = -1000;

	private const float MinMoveBeforeRerenderSqr = 0.0004f;

	private int lightChildLayer = -1;

	private float nextLowShadowUpdateTime;

	private bool hasLowShadowFrame;

	private void Start()
	{
		filter.useTriggers = true;
		filter.layerMask = Constants.ShadowMask;
		filter.useLayerMask = true;
		MinRays = Mathf.Max(MinRays, 128);
		requiredDels = new Vector2[MinRays];
		for (int i = 0; i < requiredDels.Length; i++)
		{
			requiredDels[i] = Vector2.left.Rotate((float)i / (float)requiredDels.Length * 360f);
		}
		myMesh = new Mesh();
		myMesh.MarkDynamic();
		myMesh.name = "ShadowMesh";
		GameObject gameObject = new GameObject("LightChild");
		lightChildLayer = LayerMask.NameToLayer("LightChild");
		if (lightChildLayer >= 0)
		{
			gameObject.layer = lightChildLayer;
		}
		gameObject.AddComponent<MeshFilter>().mesh = myMesh;
		MeshRenderer meshRenderer = gameObject.AddComponent<MeshRenderer>();
		Material = new Material(Material);
		meshRenderer.sharedMaterial = Material;
		child = gameObject;
		HideLightChildFromGameCameras();
	}

	private float GetValidViewDistance()
	{
		if (camera == null)
		{
			camera = Camera.main;
		}
		if (camera == null)
		{
			return LightRadius;
		}
		return Mathf.Min(LightRadius, camera.orthographicSize * camera.aspect);
	}

	public void LateUpdate()
	{
		HideLightChildFromGameCameras();
		if (SaveManager.ShadowQuality <= 0 && hasLowShadowFrame && Time.time < nextLowShadowUpdateTime)
		{
			return;
		}
		Vector3 position = base.transform.position;
		position.z -= 7f;
		child.transform.position = position;
		if (SaveManager.ShadowQuality <= 0)
		{
			hasLowShadowFrame = true;
			nextLowShadowUpdateTime = Time.time + 0.1f;
		}
		else
		{
			hasLowShadowFrame = false;
		}
		Vector2 vector = position;
		float validViewDistance = LightRadius;
		Material.SetFloat("_LightRadius", LightRadius);
		vertCount = 0;
		lastRenderedPosition = position;
		lastRenderedLightRadius = LightRadius;
		lastRenderedFrame = Time.frameCount;
		int num = Physics2D.OverlapCircleNonAlloc(vector, validViewDistance, hits, Constants.ShadowMask);
		for (int i = 0; i < num; i++)
		{
			Collider2D collider2D = hits[i];
			if (collider2D.isTrigger)
			{
				continue;
			}
			EdgeCollider2D edgeCollider2D = collider2D as EdgeCollider2D;
			if ((bool)edgeCollider2D)
			{
				Vector2[] points = edgeCollider2D.points;
				for (int j = 0; j < points.Length; j++)
				{
					Vector2 vector2 = edgeCollider2D.transform.TransformPoint(points[j]);
					del.x = vector2.x - vector.x;
					del.y = vector2.y - vector.y;
					TestBothSides(vector);
				}
				continue;
			}
			PolygonCollider2D polygonCollider2D = collider2D as PolygonCollider2D;
			if ((bool)polygonCollider2D)
			{
				Vector2[] points2 = polygonCollider2D.points;
				for (int k = 0; k < points2.Length; k++)
				{
					Vector2 vector3 = polygonCollider2D.transform.TransformPoint(points2[k]);
					del.x = vector3.x - vector.x;
					del.y = vector3.y - vector.y;
					TestBothSides(vector);
				}
				continue;
			}
			BoxCollider2D boxCollider2D = collider2D as BoxCollider2D;
			if ((bool)boxCollider2D)
			{
				Vector2 vector4 = boxCollider2D.size / 2f;
				Vector2 vector5 = (Vector2)boxCollider2D.transform.TransformPoint(boxCollider2D.offset - vector4) - vector;
				Vector2 vector6 = (Vector2)boxCollider2D.transform.TransformPoint(boxCollider2D.offset + vector4) - vector;
				del.x = vector5.x;
				del.y = vector5.y;
				TestBothSides(vector);
				del.x = vector6.x;
				TestBothSides(vector);
				del.y = vector6.y;
				TestBothSides(vector);
				del.x = vector5.x;
				TestBothSides(vector);
			}
		}
		float num2 = validViewDistance * 1.05f;
		for (int l = 0; l < requiredDels.Length; l++)
		{
			Vector2 vector7 = num2 * requiredDels[l];
			CreateVert(vector, ref vector7);
		}
		verts.Sort(0, vertCount, AngleComparer.Instance);
		myMesh.Clear();
		if (vec == null || vec.Length < vertCount + 1)
		{
			vec = new Vector3[vertCount + 1];
			uvs = new Vector2[vec.Length];
		}
		vec[0] = Vector3.zero;
		uvs[0] = new Vector2(vec[0].x, vec[0].y);
		for (int m = 0; m < vertCount; m++)
		{
			int num3 = m + 1;
			vec[num3] = verts[m].Position;
			uvs[num3] = new Vector2(vec[num3].x, vec[num3].y);
		}
		int num4 = vertCount * 3;
		if (num4 > triangles.Length)
		{
			triangles = new int[num4];
			Debug.LogWarning("Resized triangles to: " + num4);
		}
		int num5 = 0;
		for (int n = 0; n < triangles.Length; n += 3)
		{
			if (n < num4)
			{
				triangles[n] = 0;
				triangles[n + 1] = num5 + 1;
				if (n == num4 - 3)
				{
					triangles[n + 2] = 1;
				}
				else
				{
					triangles[n + 2] = num5 + 2;
				}
				num5++;
			}
			else
			{
				triangles[n] = 0;
				triangles[n + 1] = 0;
				triangles[n + 2] = 0;
			}
		}
		myMesh.vertices = vec;
		myMesh.uv = uvs;
		myMesh.SetIndices(triangles, MeshTopology.Triangles, 0);
	}

	private void HideLightChildFromGameCameras()
	{
		if (lightChildLayer < 0)
		{
			lightChildLayer = LayerMask.NameToLayer("LightChild");
		}
		if (lightChildLayer < 0)
		{
			return;
		}
		Camera[] cameras = Camera.allCameras;
		int lightChildMask = 1 << lightChildLayer;
		for (int i = 0; i < cameras.Length; i++)
		{
			Camera camera = cameras[i];
			if (!camera)
			{
				continue;
			}
			if (camera.GetComponent<ShadowCamera>())
			{
				camera.cullingMask |= lightChildMask;
				continue;
			}
			camera.cullingMask &= ~lightChildMask;
		}
	}

	private bool ShouldRerender(Vector3 position, bool useMiraAndroidOptimization)
	{
		if (!useMiraAndroidOptimization)
		{
			return true;
		}
		if (Mathf.Abs(lastRenderedLightRadius - LightRadius) > 0.001f)
		{
			return true;
		}
		if ((position - lastRenderedPosition).sqrMagnitude > MinMoveBeforeRerenderSqr)
		{
			return true;
		}
		if (Time.frameCount - lastRenderedFrame < 2)
		{
			return false;
		}
		return false;
	}

	private void TestBothSides(Vector2 myPos)
	{
		float num = length(del.x, del.y);
		if (num <= 0.0001f)
		{
			return;
		}
		tan.x = (0f - del.y) / num * tol;
		tan.y = del.x / num * tol;
		side.x = del.x + tan.x;
		side.y = del.y + tan.y;
		CreateVert(myPos, ref side);
		side.x = del.x - tan.x;
		side.y = del.y - tan.y;
		CreateVert(myPos, ref side);
	}

	private void CreateVert(Vector2 myPos, ref Vector2 del)
	{
		float num = Mathf.Min(del.magnitude * 1.05f, LightRadius * 1.5f);
		int num2 = Physics2D.Raycast(myPos, del, filter, buffer, num);
		if (num2 > 0)
		{
			lightHits.Clear();
			RaycastHit2D raycastHit2D = default(RaycastHit2D);
			Collider2D collider2D = null;
			for (int i = 0; i < num2; i++)
			{
				RaycastHit2D raycastHit2D2 = buffer[i];
				Collider2D collider = raycastHit2D2.collider;
				if (!OneWayShadows.TryGetValue(collider.gameObject, out var value) || !value.IsIgnored(this))
				{
					lightHits.Add(raycastHit2D2);
					if (!collider.isTrigger)
					{
						raycastHit2D = raycastHit2D2;
						collider2D = collider;
						break;
					}
				}
			}
			for (int j = 0; j < lightHits.Count; j++)
			{
				if (NoShadows.TryGetValue(lightHits[j].collider.gameObject, out var value2))
				{
					value2.didHit = true;
				}
			}
			if ((bool)collider2D && !collider2D.isTrigger)
			{
				Vector2 point = raycastHit2D.point;
				GetEmptyVert().Complete(point.x - myPos.x, point.y - myPos.y);
				return;
			}
		}
		Vector2 normalized = del.normalized;
		GetEmptyVert().Complete(normalized.x * num, normalized.y * num);
	}

	private VertInfo GetEmptyVert()
	{
		if (vertCount < verts.Count)
		{
			return verts[vertCount++];
		}
		VertInfo vertInfo = new VertInfo();
		verts.Add(vertInfo);
		vertCount = verts.Count;
		return vertInfo;
	}

	private static float length(float x, float y)
	{
		return Mathf.Sqrt(x * x + y * y);
	}

	public static float pseudoAngle(float dx, float dy)
	{
		if (dx < 0f)
		{
			float num = 0f - dx;
			float num2 = ((dy > 0f) ? dy : (0f - dy));
			return 2f - dy / (num + num2);
		}
		float num3 = ((dy > 0f) ? dy : (0f - dy));
		return dy / (dx + num3);
	}
}
