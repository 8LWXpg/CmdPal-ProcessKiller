using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using Microsoft.Win32.SafeHandles;
using ProcessKiller.Commands;
using ProcessKiller.Properties;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Windows.Storage.Streams;
using Windows.Win32;
using Windows.Win32.System.Threading;

namespace ProcessKiller;

internal sealed partial class ProcessItem : ListItem
{
	public ProcessItem(Process process, CommandLineQuery? commandLineQuery, bool showCommandLine, IconInfo fallbackIcon) : base(new KillCommand(process))
	{
		var gotPath = TryGetProcessFilename(process, out var path);
		var commandLine = commandLineQuery?.GetCommandLine(process.Id);

		Title = $"{process.ProcessName} - {process.Id}";
		Subtitle = path;
		// https://github.com/microsoft/PowerToys/issues/39485
		if (gotPath)
		{
			IRandomAccessStream? stream = ThumbnailHelper.GetThumbnail(path).GetAwaiter().GetResult();
			if (stream != null)
			{
				var data = new IconData(RandomAccessStreamReference.CreateFromStream(stream));
				Icon = new IconInfo(data, data);
			}
			else
			{
				Icon = fallbackIcon;
			}
		}
		else
		{
			Icon = fallbackIcon;
		}

		Details = new Details()
		{
			Title = process.ProcessName,
			HeroImage = Icon,
			Metadata = BuildDetailsElement(process, path, showCommandLine, commandLine),
		};

		MoreCommands = [
			new CommandContextItem(new KillAllCommand(process))
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

