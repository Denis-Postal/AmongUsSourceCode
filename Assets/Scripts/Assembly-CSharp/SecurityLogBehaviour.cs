using System.Collections.Generic;
using UnityEngine;

public class SecurityLogBehaviour : MonoBehaviour
{
	public enum SecurityLogLocations
	{
		North = 0,
		Southeast = 1,
		Southwest = 2
	}

	public struct SecurityLogEntry
	{
		public byte PlayerId;

		public SecurityLogLocations Location;

		public SecurityLogEntry(byte playerId, SecurityLogLocations location)
		{
			PlayerId = playerId;
			Location = location;
		}
	}

	public const byte ConsoleMask = 240;

	public const byte PlayerMask = 15;

	public Color[] BarColors = new Color[3]
	{
		new Color32(33, 77, 173, 128),
		new Color32(173, 81, 16, 128),
		new Color32(16, 97, 8, 128)
	};

	public readonly List<SecurityLogEntry> LogEntries = new List<SecurityLogEntry>();

	public bool HasNew;

	public void LogPlayer(PlayerControl player, SecurityLogLocations location)
	{
		HasNew = true;
		LogEntries.Add(new SecurityLogEntry(player.PlayerId, location));
		if (LogEntries.Count > 20)
		{
			LogEntries.RemoveAt(0);
		}
	}
}
