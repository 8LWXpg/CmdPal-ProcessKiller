using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using ProcessKiller.Helpers;
using System.Diagnostics;
using System.Globalization;

namespace ProcessKiller;

internal sealed partial class PortQuery : IDisposable
{
	public readonly Dictionary<string, Process> Query;

	// Owns what Query points at: one process holds as many local addresses as it has sockets.
	private readonly List<Process> _processes = [];

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

		Dictionary<int, Process> byId = [];
		foreach (Process p in processes)
		{
			byId[p.Id] = p;
		}

		Query = [];
		HashSet<int> keptIds = [];
		foreach (var row in output.Split("\r\n", StringSplitOptions.RemoveEmptyEntries).Skip(2))
		{
			var elements = row.Split(' ', StringSplitOptions.RemoveEmptyEntries);
			var localAddress = elements[1];
			var pid = int.Parse(elements.Length > 4 ? elements[4] : elements[3], CultureInfo.InvariantCulture);
			if (!byId.TryGetValue(pid, out Process? pr))
			{
				continue;
			}

			// There should be only one process using that address and port
			Query[localAddress] = pr;
			_ = keptIds.Add(pid);
		}

		// Keep the processes Query points at, dispose the ones nothing referenced.
		foreach (Process p in processes)
		{
			if (keptIds.Contains(p.Id))
			{
				_processes.Add(p);
			}
			else
			{
				p.Dispose();
			}
		}
	}

	public IListItem[] GetItems(bool showCommandLine, IconCache iconCache, IconInfo fallbackIcon)
	{
		// wmic costs more than everything else here put together, so start it first and collect it
		// once the icons are ready.
		Task<CommandLineQuery>? commandLines = showCommandLine
			? Task.Run(() => new CommandLineQuery())
			: null;

		// Once per process rather than once per socket, since Query repeats a process per address.
		Dictionary<int, string?> pathById = [];
		foreach (Process p in _processes)
		{
			pathById[p.Id] = ProcessHelper.GetExecutablePath(p);
		}

		iconCache.Prefetch(pathById.Values);

		CommandLineQuery? commandLineQuery = commandLines?.GetAwaiter().GetResult();

		return [.. Query.Select(e => new ProcessItem(e.Value, pathById[e.Value.Id], commandLineQuery, showCommandLine, iconCache, fallbackIcon)
		{
			Title = e.Key,
		})];
	}

	public void Dispose()
	{
		foreach (Process p in _processes)
		{
			p.Dispose();
		}

		_processes.Clear();
	}
}
