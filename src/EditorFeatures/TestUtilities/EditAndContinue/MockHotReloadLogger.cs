// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Debugger.Contracts.HotReload;

namespace Microsoft.CodeAnalysis.EditAndContinue.UnitTests;

internal sealed class MockHotReloadLogger : IHotReloadLogger, IDisposable
{
    public void Dispose()
    {
    }

    public ValueTask LogAsync(HotReloadLogMessage message, CancellationToken cancellation)
        => default;
}
