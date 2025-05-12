using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using ProcessKiller.Helpers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;

namespace ProcessKiller;
internal sealed class PortQuery
{
	public readonly Dictionary<string, Process> Query;

	/// <summary>
	/// parse output from <c>netstat.exe</c>
	/// </summary>
	public PortQuery()
	{
		var process = new Process
		{
			StartInfo = new()
			{
				Arguments = "-a -n -o",
				FileName = "netstat.exe",
				RedirectStandardOutput = true,
				WindowStyle = ProcessWindowStyle.Hidden,
			}
		};
		_ = process.Start();

		var processes = Process.GetProcesses().Where(p => !ProcessHelper.IsSystemProcess(p)).ToList();
		Query = [];
		foreach (var row in process.StandardOutput.ReadToEnd().Split("\r\n", StringSplitOptions.RemoveEmptyEntries).Skip(2))
		{
			var elements = row.Split(' ', StringSplitOptions.RemoveEmptyEntries);
			var localAddress = elements[1];
			var pid = int.Parse(elements.Length > 4 ? elements[4] : elements[3], CultureInfo.InvariantCulture);
			Process? pr = processes.FirstOrDefault(e => e.Id == pid);
			if (pr == null)
			{
				continue;
			}

			// There should be only one process using that address and port
			Query[localAddress] = pr;
		}
	}

	public IListItem[] GetItems(bool showCommandLine, IconInfo fallbackIcon)
	{
		CommandLineQuery? commandLineQuery = showCommandLine ? new() : null;

		IEnumerable<ListItem> results = Query
			.Select(e => (ListItem)new ProcessItem(e.Value, commandLineQuery, showCommandLine, fallbackIcon)
			{
				Title = e.Key,
			});

		return [.. results];
	}
}
