using System.Diagnostics;
using Windows.Win32;
using Windows.Win32.Foundation;

namespace ProcessKiller.Helpers;

internal static class ProcessHelper
{
	private static readonly HashSet<string> SystemProcessList =
	[
		"conhost",
		"svchost",
		"idle",
		"system",
		"rundll32",
		"csrss",
		"lsass",
		"lsm",
		"smss",
		"wininit",
		"winlogon",
		"services",
		"spoolsv",
		// Used by this Plugin
		"wmiprvse",
	];

	private static bool IsSystemProcess(Process p) => SystemProcessList.Contains(p.ProcessName.ToLower(System.Globalization.CultureInfo.CurrentCulture));

	public static List<Process> GetNonSystemProcesses(int? excludeProcessId = null)
	{
		List<Process> result = [];
		foreach (Process p in Process.GetProcesses())
		{
			if (IsSystemProcess(p) || p.Id == excludeProcessId)
			{
				p.Dispose();
				continue;
			}

			result.Add(p);
		}

		return result;
	}

	public static uint GetShellWindowId()
	{
		HWND hWnd = PInvoke.GetShellWindow();
		uint processId = 0;
		unsafe
		{
			_ = PInvoke.GetWindowThreadProcessId(hWnd, &processId);
		}

		return processId;
	}

	public static bool TryKill(Process p)
	{
		if (p.HasExited)
		{
			return false;
		}

		p.Kill();
		return p.WaitForExit(50);
	}

	/// <summary>
	/// Resolve a process by id and kill it. Items capture the id when the list is built, so by the
	/// time the command runs the process may be gone.
	/// </summary>
	public static bool TryKillById(int processId)
	{
		Process p;
		try
		{
			p = Process.GetProcessById(processId);
		}
		catch (ArgumentException)
		{
			// Nothing is running under that id any more.
			return false;
		}

		using (p)
		{
			return TryKill(p);
		}
	}
}
