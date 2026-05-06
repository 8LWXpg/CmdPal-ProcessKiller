using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using ProcessKiller.Helpers;
using ProcessKiller.Properties;
using System.Diagnostics;

namespace ProcessKiller.Commands;

internal sealed partial class KillAllCommand(Process process) : InvokableCommand
{
	public override string Name => Resources.kill_all_process;

	private readonly Process Process = process;

	public override ICommandResult Invoke()
	{
		Process[] processes = Process.GetProcessesByName(Process.ProcessName);
		foreach (Process p in processes)
		{
			_ = ProcessHelper.TryKill(p);
		}

		return CommandResult.GoHome();
	}
}
