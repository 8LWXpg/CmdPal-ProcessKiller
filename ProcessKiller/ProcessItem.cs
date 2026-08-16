using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using Microsoft.Win32.SafeHandles;
using ProcessKiller.Commands;
using ProcessKiller.Helpers;
using ProcessKiller.Properties;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Windows.Win32;
using Windows.Win32.System.Threading;

namespace ProcessKiller;

/// <summary>
/// A snapshot of a process. Everything shown is read out of <paramref name="process"/> during
/// construction, so the caller stays the owner and is free to dispose it as soon as the item is
/// built. The commands carry the id and name rather than the <see cref="Process"/> itself, which
/// matters because one process backs many items on <see cref="Pages.PortPage"/>.
/// </summary>
internal sealed partial class ProcessItem : ListItem
{
	public ProcessItem(Process process, CommandLineQuery? commandLineQuery, bool showCommandLine, IconCache iconCache, IconInfo fallbackIcon) : base(new KillCommand(process.Id))
	{
		var gotPath = TryGetProcessFilename(process, out var path);
		var commandLine = commandLineQuery?.GetCommandLine(process.Id);

		Title = $"{process.ProcessName} - {process.Id}";
		Subtitle = path;
		Icon = (gotPath ? iconCache.GetIcon(path) : null) ?? fallbackIcon;

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

	/// <summary>
	/// Try to get path of the process. If not, returns process name.
	/// </summary>
	/// <param name="p"></param>
	/// <returns></returns>
	public static bool TryGetProcessFilename(Process p, out string path)
	{
		uint bufferSize = 2048;
		Span<char> buffer = stackalloc char[(int)bufferSize];
		var len = bufferSize;
		using SafeFileHandle handle = PInvoke.OpenProcess_SafeHandle(PROCESS_ACCESS_RIGHTS.PROCESS_QUERY_LIMITED_INFORMATION, false, (uint)p.Id);
		var success = (bool)PInvoke.QueryFullProcessImageName(handle, 0, buffer, ref len);
		path = success ? new string(buffer[..(int)len]) : p.ProcessName;
		return success;
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

