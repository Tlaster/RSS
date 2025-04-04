// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.CommandPalette.Extensions;

namespace RSS;

[Guid("4b7a17d9-abf6-4358-a0c5-37cc9b84ebe0")]
public sealed partial class RSS : IExtension, IDisposable
{
    private readonly ManualResetEvent _extensionDisposedEvent;

    private readonly RSSCommandsProvider _provider = new();

    public RSS(ManualResetEvent extensionDisposedEvent)
    {
        _extensionDisposedEvent = extensionDisposedEvent;
    }

    public object? GetProvider(ProviderType providerType)
    {
        return providerType switch
        {
            ProviderType.Commands => _provider,
            _ => null
        };
    }

    public void Dispose()
    {
        _extensionDisposedEvent.Set();
    }
}