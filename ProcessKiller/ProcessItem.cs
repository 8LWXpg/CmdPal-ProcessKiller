using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using ProcessKiller.Commands;
using ProcessKiller.Helpers;
using ProcessKiller.Properties;
using System.Diagnostics;

namespace ProcessKiller;

/// <summary>
/// A snapshot of a process. Everything shown is read during construction, so the caller keeps
/// ownership and can dispose the process as soon as the item is built.
/// </summary>
internal sealed partial class ProcessItem : ListItem
{
	public ProcessItem(Process process, string? executablePath, CommandLineQuery? commandLineQuery, bool showCommandLine, IconCache iconCache, IconInfo fallbackIcon) : base(new KillCommand(process.Id))
	{
		var path = executablePath ?? process.ProcessName;
		var commandLine = commandLineQuery?.GetCommandLine(process.Id);

		Title = $"{process.ProcessName} - {process.Id}";
		Subtitle = path;
		Icon = iconCache.GetIcon(executablePath, fallbackIcon);

		Details = new Details()
		{
			Title = process.ProcessName,
			HeroImage = Icon,
			Metadata = BuildDetailsElement(process, path, showCommandLine, commandLine),
		};

		MoreCommands = [
			new CommandContextItem(new KillAllCommand(process.ProcessName))
		];
	}

	private static IDetailsElement[] BuildDetailsElement(
		Process process,
		string path,
		bool showCommandLine,
		string? commandLine)
	{
		List<DetailsElement> details = [];

		if (!string.IsNullOrWhiteSpace(process.MainWindowTitle))
		{
			details.Add(new() { Key = Resources.detail_main_window, Data = new DetailsLink(string.Empty, process.MainWindowTitle) });
		}

		details.Add(new() { Key = Resources.detail_memory, Data = new DetailsLink(string.Empty, FormatMemorySize(process.WorkingSet64)) });
		details.Add(new() { Key = Resources.detail_path, Data = new DetailsLink(string.Empty, path) });

		if (showCommandLine && !string.IsNullOrWhiteSpace(commandLine))
		{
			details.Add(new() { Key = Resources.detail_command_line, Data = new DetailsLink(string.Empty, commandLine) });
		}

		return [.. details.Cast<IDetailsElement>()];
	}

	private const double KB = 1024;
	private const double MB = KB * 1024;
	private const double GB = MB * 1024;
	public static string FormatMemorySize(long mem) => (double)mem switch
	{
		< KB => $"{mem:0.##} B",
		< MB => $"{mem / KB:0.##} KB",
		< GB => $"{mem / MB:0.##} MB",
		_ => $"{mem / GB:0.##} GB"
	};
}

