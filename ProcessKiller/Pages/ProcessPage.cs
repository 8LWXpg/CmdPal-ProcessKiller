using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using ProcessKiller.Helpers;
using ProcessKiller.Properties;
using System.Diagnostics;

namespace ProcessKiller.Pages;

internal sealed partial class ProcessPage : ListPage
{
	private readonly SettingsManager _settingsManager;
	private readonly IconCache _iconCache;

	public ProcessPage(SettingsManager settingsManager, IconCache iconCache)
	{
		Title = Resources.kill_a_process;
		Icon = IconHelpers.FromRelativePaths("Assets/Process.light.svg", "Assets/Process.dark.svg");
		ShowDetails = true;
		_settingsManager = settingsManager;
		_iconCache = iconCache;
	}

	public override IListItem[] GetItems()
	{
		var excludeId = _settingsManager.ShowShellExplorer ? null : (int?)ProcessHelper.GetShellWindowId();
		CommandLineQuery? commandLineQuery = _settingsManager.ShowCommandLine ? new() : null;

		List<Process> processes = ProcessHelper.GetNonSystemProcesses(excludeId);
		try
		{
			List<ProcessItem> results = processes.ConvertAll(p => new ProcessItem(
				p,
				commandLineQuery,
				_settingsManager.ShowCommandLine,
				_iconCache,
				Icon));

			results.Reverse();

			return [.. results];
		}
		finally
		{
			// The items copied out everything they show, so the snapshots are done.
			foreach (Process p in processes)
			{
				p.Dispose();
			}
		}
	}
}
