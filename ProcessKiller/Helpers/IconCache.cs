using Microsoft.CommandPalette.Extensions.Toolkit;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using Windows.Storage.Streams;

namespace ProcessKiller.Helpers;

/// <summary>
/// Caches icons by executable path, shared by every page and kept across invocations.
/// <para>
/// The shell call behind a thumbnail is the expensive part, and the same executable shows up many
/// times in a single listing - <see cref="Pages.PortPage"/> lists one item per socket, so a mail
/// client alone can repeat one icon hundreds of times.
/// </para>
/// <para>
/// One <see cref="IconInfo"/> serves every item with the same path. That is only safe because it
/// is backed by an <see cref="IconBytesReference"/>, which gives each read its own stream, so the
/// bitmap is stored once no matter how many rows show it.
/// </para>
/// <para>
/// Nothing here is ever disposed. Icons handed to the host have to stay readable for as long as
/// the host holds them and we get no notification of when that ends, so evicted entries are left
/// to the GC instead.
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
	// on every refresh. The fallback is substituted on the way out and never stored, so one cache
	// can serve pages that fall back to different icons.
	private readonly Dictionary<string, IconInfo?> _byPath = [];
	private readonly Queue<string> _insertionOrder = new();
	private readonly Lock _lock = new();

	/// <summary>
	/// Get the icon for <paramref name="path"/>, falling back to <paramref name="fallbackIcon"/>
	/// when there is no path or the shell has no thumbnail for it.
	/// </summary>
	public IconInfo GetIcon(string? path, IconInfo fallbackIcon)
	{
		if (path == null)
		{
			return fallbackIcon;
		}

		// The lock is held across the thumbnail call. Items within a refresh are built one at a
		// time anyway, so the only thing this can serialize is two pages refreshing at once, and
		// in exchange each path costs one shell call for the lifetime of the server.
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

		// Copy the bytes out and let the shell's stream go. Only the buffer is kept.
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
