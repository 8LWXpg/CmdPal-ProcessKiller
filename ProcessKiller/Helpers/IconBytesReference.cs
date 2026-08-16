using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Storage.Streams;

namespace ProcessKiller.Helpers;

/// <summary>
/// A stream reference over a fixed buffer, opening a new stream each time so any number of items
/// can share one icon. <see cref="RandomAccessStreamReference.CreateFromStream"/> is not a
/// substitute: it wraps one live stream, and concurrent opens of it read empty.
/// </summary>
internal sealed partial class IconBytesReference(byte[] bytes) : IRandomAccessStreamReference
{
	private readonly byte[] _bytes = bytes;

	public IAsyncOperation<IRandomAccessStreamWithContentType> OpenReadAsync()
	{
		InMemoryRandomAccessStream stream = new();
		_ = stream.WriteAsync(_bytes.AsBuffer()).AsTask().GetAwaiter().GetResult();
		stream.Seek(0);

		// Only for the content type wrapper, the stream it wraps belongs to this call alone.
		return RandomAccessStreamReference.CreateFromStream(stream).OpenReadAsync();
	}
}
