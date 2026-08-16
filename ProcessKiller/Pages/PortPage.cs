using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using ProcessKiller.Helpers;
using ProcessKiller.Properties;
using System.Collections.Generic;

namespace ProcessKiller.Pages;

internal sealed partial class PortPage : ListPage
{
	private readonly SettingsManager _settingsManager;

	// Items kept alive so their Process/icon-stream can be disposed at the start of the next
	// refresh, once they're no longer the ones being shown.
	private List<ProcessItem> _trackedItems = [];

	public PortPage(SettingsManager settingsManager)
	{
		Title = Resources.kill_a_process_by_ip_and_port;
		Icon = IconHelpers.FromRelativePaths("Assets/Port.light.svg", "Assets/Port.dark.svg");
		ShowDetails = true;
		_settingsManager = settingsManager;
	}

	public override IListItem[] GetItems()
	{
		foreach (ProcessItem item in _trackedItems)
		{
			item.Dispose();
		}

		_trackedItems = new PortQuery().GetItems(_settingsManager.ShowCommandLine, Icon);

		return [.. _trackedItems];
	}
}
