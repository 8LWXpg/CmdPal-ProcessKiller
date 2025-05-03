using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using ProcessKiller.Helpers;
using System.Diagnostics;

namespace ProcessKiller;
internal sealed partial class KillCommand(Process process) : InvokableCommand
{
	public override string Name => "Kill process";

	private readonly Process Process = process;

	public override ICommandResult Invoke()
	{
		_ = ProcessHelper.TryKill(Process);
		return CommandResult.GoHome();
	}
}
