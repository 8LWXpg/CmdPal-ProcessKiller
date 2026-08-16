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

		// wmic is the slowest thing on this page by far and nothing below needs it until the items
		// are built, so start it before doing anything else.
		Task<CommandLineQuery>? commandLines = _settingsManager.ShowCommandLine
			? Task.Run(() => new CommandLineQuery())
			: null;

		List<Process> processes = ProcessHelper.GetNonSystemProcesses(excludeId);
		try
		{
			List<string?> paths = processes.ConvertAll(ProcessHelper.GetExecutablePath);
			_iconCache.Prefetch(paths);

			CommandLineQuery? commandLineQuery = commandLines?.GetAwaiter().GetResult();

			List<ProcessItem> results = [];
			for (var i = 0; i < processes.Count; i++)
			{
				results.Add(new ProcessItem(
					processes[i],
					paths[i],
					commandLineQuery,
					_settingsManager.ShowCommandLine,
					_iconCache,
					Icon));
			}

			results.Reverse();

			return [.. results];
		}
		finally
		{
			foreach (Process p in processes)
			{
				p.Dispose();
			}
		}
	}
}
