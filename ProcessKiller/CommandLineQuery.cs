using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;

namespace ProcessKiller;
/// <summary>
/// Query all running processes and their command lines using WMI.
/// A lot faster then querying each process individually
/// </summary>
internal sealed class CommandLineQuery
{
	public readonly Dictionary<int, string?> query = [];

	public CommandLineQuery()
	{
		using Process proc = Process.Start(new ProcessStartInfo("wmic", "process get ProcessId,CommandLine /format:csv")
		{
			RedirectStandardOutput = true,
			UseShellExecute = false,
			CreateNoWindow = true,
		}) ?? throw new InvalidOperationException("Failed to start wmic");
		using StreamReader reader = proc.StandardOutput;

		// Skip header line
		_ = reader.ReadLine();
		_ = reader.ReadLine();

		while (reader.ReadLine() is { } line)
		{
			if (string.IsNullOrWhiteSpace(line))
			{
				continue;
			}

			// CSV format: Node,CommandLine,ProcessId
			var parts = line.Split(',', 3);
			if (!int.TryParse(parts[2].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var pid))
			{
				continue;
			}

			var commandLine = string.IsNullOrWhiteSpace(parts[1]) ? null : parts[1].Trim();
			query[pid] = commandLine;
		}

		proc.WaitForExit();
	}

	public string? GetCommandLine(int processId) => query.GetValueOrDefault(processId);
}
