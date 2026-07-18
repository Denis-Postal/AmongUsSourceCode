public class WinningPlayerData
{
	public string Name;

	public bool IsDead;

	public bool IsImpostor;

	public int ColorId;

	public uint SkinId;

	public uint HatId;

	public uint PetId;

	public bool IsYou;

	public WinningPlayerData()
	{
	}

	public WinningPlayerData(GameData.PlayerInfo player)
	{
		IsYou = player.Object == PlayerControl.LocalPlayer;
		Name = player.PlayerName;
		IsDead = player.IsDead || player.Disconnected;
		IsImpostor = player.IsImpostor;
		ColorId = player.ColorId;
		SkinId = player.SkinId;
		PetId = player.PetId;
		HatId = player.HatId;
	}
}
