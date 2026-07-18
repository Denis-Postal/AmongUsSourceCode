using UnityEngine;

public class CloudGenerator : MonoBehaviour
{
	private struct Cloud
	{
		public int CloudIdx;

		public float Rate;

		public float Size;

		public float FlipX;

		public float PositionX;

		public float PositionY;

		public float PositionZ;
	}

	public Sprite[] CloudImages;

	private Vector2[] ExtentCache;

	public int NumClouds = 500;

	public float Length = 25f;

	public float Width = 25f;

	public Vector2 Direction = new Vector2(1f, 0f);

	private Vector2 NormDir = new Vector2(1f, 0f);

	private Vector2 Tangent = new Vector2(0f, 1f);

	private float tanLen;

	public FloatRange Rates = new FloatRange(0.25f, 1f);

	public FloatRange Sizes = new FloatRange(0.75f, 1.25f);

	public bool Depth;

	public float MaxDepth = 4f;

	public float ParallaxOffset;

	public float ParallaxStrength = 1f;

	[HideInInspector]
	private Cloud[] clouds;

	[HideInInspector]
	private Vector3[] verts;

	[HideInInspector]
	private Mesh mesh;

	public void Start()
	{
		Vector2[][] array = new Vector2[CloudImages.Length][];
		ExtentCache = new Vector2[CloudImages.Length];
		for (int i = 0; i < CloudImages.Length; i++)
		{
			Sprite sprite = CloudImages[i];
			array[i] = sprite.uv;
			ExtentCache[i] = sprite.bounds.extents;
		}
		clouds = new Cloud[NumClouds];
		verts = new Vector3[NumClouds * 4];
		Vector2[] array2 = new Vector2[NumClouds * 4];
		int[] array3 = new int[NumClouds * 6];
		SetDirection(Direction);
		MeshFilter component = GetComponent<MeshFilter>();
		mesh = new Mesh();
		mesh.MarkDynamic();
		component.mesh = mesh;
		Vector3 vector = default(Vector3);
		for (int j = 0; j < clouds.Length; j++)
		{
			Cloud cloud = clouds[j];
			int num = (cloud.CloudIdx = CloudImages.RandomIdx());
			Vector2 vector2 = ExtentCache[num];
			Vector2[] array4 = array[num];
			float num2 = FloatRange.Next(-1f, 1f) * Length;
			float num3 = FloatRange.Next(-1f, 1f) * Width;
			float num4 = (cloud.PositionX = num2 * NormDir.x + num3 * Tangent.x);
			float num5 = (cloud.PositionY = num2 * NormDir.y + num3 * Tangent.y);
			cloud.Rate = Rates.Next();
			cloud.Size = Sizes.Next();
			cloud.FlipX = ((!BoolRange.Next()) ? 1 : (-1));
			if (Depth)
			{
				cloud.PositionZ = FloatRange.Next(0f, MaxDepth);
			}
			vector2 *= cloud.Size;
			clouds[j] = cloud;
			int num6 = j * 4;
			vector.x = num4 - vector2.x * cloud.FlipX;
			vector.y = num5 + vector2.y;
			vector.z = cloud.PositionZ;
			verts[num6] = vector;
			vector.x = num4 + vector2.x * cloud.FlipX;
			verts[num6 + 1] = vector;
			vector.x = num4 - vector2.x * cloud.FlipX;
			vector.y = num5 - vector2.y;
			verts[num6 + 2] = vector;
			vector.x = num4 + vector2.x * cloud.FlipX;
			verts[num6 + 3] = vector;
			array2[num6] = array4[0];
			array2[num6 + 1] = array4[1];
			array2[num6 + 2] = array4[2];
			array2[num6 + 3] = array4[3];
			int num7 = j * 6;
			array3[num7] = num6;
			array3[num7 + 1] = num6 + 1;
			array3[num7 + 2] = num6 + 2;
			array3[num7 + 3] = num6 + 2;
			array3[num7 + 4] = num6 + 1;
			array3[num7 + 5] = num6 + 3;
		}
		mesh.vertices = verts;
		mesh.uv = array2;
		mesh.SetIndices(array3, MeshTopology.Triangles, 0);
	}

	private void FixedUpdate()
	{
		float num = -0.99f * Length;
		Vector2 vector = Direction * Time.fixedDeltaTime;
		Vector3 vector2 = default(Vector3);
		for (int i = 0; i < clouds.Length; i++)
		{
			int num2 = i * 4;
			Cloud cloud = clouds[i];
			float positionX = cloud.PositionX;
			float positionY = cloud.PositionY;
			Vector2 vector3 = ExtentCache[cloud.CloudIdx];
			vector3 *= cloud.Size;
			float rate = cloud.Rate;
			positionX += rate * vector.x;
			positionY += rate * vector.y;
			if (OrthoDistance(positionX, positionY) > Length)
			{
				float num3 = FloatRange.Next(-1f, 1f) * Width;
				positionX = num * NormDir.x + num3 * Tangent.x;
				positionY = num * NormDir.y + num3 * Tangent.y;
				cloud.Rate = Rates.Next();
				cloud.Size = Sizes.Next();
				cloud.FlipX = ((!BoolRange.Next()) ? 1 : (-1));
				if (Depth)
				{
					cloud.PositionZ = FloatRange.Next(0f, MaxDepth);
				}
			}
			cloud.PositionX = positionX;
			cloud.PositionY = positionY;
			if (Depth)
			{
				positionY += (base.transform.position.y + ParallaxOffset) / (cloud.PositionZ * ParallaxStrength + 0.0001f);
				vector2.z = cloud.PositionZ;
			}
			clouds[i] = cloud;
			vector2.x = positionX - vector3.x * cloud.FlipX;
			vector2.y = positionY + vector3.y;
			verts[num2] = vector2;
			vector2.x = positionX + vector3.x * cloud.FlipX;
			verts[num2 + 1] = vector2;
			vector2.x = positionX - vector3.x * cloud.FlipX;
			vector2.y = positionY - vector3.y;
			verts[num2 + 2] = vector2;
			vector2.x = positionX + vector3.x * cloud.FlipX;
			verts[num2 + 3] = vector2;
		}
		mesh.vertices = verts;
	}

	public void SetDirection(Vector2 dir)
	{
		Direction = dir;
		NormDir = Direction.normalized;
		Tangent = new Vector2(0f - NormDir.y, NormDir.x);
		tanLen = Mathf.Sqrt(Tangent.y * Tangent.y + Tangent.x * Tangent.x);
	}

	private float OrthoDistance(float pointx, float pointy)
	{
		return (Tangent.y * pointx - Tangent.x * pointy) / tanLen;
	}
}
