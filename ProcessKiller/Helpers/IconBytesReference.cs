using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Storage.Streams;

namespace ProcessKiller.Helpers;

/// <summary>
/// A stream reference backed by a fixed buffer. A new stream is opened for each request, allowing multiple
/// items to share the same icon. <see cref="RandomAccessStreamReference.CreateFromStream"/> is not suitable
/// because it wraps a single live stream, and concurrent opens on that reference returns an empty stream.
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
