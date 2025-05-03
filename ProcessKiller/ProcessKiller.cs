// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CommandPalette.Extensions;
using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace ProcessKiller;

[Guid("b4089b42-6b7a-40fc-9ec1-3c79bf14e4ae")]
public sealed partial class ProcessKiller : IExtension, IDisposable
{
	private readonly ManualResetEvent _extensionDisposedEvent;
	private readonly ProcessKillerCommandsProvider _provider = new();

	public ProcessKiller(ManualResetEvent extensionDisposedEvent) => _extensionDisposedEvent = extensionDisposedEvent;

	public object? GetProvider(ProviderType providerType)
	{
		return providerType switch
		{
			ProviderType.Commands => _provider,
			_ => null,
		};
	}

	public void Dispose() => _extensionDisposedEvent.Set();
}
