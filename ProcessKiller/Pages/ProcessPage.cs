using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using ProcessKiller.Helpers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace ProcessKiller.Pages;
internal sealed partial class ProcessPage : ListPage
{
	private readonly SettingsManager _settingsManager;

	public ProcessPage(SettingsManager settingsManager)
	{
		Title = "Kill a process";
		Name = "Enter";
		Icon = IconHelpers.FromRelativePaths("Assets/Process.light.svg", "Assets/Process.dark.svg");
		ShowDetails = true;
		_settingsManager = settingsManager;
	}

	public override IListItem[] GetItems()
	{
		var shellWindowId = ProcessHelper.GetProcessIDFromWindowHandle(NativeMethods.GetShellWindow());
		var processes = Process.GetProcesses().Where(p => !ProcessHelper.IsSystemProcess(p) && (p.Id != shellWindowId || _settingsManager.ShowShellExplorer)).ToList();
		CommandLineQuery? commandLineQuery = _settingsManager.ShowCommandLine ? new() : null;

		List<ListItem> results = processes.ConvertAll(p => (ListItem)new ProcessItem(
			p,
			commandLineQuery,
			_settingsManager.ShowCommandLine,
			Icon));
		results.Reverse();

		return [.. results];
	}
}
