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
	private const int Capacity = 512;

	// A null result means the shell had no thumbnail, cached so the miss is not retried on every
	// refresh. The caller's fallback is substituted on return and never stored, so pages that fall
	// back to different icons can share one cache.
	private readonly Dictionary<string, Lazy<IconInfo?>> _byPath = [];
	private readonly Queue<string> _insertionOrder = new();
	private readonly Lock _lock = new();

	/// <summary>
	/// Get the icon for <paramref name="path"/>, or <paramref name="fallbackIcon"/> when there is
	/// no path or no thumbnail for it.
	/// </summary>
	public IconInfo GetIcon(string? path, IconInfo fallbackIcon)
		=> (path is null ? null : Entry(path).Value) ?? fallbackIcon;

	/// <summary>
	/// Fetch <paramref name="paths"/> at once. Callers build their items one at a time, which
	/// would otherwise fetch one thumbnail at a time; the shell handles them far better in
	/// parallel. Nulls and repeats are ignored.
	/// </summary>
	public void Prefetch(IEnumerable<string?> paths)
		=> Parallel.ForEach(paths.OfType<string>().Distinct(), path => _ = Entry(path).Value);

	/// <summary>
	/// The cache slot for a path. The lock covers the dictionary only, never the thumbnail call,
	/// so different paths resolve in parallel while one path is still only ever fetched once.
	/// </summary>
	private Lazy<IconInfo?> Entry(string path)
	{
		lock (_lock)
		{
			if (_byPath.TryGetValue(path, out Lazy<IconInfo?>? icon))
			{
				return icon;
			}

			icon = new Lazy<IconInfo?>(() => BuildIcon(path), LazyThreadSafetyMode.ExecutionAndPublication);

			if (_insertionOrder.Count >= Capacity)
			{
				_ = _byPath.Remove(_insertionOrder.Dequeue());
			}

			_byPath[path] = icon;
			_insertionOrder.Enqueue(path);
			return icon;
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
