using System.Runtime.InteropServices.WindowsRuntime;
using ProcessKiller.Helpers;
using Windows.Storage.Streams;

namespace ProcessKiller.Tests;

public class IconBytesReferenceTests
{
	private const int IconSize = 8192;

	// Every row sharing one icon opens the same reference, and the host opens them all at once.
	private const int Rows = 300;

	/// <summary>
	/// Caching a live stream instead of the bytes passes when opens are sequential and silently
	/// drops reads when they are not, which is why this asserts the concurrent case.
	/// </summary>
	[Fact]
	public async Task EveryConcurrentOpenReadsTheWholeIcon()
	{
		var bytes = new byte[IconSize];
		Random.Shared.NextBytes(bytes);
		IconBytesReference shared = new(bytes);

		byte[][] reads = await Task.WhenAll(Enumerable.Range(0, Rows)
			.Select(_ => Task.Run(async () => await ReadAll(await shared.OpenReadAsync()))));

		// Counted rather than compared per row: the failure mode is a handful of short reads,
		// so "287 of 300" is the useful message.
		Assert.Equal(Rows, reads.Count(r => r.AsSpan().SequenceEqual(bytes)));
	}

	/// <summary>
	/// Reads until the stream stops yielding, so a short read means the data was unreachable
	/// rather than merely split across calls. A racing reader can also hand back a buffer that
	/// throws on access, which counts as a failed read like any other.
	/// </summary>
	private static async Task<byte[]> ReadAll(IRandomAccessStream stream)
	{
		using (stream)
		{
			IInputStream input = stream.GetInputStreamAt(0);
			List<byte> read = new(IconSize);
			try
			{
				while (read.Count < IconSize)
				{
					var remaining = (uint)(IconSize - read.Count);
					IBuffer buffer = await input.ReadAsync(new Windows.Storage.Streams.Buffer(remaining), remaining, InputStreamOptions.None);
					if (buffer.Length == 0)
					{
						break;
					}

					read.AddRange(buffer.ToArray());
				}
			}
			catch (Exception)
			{
				return [];
			}

			return [.. read];
		}
	}
}
