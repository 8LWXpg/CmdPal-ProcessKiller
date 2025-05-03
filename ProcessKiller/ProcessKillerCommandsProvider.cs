// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using ProcessKiller.Helpers;
using ProcessKiller.Pages;
using ProcessKiller.Properties;

namespace ProcessKiller;

public partial class ProcessKillerCommandsProvider : CommandProvider
{
	private readonly ICommandItem[] _commands;
	private readonly SettingsManager _settingsManager = new();

	public ProcessKillerCommandsProvider()
	{
		DisplayName = Resources.plugin_name;
		Icon = IconHelpers.FromRelativePaths("Assets/ProcessKiller.light.svg", "Assets/ProcessKiller.dark.svg");
		Settings = _settingsManager.Settings;
		_commands = [
			new CommandItem(new ProcessPage(_settingsManager))
			{
				Title = DisplayName,
				Subtitle = Resources.plugin_description,
				Icon = Icon,
			},
			new CommandItem(new PortPage(_settingsManager))
			{
				Title = "Kill a Process by IP and port",
				Icon = IconHelpers.FromRelativePaths("Assets/Port.light.svg", "Assets/Port.dark.svg"),
			}
		];
	}

	public override ICommandItem[] TopLevelCommands() => _commands;
}
