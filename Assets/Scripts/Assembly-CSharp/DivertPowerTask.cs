using System.Linq;
using System.Text;

public class DivertPowerTask : NormalPlayerTask
{
	public SystemTypes TargetSystem;

	public override bool ValidConsole(Console console)
	{
		if (console.Room != TargetSystem || !console.ValidTasks.Any((TaskSet set) => TaskType == set.taskType && set.taskStep.Contains(taskStep)))
		{
			if (taskStep == 0)
			{
				return console.TaskTypes.Contains(TaskType);
			}
			return false;
		}
		return true;
	}

	public override void AppendTaskText(StringBuilder sb)
	{
		if (taskStep > 0)
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
		if (taskStep == 0)
		{
			sb.Append(DestroyableSingleton<TranslationController>.Instance.GetString(StartAt));
			sb.Append(": ");
			sb.Append(DestroyableSingleton<TranslationController>.Instance.GetString(StringNames.DivertPowerTo));
			sb.Append(" ");
			sb.Append(DestroyableSingleton<TranslationController>.Instance.GetString(TargetSystem));
		}
		else
		{
			sb.Append(DestroyableSingleton<TranslationController>.Instance.GetString(TargetSystem));
			sb.Append(": ");
			sb.Append(DestroyableSingleton<TranslationController>.Instance.GetString(StringNames.AcceptDivertedPower));
		}
		sb.Append(" (");
		sb.Append(taskStep);
		sb.Append("/");
		sb.Append(MaxStep);
		sb.AppendLine(")");
		if (taskStep > 0)
		{
			sb.Append("[]");
		}
	}
}
