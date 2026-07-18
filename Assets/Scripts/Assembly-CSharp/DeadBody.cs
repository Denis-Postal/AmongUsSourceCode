using UnityEngine;

public class DeadBody : MonoBehaviour
{
	public bool Reported;

	public short KillIdx;

	public byte ParentId;

	public Collider2D myCollider;

	public Vector2 TruePosition => base.transform.position + (Vector3)myCollider.offset;

	public void OnClick()
	{
		if (!Reported)
		{
			Vector2 truePosition = PlayerControl.LocalPlayer.GetTruePosition();
			Vector2 truePosition2 = TruePosition;
			if (Vector2.Distance(truePosition2, truePosition) <= PlayerControl.LocalPlayer.MaxReportDistance && PlayerControl.LocalPlayer.CanMove && !PhysicsHelpers.AnythingBetween(truePosition, truePosition2, Constants.ShipAndObjectsMask, useTriggers: false))
			{
				Reported = true;
				GameData.PlayerInfo playerById = GameData.Instance.GetPlayerById(ParentId);
				PlayerControl.LocalPlayer.CmdReportDeadBody(playerById);
			}
		}
	}
}
