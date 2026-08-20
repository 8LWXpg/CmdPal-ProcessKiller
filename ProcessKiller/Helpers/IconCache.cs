using Microsoft.CommandPalette.Extensions.Toolkit;
using System.Collections.Concurrent;
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
	// A null result means the shell had no thumbnail, cached so the miss is not retried on every
	// refresh. The caller's fallback is substituted on return and never stored, so pages that fall
	// back to different icons can share one cache.
	//
	// Storing the Lazy rather than the icon is what keeps the thumbnail call outside the
	// dictionary's lock: one path is still only ever fetched once, but different paths resolve at
	// the same time.
	private readonly ConcurrentDictionary<string, Lazy<IconInfo?>> _byPath = new();

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

	private Lazy<IconInfo?> Entry(string path)
		=> _byPath.GetOrAdd(path, p => new Lazy<IconInfo?>(() => BuildIcon(p), LazyThreadSafetyMode.ExecutionAndPublication));

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
