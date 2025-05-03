using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using ProcessKiller.Helpers;
using ProcessKiller.Properties;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;

namespace ProcessKiller;
internal sealed partial class ProcessItem : ListItem
{
	public ProcessItem(Process process, CommandLineQuery? commandLineQuery, bool showCommandLine, IconInfo fallbackIcon) : base(new KillCommand(process))
	{
		(var iconFallback, var path) = TryGetProcessFilename(process);
		var commandLine = commandLineQuery?.GetCommandLine(process.Id);

		Title = $"{process.ProcessName} - {process.Id}";
		Subtitle = path;
		Icon = iconFallback ? fallbackIcon : new(path);
		Details = new Details()
		{
			Title = process.ProcessName,
			Body = $"{process.Id}",
			Metadata = BuildDetailsElement(process, path, showCommandLine, commandLine),
		};
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
			details.Add(new() { Key = Resources.plugin_tool_tip_main_window, Data = new DetailsLink(process.MainWindowTitle) });
		}

		details.Add(new() { Key = Resources.plugin_tool_tip_memory, Data = new DetailsLink(FormatMemorySize(process.WorkingSet64)) });
		details.Add(new() { Key = Resources.plugin_tool_tip_path, Data = new DetailsLink(path) });

		if (showCommandLine && !string.IsNullOrWhiteSpace(commandLine))
		{
			details.Add(new() { Key = Resources.plugin_tool_tip_command_line, Data = new DetailsLink(commandLine) });
		}

		return [.. details.Cast<IDetailsElement>()];
	}

	/// <summary>
	/// Try to get path of the process. If not, returns process name.
	/// </summary>
	/// <param name="p"></param>
	/// <returns></returns>
	private static (bool, string) TryGetProcessFilename(Process p)
	{
		try
		{
			var bufferSize = 2048;
			unsafe
			{
				var buffer = stackalloc char[bufferSize];
				var len = bufferSize;
				var ptr = NativeMethods.OpenProcess(
					NativeMethods.ProcessAccessFlags.QueryLimitedInformation,
					false,
					p.Id);
				return QueryFullProcessImageName(ptr, 0, out buffer, ref len) ?
					(false, new(buffer)) :
					(true, p.ProcessName);
			}
		}
		catch
		{
			return (true, p.ProcessName);
		}
	}

	[LibraryImport("kernel32.dll", SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static unsafe partial bool QueryFullProcessImageName(
		IntPtr hProcess,
		int dwFlags,
		out char* lpExeName,
		ref int lpdwSize);

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

