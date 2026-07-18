using System.Collections;
using InnerNet;
using Photon.Pun;
using Photon.Realtime;
using PowerTools;
using UnityEngine;
using UnityEngine.SceneManagement;

public class FindGameButton : MonoBehaviour, IConnectButton
{
	public SpriteAnim connectIcon;

	public AnimationClip connectClip;

	public void OnClick()
	{
		if (!NameTextBehaviour.Instance.ShakeIfInvalid())
		{
			if (StatsManager.Instance.AmBanned)
			{
				AmongUsClient.Instance.LastDisconnectReason = DisconnectReasons.IntentionalLeaving;
				DestroyableSingleton<DisconnectPopup>.Instance.Show();
			}
			else if (DestroyableSingleton<MatchMaker>.Instance.Connecting(this))
			{
				AmongUsClient.Instance.GameMode = GameModes.OnlineGame;
				AmongUsClient.Instance.MainMenuScene = "MMOnline";
				StartCoroutine(ConnectForFindGame());
			}
		}
	}

	private IEnumerator ConnectForFindGame()
	{
		AmongUsClient.Instance.SetEndpoint(DestroyableSingleton<ServerManager>.Instance.OnlineNetAddress, 22023);
		AmongUsClient.Instance.OnlineScene = "OnlineGame";
		AmongUsClient.Instance.mode = MatchMakerModes.Client;
		yield return AmongUsClient.Instance.CoConnect();
		if (AmongUsClient.Instance.LastDisconnectReason != DisconnectReasons.ExitGame || !IsReadyForGameList())
		{
			DestroyableSingleton<MatchMaker>.Instance.NotConnecting();
			yield break;
		}
		AmongUsClient.Instance.HostId = AmongUsClient.Instance.ClientId;
		SceneManager.LoadScene("FindAGame");
	}

	private static bool IsReadyForGameList()
	{
		return PhotonNetwork.InLobby || PhotonNetwork.NetworkClientState == ClientState.ConnectedToMasterServer || PhotonNetwork.IsConnectedAndReady;
	}

	public void StartIcon()
	{
		connectIcon.Play(connectClip);
	}

	public void StopIcon()
	{
		connectIcon.Stop();
		connectIcon.GetComponent<SpriteRenderer>().sprite = null;
	}
}
