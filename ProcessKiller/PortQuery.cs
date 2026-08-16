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
		using var process = new Process
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

		List<Process> processes = ProcessHelper.GetNonSystemProcesses();
		var output = process.StandardOutput.ReadToEnd();
		process.WaitForExit();

		Query = [];
		HashSet<int> keptIds = [];
		foreach (var row in output.Split("\r\n", StringSplitOptions.RemoveEmptyEntries).Skip(2))
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
			_ = keptIds.Add(pr.Id);
		}

		// Dispose the processes we queried but didn't end up keeping in Query.
		foreach (Process p in processes)
		{
			if (!keptIds.Contains(p.Id))
			{
				p.Dispose();
			}
		}
	}

	public List<ProcessItem> GetItems(bool showCommandLine, IconInfo fallbackIcon)
	{
		CommandLineQuery? commandLineQuery = showCommandLine ? new() : null;

		return [.. Query.Select(e => new ProcessItem(e.Value, commandLineQuery, showCommandLine, fallbackIcon)
		{
			Title = e.Key,
		})];
	}
}
