using System.Diagnostics;
using Microsoft.Win32.SafeHandles;
using Windows.Wdk.System.Threading;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.Threading;

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

	private static bool IsSystemProcess(Process p) => SystemProcessList.Contains(p.ProcessName, StringComparer.OrdinalIgnoreCase);

	public static List<Process> GetNonSystemProcesses(int? excludeProcessId = null)
	{
		Process[] processes = Process.GetProcesses();
		List<Process> result = new(processes.Length);
		foreach (Process p in processes)
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

	/// <summary>
	/// Command line of a process, or null when it cannot be read.
	/// </summary>
	public static unsafe string? GetCommandLine(Process p)
	{
		using SafeFileHandle handle = PInvoke.OpenProcess_SafeHandle(PROCESS_ACCESS_RIGHTS.PROCESS_QUERY_LIMITED_INFORMATION, false, (uint)p.Id);
		if (handle.IsInvalid)
		{
			return null;
		}

		HANDLE process = (HANDLE)handle.DangerousGetHandle();
		uint size = 0;

		// The first call only reports how big the answer is.
		if (Windows.Wdk.PInvoke.NtQueryInformationProcess(process, PROCESSINFOCLASS.ProcessCommandLineInformation, null, 0, ref size) != NTSTATUS.STATUS_INFO_LENGTH_MISMATCH || size == 0)
		{
			return null;
		}

		var buffer = new byte[size];
		fixed (byte* info = buffer)
		{
			if (Windows.Wdk.PInvoke.NtQueryInformationProcess(process, PROCESSINFOCLASS.ProcessCommandLineInformation, info, size, ref size).SeverityCode != NTSTATUS.Severity.Success)
			{
				return null;
			}

			// The text follows the UNICODE_STRING header, whose Buffer points back into it.
			UNICODE_STRING* value = (UNICODE_STRING*)info;
			return value->Length == 0 ? null : new string(value->Buffer, 0, value->Length / 2);
		}
	}

	/// <summary>
	/// Full path of the process image, or null when it cannot be read, which is normal for
	/// elevated processes.
	/// </summary>
	public static string? GetExecutablePath(Process p)
	{
		uint bufferSize = 2048;
		Span<char> buffer = stackalloc char[(int)bufferSize];
		var len = bufferSize;
		using SafeFileHandle handle = PInvoke.OpenProcess_SafeHandle(PROCESS_ACCESS_RIGHTS.PROCESS_QUERY_LIMITED_INFORMATION, false, (uint)p.Id);
		return (bool)PInvoke.QueryFullProcessImageName(handle, 0, buffer, ref len)
			? new string(buffer[..(int)len])
			: null;
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
	/// Kill the process with this id. Items capture the id when the list is built, so it may
	/// already be gone by the time the command runs.
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
			return false;
		}

		using (p)
		{
			return TryKill(p);
		}
	}
}
