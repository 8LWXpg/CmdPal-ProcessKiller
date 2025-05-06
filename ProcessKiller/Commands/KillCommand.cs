using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using ProcessKiller.Helpers;
using ProcessKiller.Properties;
using System.Diagnostics;

namespace ProcessKiller;
internal sealed partial class KillCommand(Process process) : InvokableCommand
{
	public override string Name => Resources.kill;

	private readonly Process Process = process;

	public override ICommandResult Invoke()
	{
		_ = ProcessHelper.TryKill(Process);
		return CommandResult.GoHome();
	}
}
