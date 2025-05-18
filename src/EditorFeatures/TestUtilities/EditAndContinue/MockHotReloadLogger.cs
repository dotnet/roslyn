// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Debugger.Contracts.HotReload;

namespace Microsoft.CodeAnalysis.EditAndContinue.UnitTests;

internal class MockHotReloadLogger : IHotReloadLogger
{
    public ValueTask LogAsync(HotReloadLogMessage message, CancellationToken cancellation)
        => default;
}
