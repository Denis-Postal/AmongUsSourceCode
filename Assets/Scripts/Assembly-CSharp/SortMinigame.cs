using UnityEngine;

public class SortMinigame : Minigame
{
	public SortGameObject[] Objects;

	public BoxCollider2D AnimalBox;

	public BoxCollider2D PlantBox;

	public BoxCollider2D MineralBox;

	public AudioClip[] PickUpSounds;

	public AudioClip[] DropSounds;

	private Controller myController = new Controller();

	public void Start()
	{
		Objects.Shuffle();
		for (int i = 0; i < Objects.Length; i++)
		{
			SortGameObject sortGameObject = Objects[i];
			sortGameObject.transform.localPosition = new Vector3(Mathf.Lerp(-2f, 2f, (float)i / ((float)Objects.Length - 1f)), FloatRange.Next(-2.25f, -1.7f), -1f);
			CheckBox(sortGameObject, dropped: true);
		}
	}

	public void Update()
	{
		if (amClosing != CloseState.None)
		{
			return;
		}
		myController.Update();
		for (int i = 0; i < Objects.Length; i++)
		{
			SortGameObject sortGameObject = Objects[i];
			switch (myController.CheckDrag(sortGameObject.Collider))
			{
			case DragState.TouchStart:
				if (Constants.ShouldPlaySfx())
				{
					SoundManager.Instance.PlaySound(PickUpSounds.Random(), loop: false);
				}
				sortGameObject.StopAllCoroutines();
				sortGameObject.StartCoroutine(sortGameObject.CoShadowRise());
				break;
			case DragState.Dragging:
			{
				Vector2 dragPosition = myController.DragPosition;
				Vector3 position = sortGameObject.transform.position;
				position.x = dragPosition.x;
				position.y = dragPosition.y;
				sortGameObject.transform.position = position;
				CheckBox(sortGameObject, dropped: false);
				break;
			}
			case DragState.Released:
			{
				bool flag = true;
				for (int j = 0; j < Objects.Length; j++)
				{
					SortGameObject obj = Objects[j];
					flag &= CheckBox(obj, dropped: true);
				}
				sortGameObject.StopAllCoroutines();
				sortGameObject.StartCoroutine(sortGameObject.CoShadowFall(CheckBox(sortGameObject, dropped: true), DropSounds.Random()));
				if (flag)
				{
					MyNormTask.NextStep();
					StartCoroutine(CoStartClose());
				}
				break;
			}
			}
		}
	}

	private bool CheckBox(SortGameObject obj, bool dropped)
	{
		BoxCollider2D collider = null;
		switch (obj.MyType)
		{
		case SortGameObject.ObjType.Animal:
			collider = AnimalBox;
			break;
		case SortGameObject.ObjType.Mineral:
			collider = MineralBox;
			break;
		case SortGameObject.ObjType.Plant:
			collider = PlantBox;
			break;
		}
		if (obj.Collider.IsTouching(collider))
		{
			obj.Shadow.material.SetFloat("_Outline", 1f);
			obj.Shadow.material.SetColor("_OutlineColor", new Color(0f, 0.8f, 1f));
			return true;
		}
		if (dropped)
		{
			obj.Shadow.material.SetFloat("_Outline", 1f);
			obj.Shadow.material.SetColor("_OutlineColor", Color.red);
		}
		else
		{
			obj.Shadow.material.SetFloat("_Outline", 0f);
		}
		return false;
	}
}
