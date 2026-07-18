using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public class NormalPlayerTask : PlayerTask
{
	public enum TimerState
	{
		NotStarted = 0,
		Started = 1,
		Finished = 2
	}

	public int taskStep;

	public int MaxStep;

	public bool ShowTaskStep = true;

	public bool ShowTaskTimer;

	public TimerState TimerStarted;

	public float TaskTimer;

	public byte[] Data;

	public ArrowBehaviour Arrow;

	public override int TaskStep => taskStep;

	public override bool IsComplete => taskStep >= MaxStep;

	public override void Initialize()
	{
		if ((bool)Arrow && !base.Owner.AmOwner)
		{
			Arrow.gameObject.SetActive(value: false);
		}
		HasLocation = true;
		LocationDirty = true;
		switch (TaskType)
		{
		case TaskTypes.WaterPlants:
			Data = new byte[4];
			break;
		case TaskTypes.EnterIdCode:
			Data = BitConverter.GetBytes(IntRange.Next(1, 99999));
			break;
		case TaskTypes.ChartCourse:
			Data = new byte[4];
			break;
		case TaskTypes.InspectSample:
			Data = new byte[2];
			break;
		case TaskTypes.FuelEngines:
			Data = new byte[2];
			break;
		case TaskTypes.StartReactor:
			Data = new byte[6];
			break;
		case TaskTypes.PrimeShields:
		{
			Data = new byte[1];
			int num2 = 0;
			for (int num3 = 0; num3 < 7; num3++)
			{
				byte b = (byte)(1 << num3);
				if (BoolRange.Next(0.7f))
				{
					Data[0] |= b;
					num2++;
				}
			}
			Data[0] &= 118;
			break;
		}
		case TaskTypes.AlignEngineOutput:
			Data = new byte[2];
			Data[0] = AlignGame.ToByte((float)IntRange.RandomSign() * FloatRange.Next(1f, 3f));
			Data[1] = (byte)(IntRange.RandomSign() * IntRange.Next(25, 255));
			break;
		case TaskTypes.FixWiring:
		{
			Data = new byte[MaxStep];
			List<Console> list = ShipStatus.Instance.AllConsoles.Where((Console t) => t.TaskTypes.Contains(TaskTypes.FixWiring)).ToList();
			List<Console> list2 = new List<Console>(list);
			for (int num = 0; num < Data.Length; num++)
			{
				int index = list2.RandomIdx();
				Data[num] = (byte)list2[index].ConsoleId;
				list2.RemoveAt(index);
			}
			Array.Sort(Data);
			Console console = list.First((Console v) => v.ConsoleId == Data[0]);
			StartAt = console.Room;
			break;
		}
		}
	}

	public void NextStep()
	{
		taskStep++;
		UpdateArrow();
		if (taskStep >= MaxStep)
		{
			taskStep = MaxStep;
			if (!PlayerControl.LocalPlayer)
			{
				return;
			}
			if (DestroyableSingleton<HudManager>.InstanceExists)
			{
				DestroyableSingleton<HudManager>.Instance.ShowTaskComplete();
				StatsManager.Instance.TasksCompleted++;
				if (PlayerTask.AllTasksCompleted(PlayerControl.LocalPlayer))
				{
					StatsManager.Instance.CompletedAllTasks++;
				}
			}
			PlayerControl.LocalPlayer.RpcCompleteTask(base.Id);
		}
		else if (ShowTaskStep && Constants.ShouldPlaySfx())
		{
			SoundManager.Instance.PlaySound(DestroyableSingleton<HudManager>.Instance.TaskUpdateSound, loop: false);
		}
	}

	public void UpdateArrow()
	{
		if (!Arrow)
		{
			return;
		}
		if (!base.Owner.AmOwner)
		{
			Arrow.gameObject.SetActive(value: false);
		}
		else if (!IsComplete)
		{
			Arrow.gameObject.SetActive(value: true);
			if (TaskType == TaskTypes.FixWiring)
			{
				Console console = FindSpecialConsole((Console c) => c.TaskTypes.Contains(TaskTypes.FixWiring) && c.ConsoleId == Data[taskStep]);
				Arrow.target = console.transform.position;
				StartAt = console.Room;
			}
			else if (TaskType == TaskTypes.AlignEngineOutput)
			{
				if (AlignGame.IsSuccess(Data[0]))
				{
					Arrow.target = FindSpecialConsole((Console c) => c.TaskTypes.Contains(TaskTypes.AlignEngineOutput) && c.ConsoleId == 1).transform.position;
					StartAt = SystemTypes.UpperEngine;
				}
				else
				{
					Arrow.target = FindSpecialConsole((Console console3) => console3.TaskTypes.Contains(TaskTypes.AlignEngineOutput) && console3.ConsoleId == 0).transform.position;
					StartAt = SystemTypes.LowerEngine;
				}
			}
			else
			{
				Console console2 = FindObjectPos();
				Arrow.target = console2.transform.position;
				StartAt = console2.Room;
			}
			LocationDirty = true;
		}
		else
		{
			Arrow.gameObject.SetActive(value: false);
		}
	}

	private void FixedUpdate()
	{
		if (TimerStarted == TimerState.Started)
		{
			TaskTimer -= Time.fixedDeltaTime;
			if (TaskTimer <= 0f)
			{
				TaskTimer = 0f;
				TimerStarted = TimerState.Finished;
			}
		}
	}

	public override bool ValidConsole(Console console)
	{
		if (TaskType == TaskTypes.FixWiring)
		{
			if (console.TaskTypes.Contains(TaskType))
			{
				return console.ConsoleId == Data[taskStep];
			}
			return false;
		}
		if (TaskType == TaskTypes.AlignEngineOutput)
		{
			if (console.TaskTypes.Contains(TaskType))
			{
				return !AlignGame.IsSuccess(Data[console.ConsoleId]);
			}
			return false;
		}
		if (TaskType == TaskTypes.FuelEngines)
		{
			if (!console.TaskTypes.Contains(TaskType) || console.ConsoleId != Data[1])
			{
				return console.ValidTasks.Any((TaskSet set) => TaskType == set.taskType && set.taskStep.Contains(Data[1]));
			}
			return true;
		}
		if (!console.TaskTypes.Any((TaskTypes tt) => tt == TaskType))
		{
			return console.ValidTasks.Any((TaskSet set) => TaskType == set.taskType && set.taskStep.Contains(taskStep));
		}
		return true;
	}

	public override void Complete()
	{
		taskStep = MaxStep;
	}

	public override void AppendTaskText(StringBuilder sb)
	{
		bool num = ShouldYellowText();
		if (num)
		{
			if (IsComplete)
			{
				sb.Append("[00DD00FF]");
			}
			else
			{
				sb.Append("[FFFF00FF]");
			}
		}
		sb.Append(DestroyableSingleton<TranslationController>.Instance.GetString(StartAt));
		sb.Append(": ");
		sb.Append(DestroyableSingleton<TranslationController>.Instance.GetString(TaskType));
		if (ShowTaskTimer && TimerStarted == TimerState.Started)
		{
			sb.Append(" (");
			sb.Append((int)TaskTimer);
			sb.Append("s)");
		}
		else if (ShowTaskStep)
		{
			sb.Append(" (");
			sb.Append(taskStep);
			sb.Append("/");
			sb.Append(MaxStep);
			sb.Append(")");
		}
		if (num)
		{
			sb.Append("[]");
		}
		sb.AppendLine();
	}

	private bool ShouldYellowText()
	{
		if (TaskType == TaskTypes.FuelEngines && Data[1] > 0)
		{
			return true;
		}
		if (taskStep <= 0)
		{
			return TimerStarted != TimerState.NotStarted;
		}
		return true;
	}
}
