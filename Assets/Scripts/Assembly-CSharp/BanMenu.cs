using System.Collections.Generic;
using System.Linq;
using InnerNet;
using UnityEngine;

public class BanMenu : MonoBehaviour
{
	public BanButton BanButtonPrefab;

	public SpriteRenderer Background;

	public SpriteRenderer BanButton;

	public SpriteRenderer KickButton;

	public GameObject ContentParent;

	public int selected = -1;

	[HideInInspector]
	public List<BanButton> allButtons = new List<BanButton>();

	public void SetVisible(bool show)
	{
		show &= (bool)PlayerControl.LocalPlayer && PlayerControl.LocalPlayer.Data != null && !PlayerControl.LocalPlayer.Data.IsDead;
		show &= AmongUsClient.Instance.CanKick();
		show &= (bool)MeetingHud.Instance || !ShipStatus.Instance;
		BanButton.gameObject.SetActive(AmongUsClient.Instance.CanBan());
		KickButton.gameObject.SetActive(AmongUsClient.Instance.CanKick());
		GetComponent<SpriteRenderer>().enabled = show;
		GetComponent<PassiveButton>().enabled = show;
	}

	private void Update()
	{
		if (!AmongUsClient.Instance)
		{
			return;
		}
		for (int i = 0; i < AmongUsClient.Instance.allClients.Count; i++)
		{
			try
			{
				ClientData client = AmongUsClient.Instance.allClients[i];
				if (client == null)
				{
					break;
				}
				if (VoteBanSystem.Instance.HasMyVote(client.Id) && VoteBanSystem.Instance.Votes.TryGetValue(client.Id, out var value))
				{
					int num = value.Count((int c) => c != 0);
					BanButton banButton = allButtons.FirstOrDefault((BanButton b) => b.TargetClientId == client.Id);
					if ((bool)banButton && banButton.numVotes != num)
					{
						banButton.SetVotes(num);
					}
				}
			}
			catch
			{
				break;
			}
		}
	}

	public void Show()
	{
		if (ContentParent.activeSelf)
		{
			Hide();
			return;
		}
		selected = -1;
		KickButton.color = Color.gray;
		BanButton.color = Color.gray;
		ContentParent.SetActive(value: true);
		int num = 0;
		if ((bool)AmongUsClient.Instance)
		{
			List<ClientData> allClients = AmongUsClient.Instance.allClients;
			for (int i = 0; i < allClients.Count; i++)
			{
				ClientData clientData = allClients[i];
				if (clientData.Id != AmongUsClient.Instance.ClientId && (bool)clientData.Character)
				{
					GameData.PlayerInfo data = clientData.Character.Data;
					if (!string.IsNullOrWhiteSpace(data.PlayerName))
					{
						BanButton banButton = Object.Instantiate(BanButtonPrefab, ContentParent.transform);
						banButton.transform.localPosition = new Vector3(-0.2f, -0.15f - 0.4f * (float)num, -1f);
						banButton.Parent = this;
						banButton.NameText.Text = data.PlayerName;
						banButton.TargetClientId = clientData.Id;
						banButton.Unselect();
						allButtons.Add(banButton);
						num++;
					}
				}
			}
		}
		KickButton.transform.localPosition = new Vector3(-0.8f, -0.15f - 0.4f * (float)num - 0.1f, -1f);
		BanButton.transform.localPosition = new Vector3(0.3f, -0.15f - 0.4f * (float)num - 0.1f, -1f);
		float num2 = 0.3f + (float)(num + 1) * 0.4f;
		Background.size = new Vector2(3f, num2);
		Background.GetComponent<BoxCollider2D>().size = new Vector2(3f, num2);
		Background.transform.localPosition = new Vector3(0f, (0f - num2) / 2f + 0.15f, 0.1f);
	}

	public void Hide()
	{
		selected = -1;
		ContentParent.SetActive(value: false);
		for (int i = 0; i < allButtons.Count; i++)
		{
			Object.Destroy(allButtons[i].gameObject);
		}
		allButtons.Clear();
	}

	public void Select(int client)
	{
		if (VoteBanSystem.Instance.HasMyVote(client))
		{
			return;
		}
		selected = client;
		for (int i = 0; i < allButtons.Count; i++)
		{
			BanButton banButton = allButtons[i];
			if (banButton.TargetClientId != client)
			{
				banButton.Unselect();
			}
		}
		KickButton.color = Color.white;
		BanButton.color = Color.white;
	}

	public void Kick(bool ban)
	{
		if (selected >= 0)
		{
			if (AmongUsClient.Instance.CanBan())
			{
				AmongUsClient.Instance.KickPlayer(selected, ban);
				Hide();
			}
			else
			{
				VoteBanSystem.Instance.CmdAddVote(selected);
			}
		}
		Select(-1);
	}
}
