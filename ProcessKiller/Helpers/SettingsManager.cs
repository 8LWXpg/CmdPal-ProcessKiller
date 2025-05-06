using Microsoft.CommandPalette.Extensions.Toolkit;
using ProcessKiller.Properties;
using System.IO;

namespace ProcessKiller.Helpers;
internal sealed class SettingsManager : JsonSettingsManager
{
	public bool ShowCommandLine => _showCommandLine.Value;
	private readonly ToggleSetting _showCommandLine = new(
		nameof(ShowCommandLine),
		Resources.setting_show_command_line,
		Resources.setting_show_command_line_description,
		false);

	public bool ShowShellExplorer => _showShellExplorer.Value;
	private readonly ToggleSetting _showShellExplorer = new(
		nameof(ShowShellExplorer),
		Resources.setting_show_shell_explorer,
		"",
		false);

	public SettingsManager()
	{
		FilePath = SettingsJsonPath();
		Settings.Add(_showCommandLine);
		Settings.Add(_showShellExplorer);

		LoadSettings();

		Settings.SettingsChanged += (s, a) => SaveSettings();
	}

	private static string SettingsJsonPath()
	{
		var directory = Utilities.BaseSettingsPath("ProcessKillerExtension");
		_ = Directory.CreateDirectory(directory);
		return Path.Combine(directory, "settings.json");
	}
}
