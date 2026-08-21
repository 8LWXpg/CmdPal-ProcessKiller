using System.Diagnostics;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using ProcessKiller.Commands;
using ProcessKiller.Helpers;
using ProcessKiller.Properties;

namespace ProcessKiller;

/// <summary>
/// A snapshot of a process. Everything shown is read during construction, so the caller keeps
/// ownership and can dispose the process as soon as the item is built. The details are only
/// assembled when the host asks for them.
/// </summary>
internal sealed partial class ProcessItem : ListItem
{
	private readonly string _processName;
	private readonly string _path;
	private readonly string _mainWindowTitle;
	private readonly string _memory;
	private readonly string? _commandLine;
	private readonly IconInfo _icon;
	private IDetails? _details;

	public ProcessItem(Process process, string? executablePath, bool showCommandLine, IconCache iconCache, IconInfo fallbackIcon) : base(new KillCommand(process.Id))
	{
		var path = executablePath ?? process.ProcessName;

		_icon = iconCache.GetIcon(executablePath, fallbackIcon);

		Title = $"{process.ProcessName} - {process.Id}";
		Subtitle = path;
		Icon = _icon;

		// Read now, build later. The page disposes the process as soon as the list is built, so
		// the values have to be taken here even though the objects holding them are not needed
		// until a row is shown.
		_processName = process.ProcessName;
		_path = path;
		_mainWindowTitle = process.MainWindowTitle;
		_memory = FormatMemorySize(process.WorkingSet64);
		_commandLine = showCommandLine ? ProcessHelper.GetCommandLine(process) : null;

		MoreCommands = [
			new CommandContextItem(new KillAllCommand(process.ProcessName))
		];
	}

	/// <summary>
	/// Built on first read. Only the highlighted row is ever shown, so building this for every
	/// row costs several objects each for the host to marshal and then discard.
	/// </summary>
	public override IDetails? Details => _details ??= BuildDetails();

	private Details BuildDetails()
	{
		List<DetailsElement> details = [];

		if (!string.IsNullOrWhiteSpace(_mainWindowTitle))
		{
			details.Add(new() { Key = Resources.detail_main_window, Data = new DetailsLink(string.Empty, _mainWindowTitle) });
		}

		details.Add(new() { Key = Resources.detail_memory, Data = new DetailsLink(string.Empty, _memory) });
		details.Add(new() { Key = Resources.detail_path, Data = new DetailsLink(string.Empty, _path) });

		if (!string.IsNullOrWhiteSpace(_commandLine))
		{
			details.Add(new() { Key = Resources.detail_command_line, Data = new DetailsLink(string.Empty, _commandLine) });
		}

		return new Details()
		{
			Title = _processName,
			HeroImage = _icon,
			Metadata = [.. details.Cast<IDetailsElement>()],
		};
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

