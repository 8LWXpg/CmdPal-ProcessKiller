using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using ProcessKiller.Properties;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Windows.Win32;
using Windows.Win32.System.Threading;

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
			HeroImage = Icon,
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
	private static (bool, string) TryGetProcessFilename(Process p)
	{
		try
		{
			unsafe
			{
				var bufferSize = 512;
				unsafe
				{
					Span<char> buffer = stackalloc char[bufferSize];
					var len = (uint)bufferSize;
					return PInvoke.QueryFullProcessImageName(p.SafeHandle, PROCESS_NAME_FORMAT.PROCESS_NAME_WIN32, buffer, ref len)
						? (false, new(buffer[..buffer.IndexOf('\0')]))
						: ((bool, string))(true, p.ProcessName);
				}
			}
		}
		catch
		{
			return (true, p.ProcessName);
		}
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

