using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using ProcessKiller.Helpers;

namespace ProcessKiller.Pages;
internal sealed partial class PortPage : ListPage
{
	private readonly SettingsManager _settingsManager;

	public PortPage(SettingsManager settingsManager)
	{
		Title = "Kill a process by IP and port";
		Name = "Kill";
		Icon = IconHelpers.FromRelativePaths("Assets/Port.light.svg", "Assets/Port.dark.svg");
		ShowDetails = true;
		_settingsManager = settingsManager;
	}

	public override IListItem[] GetItems() => new PortQuery().GetItems(
		_settingsManager.ShowCommandLine,
		Icon);
}
