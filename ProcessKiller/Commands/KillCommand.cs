using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using ProcessKiller.Helpers;
using ProcessKiller.Properties;

namespace ProcessKiller;

internal sealed partial class KillCommand(int processId) : InvokableCommand
{
	public override string Name => Resources.kill;

	private readonly int _processId = processId;

	public override ICommandResult Invoke()
	{
		_ = ProcessHelper.TryKillById(_processId);
		return CommandResult.GoHome();
	}
}
