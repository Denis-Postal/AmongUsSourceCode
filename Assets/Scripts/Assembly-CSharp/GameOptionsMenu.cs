using UnityEngine;

public class GameOptionsMenu : MonoBehaviour
{
	private GameOptionsData cachedData;

	public GameObject ResetButton;

	private OptionBehaviour[] Children;

	public void Start()
	{
		ConvertPlusMinusButtonsToTmp();
		Children = GetComponentsInChildren<OptionBehaviour>();
		cachedData = PlayerControl.GameOptions;
		for (int i = 0; i < Children.Length; i++)
		{
			OptionBehaviour optionBehaviour = Children[i];
			optionBehaviour.OnValueChanged = ValueChanged;
			if ((bool)AmongUsClient.Instance && !AmongUsClient.Instance.AmHost)
			{
				optionBehaviour.SetAsPlayer();
			}
		}
	}

	private void ConvertPlusMinusButtonsToTmp()
	{
		Transform[] transforms = GetComponentsInChildren<Transform>(true);
		for (int i = 0; i < transforms.Length; i++)
		{
			Transform child = transforms[i];
			if (child.name == "Plus" && IsAdjustableOptionButton(child, "Increase"))
			{
				NumberOption.ConvertButtonToTmp(child, "+");
			}
			else if (child.name == "Minus" && IsAdjustableOptionButton(child, "Decrease"))
			{
				NumberOption.ConvertButtonToTmp(child, "-");
			}
		}
	}

	private bool IsAdjustableOptionButton(Transform button, string methodName)
	{
		if (!button.GetComponentInParent<NumberOption>() && !button.GetComponentInParent<StringOption>())
		{
			return false;
		}
		PassiveButton passiveButton = button.GetComponent<PassiveButton>();
		if (!passiveButton)
		{
			return false;
		}
		for (int i = 0; i < passiveButton.OnClick.GetPersistentEventCount(); i++)
		{
			UnityEngine.Object target = passiveButton.OnClick.GetPersistentTarget(i);
			if (passiveButton.OnClick.GetPersistentMethodName(i) == methodName && (!target || target is NumberOption || target is StringOption))
			{
				return true;
			}
		}
		return false;
	}

	public void Update()
	{
		if (cachedData != PlayerControl.GameOptions)
		{
			cachedData = PlayerControl.GameOptions;
			RefreshChildren();
		}
	}

	private void RefreshChildren()
	{
		for (int i = 0; i < Children.Length; i++)
		{
			OptionBehaviour obj = Children[i];
			obj.enabled = false;
			obj.enabled = true;
		}
	}

	public void ValueChanged(OptionBehaviour option)
	{
		if (!AmongUsClient.Instance || !AmongUsClient.Instance.AmHost)
		{
			return;
		}
		if (option.Title == StringNames.GameRecommendedSettings)
		{
			if (cachedData.isDefaults)
			{
				cachedData.isDefaults = false;
			}
			else
			{
				cachedData.SetRecommendations(GameData.Instance.PlayerCount, AmongUsClient.Instance.GameMode);
			}
			RefreshChildren();
		}
		else
		{
			GameOptionsData gameOptions = PlayerControl.GameOptions;
			switch (option.Title)
			{
			case StringNames.GamePlayerSpeed:
				gameOptions.PlayerSpeedMod = option.GetFloat();
				break;
			case StringNames.GameCrewLight:
				gameOptions.CrewLightMod = option.GetFloat();
				break;
			case StringNames.GameImpostorLight:
				gameOptions.ImpostorLightMod = option.GetFloat();
				break;
			case StringNames.GameKillCooldown:
				gameOptions.KillCooldown = option.GetFloat();
				break;
			case StringNames.GameKillDistance:
				gameOptions.KillDistance = option.GetInt();
				break;
			case StringNames.GameCommonTasks:
				gameOptions.NumCommonTasks = option.GetInt();
				break;
			case StringNames.GameLongTasks:
				gameOptions.NumLongTasks = option.GetInt();
				break;
			case StringNames.GameShortTasks:
				gameOptions.NumShortTasks = option.GetInt();
				break;
			case StringNames.GameNumImpostors:
				gameOptions.NumImpostors = option.GetInt();
				break;
			case StringNames.GameNumMeetings:
				gameOptions.NumEmergencyMeetings = option.GetInt();
				break;
			case StringNames.GameEmergencyCooldown:
				gameOptions.EmergencyCooldown = option.GetInt();
				break;
			case StringNames.GameDiscussTime:
				gameOptions.DiscussionTime = option.GetInt();
				break;
			case StringNames.GameVotingTime:
				gameOptions.VotingTime = option.GetInt();
				break;
			case StringNames.GameMapName:
				gameOptions.MapId = (byte)option.GetInt();
				break;
			default:
				Debug.Log("Ono, unrecognized setting: " + option.Title);
				break;
			}
			if (gameOptions.isDefaults && option.Title != StringNames.GameMapName)
			{
				gameOptions.isDefaults = false;
				RefreshChildren();
			}
		}
		PlayerControl.LocalPlayer?.RpcSyncSettings(PlayerControl.GameOptions);
	}
}
