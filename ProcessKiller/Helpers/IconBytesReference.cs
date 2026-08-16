using System;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Storage.Streams;

namespace ProcessKiller.Helpers;

/// <summary>
/// A stream reference over a fixed buffer that mints a fresh stream on every open.
/// <para>
/// <see cref="RandomAccessStreamReference.CreateFromStream"/> wraps one live stream and every
/// open reads that same one, so sharing a single icon across many list items had the host reading
/// it concurrently and rows that lost the race rendered blank. Cloning does not help, clones
/// still coordinate through the parent. Holding the bytes and building a new stream per open
/// makes readers fully independent, which is what lets one icon back any number of items.
/// </para>
/// </summary>
internal sealed partial class IconBytesReference(byte[] bytes) : IRandomAccessStreamReference
{
	private readonly byte[] _bytes = bytes;

	public IAsyncOperation<IRandomAccessStreamWithContentType> OpenReadAsync()
	{
		InMemoryRandomAccessStream stream = new();
		_ = stream.WriteAsync(_bytes.AsBuffer()).AsTask().GetAwaiter().GetResult();
		stream.Seek(0);

		// CreateFromStream only has to supply the content type wrapper here, the stream it wraps
		// is this call's own and is never handed to anyone else.
		return RandomAccessStreamReference.CreateFromStream(stream).OpenReadAsync();
	}
}
