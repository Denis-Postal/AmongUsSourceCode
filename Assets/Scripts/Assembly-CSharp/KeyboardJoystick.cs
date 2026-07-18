using UnityEngine;

public class KeyboardJoystick : MonoBehaviour, IVirtualJoystick
{
	private Vector2 del;

	public Vector2 Delta => del;

	private void Update()
	{
		if (!PlayerControl.LocalPlayer)
		{
			return;
		}
		del.x = (del.y = 0f);
		if (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D))
		{
			del.x += 1f;
		}
		if (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A))
		{
			del.x -= 1f;
		}
		if (Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.W))
		{
			del.y += 1f;
		}
		if (Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.S))
		{
			del.y -= 1f;
		}
		HandleHud();
		if (Input.GetKeyDown(KeyCode.Escape))
		{
			if ((bool)Minigame.Instance)
			{
				Minigame.Instance.Close();
			}
			else if (DestroyableSingleton<HudManager>.InstanceExists && (bool)MapBehaviour.Instance && MapBehaviour.Instance.IsOpen)
			{
				MapBehaviour.Instance.Close();
			}
			else if ((bool)CustomPlayerMenu.Instance)
			{
				CustomPlayerMenu.Instance.Close(canMove: true);
			}
		}
		del.Normalize();
	}

	private static void HandleHud()
	{
		if (!DestroyableSingleton<HudManager>.InstanceExists)
		{
			return;
		}
		if (Input.GetKeyDown(KeyCode.R))
		{
			DestroyableSingleton<HudManager>.Instance.ReportButton.DoClick();
		}
		if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.E))
		{
			DestroyableSingleton<HudManager>.Instance.UseButton.DoClick();
		}
		if (Input.GetKeyDown(KeyCode.Tab))
		{
			DestroyableSingleton<HudManager>.Instance.ShowMap(delegate(MapBehaviour m)
			{
				m.ShowNormalMap();
			});
		}
		if (PlayerControl.LocalPlayer.Data != null && PlayerControl.LocalPlayer.Data.IsImpostor && Input.GetKeyDown(KeyCode.Q))
		{
			DestroyableSingleton<HudManager>.Instance.KillButton.PerformKill();
		}
	}
}
