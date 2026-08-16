using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using ProcessKiller.Helpers;
using ProcessKiller.Properties;
using System.Diagnostics;

namespace ProcessKiller.Commands;

internal sealed partial class KillAllCommand(string processName) : InvokableCommand
{
	public override string Name => Resources.kill_all_process;

	private readonly string _processName = processName;

	public override ICommandResult Invoke()
	{
		foreach (Process p in Process.GetProcessesByName(_processName))
		{
			using (p)
			{
				_ = ProcessHelper.TryKill(p);
			}
		}

		return CommandResult.GoHome();
	}
}
