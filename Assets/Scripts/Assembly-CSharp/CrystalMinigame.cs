using System.Linq;
using UnityEngine;

public class CrystalMinigame : Minigame
{
	public CrystalBehaviour[] CrystalPieces;

	private CrystalBehaviour[] Shuffed;

	public Transform[] CrystalSlots;

	public FloatRange XRange;

	public float TrayY = -2.28f;

	public AudioClip[] PickUpSounds;

	public AudioClip AttachSound;

	private Controller myController = new Controller();

	public void Start()
	{
		Shuffed = CrystalPieces.ToArray();
		Shuffed.Shuffle();
		for (int i = 0; i < Shuffed.Length; i++)
		{
			Shuffed[i].transform.localPosition = new Vector3(XRange.Lerp(((float)i + 0.5f) / (float)Shuffed.Length), TrayY, ((float)i - (float)Shuffed.Length / 2f) / 100f);
		}
	}

	public void Update()
	{
		myController.Update();
		for (int i = 0; i < CrystalPieces.Length; i++)
		{
			CrystalBehaviour crystalBehaviour = CrystalPieces[i];
			switch (myController.CheckDrag(crystalBehaviour.Collider))
			{
			case DragState.TouchStart:
				if (Constants.ShouldPlaySfx())
				{
					SoundManager.Instance.PlaySound(PickUpSounds.Random(), loop: false);
				}
				crystalBehaviour.StopAllCoroutines();
				break;
			case DragState.Dragging:
			{
				Vector3 position = myController.DragPosition;
				position.z = base.transform.position.z;
				crystalBehaviour.transform.position = position;
				break;
			}
			case DragState.Released:
				CheckSolution(i);
				if (crystalBehaviour.CanMove)
				{
					int num = Shuffed.IndexOf(crystalBehaviour);
					crystalBehaviour.StartCoroutine(Effects.Slide2D(crystalBehaviour.transform, crystalBehaviour.transform.localPosition, new Vector2(XRange.Lerp(((float)num + 0.5f) / (float)CrystalPieces.Length), TrayY), 0.15f));
				}
				break;
			}
		}
	}

	private void CheckSolution(int startAt)
	{
		CrystalBehaviour crystalBehaviour = CrystalPieces[startAt];
		if (!crystalBehaviour.CanMove)
		{
			return;
		}
		Transform transform = CrystalSlots[startAt];
		if (crystalBehaviour.Collider.OverlapPoint(transform.position))
		{
			if (Constants.ShouldPlaySfx())
			{
				SoundManager.Instance.PlaySound(AttachSound, loop: false);
			}
			crystalBehaviour.CanMove = false;
			crystalBehaviour.TargetPosition = transform;
			for (int i = startAt; i < CrystalPieces.Length; i++)
			{
				CrystalPieces[i].Flash((float)(i - startAt) * 0.1f);
			}
			for (int num = startAt - 1; num >= 0; num--)
			{
				CrystalPieces[num].Flash((float)(startAt - num) * 0.1f);
			}
		}
		if (CrystalPieces.All((CrystalBehaviour c) => !c.CanMove))
		{
			MyNormTask.NextStep();
			StartCoroutine(CoStartClose());
		}
	}
}
