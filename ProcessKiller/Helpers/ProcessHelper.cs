using System;
using System.Collections.Generic;
using System.Diagnostics;

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

	public static bool IsSystemProcess(Process p) => SystemProcessList.Contains(p.ProcessName.ToLower(System.Globalization.CultureInfo.CurrentCulture));

	public static uint GetProcessIDFromWindowHandle(IntPtr hwnd)
	{
		_ = NativeMethods.GetWindowThreadProcessId(hwnd, out var processId);
		return processId;
	}

	public static bool TryKill(Process p)
	{
		try
		{
			if (!p.HasExited)
			{
				p.Kill();
				return p.WaitForExit(50);
			}
		}
		catch (Exception)
		{
			throw;
		}

		return false;
	}
}
