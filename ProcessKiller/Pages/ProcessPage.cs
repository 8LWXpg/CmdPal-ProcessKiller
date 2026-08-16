using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using ProcessKiller.Helpers;
using ProcessKiller.Properties;
using System.Collections.Generic;

namespace ProcessKiller.Pages;

internal sealed partial class ProcessPage : ListPage
{
	private readonly SettingsManager _settingsManager;

	// Items kept alive so their Process/icon-stream can be disposed at the start of the next
	// refresh, once they're no longer the ones being shown.
	private List<ProcessItem> _trackedItems = [];

	public ProcessPage(SettingsManager settingsManager)
	{
		Title = Resources.kill_a_process;
		Icon = IconHelpers.FromRelativePaths("Assets/Process.light.svg", "Assets/Process.dark.svg");
		ShowDetails = true;
		_settingsManager = settingsManager;
	}

	public override IListItem[] GetItems()
	{
		foreach (ProcessItem item in _trackedItems)
		{
			item.Dispose();
		}

		var excludeId = _settingsManager.ShowShellExplorer ? null : (int?)ProcessHelper.GetShellWindowId();
		CommandLineQuery? commandLineQuery = _settingsManager.ShowCommandLine ? new() : null;

		List<ProcessItem> results = ProcessHelper
			.GetNonSystemProcesses(excludeId)
			.ConvertAll(p => new ProcessItem(p, commandLineQuery, _settingsManager.ShowCommandLine, Icon));

		results.Reverse();
		_trackedItems = results;

		return [.. results];
	}
}
