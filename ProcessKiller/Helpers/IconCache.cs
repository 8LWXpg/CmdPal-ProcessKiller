using Microsoft.CommandPalette.Extensions.Toolkit;
using System.Collections.Generic;
using System.Threading;
using Windows.Storage.Streams;

namespace ProcessKiller.Helpers;

/// <summary>
/// Caches icons by executable path, shared by every page and kept across invocations.
/// <para>
/// Building one costs a shell thumbnail call plus an in-memory stream, and the same executable
/// shows up many times in a single listing - <see cref="Pages.PortPage"/> lists one item per
/// socket, so a browser alone can repeat one icon hundreds of times.
/// </para>
/// <para>
/// Nothing here is ever disposed. An <see cref="IconInfo"/> handed to the host has to stay
/// readable for as long as the host holds it, and we get no notification of when that ends, so
/// evicted entries are left to the GC instead.
/// </para>
/// </summary>
internal sealed class IconCache
{
	/// <summary>
	/// Roughly the number of distinct executables a busy machine runs. Paths repeat across
	/// invocations, so the cap is only there to stop unbounded growth in a long lived server.
	/// </summary>
	private const int Capacity = 512;

	// null value means "the shell has no thumbnail for this path", cached so a miss is not retried
	// on every refresh. Callers substitute their own fallback icon.
	private readonly Dictionary<string, IconInfo?> _byPath = [];
	private readonly Queue<string> _insertionOrder = new();
	private readonly Lock _lock = new();

	/// <summary>
	/// Get the icon for <paramref name="path"/>, or <see langword="null"/> if the shell has no
	/// thumbnail for it.
	/// </summary>
	public IconInfo? GetIcon(string path)
	{
		lock (_lock)
		{
			if (_byPath.TryGetValue(path, out IconInfo? cached))
			{
				return cached;
			}
		}

		// Fetched outside the lock, a thumbnail call goes out to the shell and can block.
		IconInfo? icon = BuildIcon(path);

		lock (_lock)
		{
			// A concurrent refresh may have gotten there first. Prefer its entry so a path keeps
			// mapping to a single icon and ours is dropped.
			if (_byPath.TryGetValue(path, out IconInfo? cached))
			{
				return cached;
			}

			if (_insertionOrder.Count >= Capacity)
			{
				_ = _byPath.Remove(_insertionOrder.Dequeue());
			}

			_byPath[path] = icon;
			_insertionOrder.Enqueue(path);
		}

		return icon;
	}

	private static IconInfo? BuildIcon(string path)
	{
		// https://github.com/microsoft/PowerToys/issues/39485
		IRandomAccessStream? stream = ThumbnailHelper.GetThumbnail(path).GetAwaiter().GetResult();
		if (stream == null)
		{
			return null;
		}

		var data = new IconData(RandomAccessStreamReference.CreateFromStream(stream));
		return new IconInfo(data, data);
	}
}
