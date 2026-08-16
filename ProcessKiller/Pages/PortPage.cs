using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using ProcessKiller.Helpers;
using ProcessKiller.Properties;

namespace ProcessKiller.Pages;

internal sealed partial class PortPage : ListPage
{
	private readonly SettingsManager _settingsManager;
	private readonly IconCache _iconCache;

	public PortPage(SettingsManager settingsManager, IconCache iconCache)
	{
		Title = Resources.kill_a_process_by_ip_and_port;
		Icon = IconHelpers.FromRelativePaths("Assets/Port.light.svg", "Assets/Port.dark.svg");
		ShowDetails = true;
		_settingsManager = settingsManager;
		_iconCache = iconCache;
	}

	public override IListItem[] GetItems()
	{
		using PortQuery query = new();
		return query.GetItems(_settingsManager.ShowCommandLine, _iconCache, Icon);
	}
}
