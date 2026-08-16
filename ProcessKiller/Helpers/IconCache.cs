using Microsoft.CommandPalette.Extensions.Toolkit;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Storage.Streams;

namespace ProcessKiller.Helpers;

/// <summary>
/// Caches icons by executable path, shared by every page and kept for the lifetime of the server.
/// Entries are never disposed, the host reads an icon whenever it repaints and gives no signal
/// when it is done with one.
/// </summary>
internal sealed class IconCache
{
	// Distinct executables on a busy machine, capped so a long lived server cannot grow forever.
	private const int Capacity = 512;

	// null means the shell had no thumbnail, cached so the miss is not retried on every refresh.
	// The caller's fallback is substituted on return and never stored, so pages that fall back to
	// different icons can share one cache.
	private readonly Dictionary<string, IconInfo?> _byPath = [];
	private readonly Queue<string> _insertionOrder = new();
	private readonly Lock _lock = new();

	/// <summary>
	/// Get the icon for <paramref name="path"/>, or <paramref name="fallbackIcon"/> when there is
	/// no path or no thumbnail for it.
	/// </summary>
	public IconInfo GetIcon(string? path, IconInfo fallbackIcon)
	{
		if (path == null)
		{
			return fallbackIcon;
		}

		// Held across the thumbnail call. Items are built one at a time, so the most this can
		// serialize is two pages refreshing at once.
		lock (_lock)
		{
			if (!_byPath.TryGetValue(path, out IconInfo? icon))
			{
				icon = BuildIcon(path);

				if (_insertionOrder.Count >= Capacity)
				{
					_ = _byPath.Remove(_insertionOrder.Dequeue());
				}

				_byPath[path] = icon;
				_insertionOrder.Enqueue(path);
			}

			return icon ?? fallbackIcon;
		}
	}

	private static IconInfo? BuildIcon(string path)
	{
		// https://github.com/microsoft/PowerToys/issues/39485
		IRandomAccessStream? thumbnail = ThumbnailHelper.GetThumbnail(path).GetAwaiter().GetResult();
		if (thumbnail == null)
		{
			return null;
		}

		using (thumbnail)
		{
			var size = (uint)thumbnail.Size;
			if (size == 0)
			{
				return null;
			}

			IBuffer buffer = thumbnail
				.GetInputStreamAt(0)
				.ReadAsync(new Windows.Storage.Streams.Buffer(size), size, InputStreamOptions.None)
				.GetAwaiter()
				.GetResult();

			var data = new IconData(new IconBytesReference(buffer.ToArray()));
			return new IconInfo(data, data);
		}
	}
}
