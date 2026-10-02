// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;
using System.Windows.Threading;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Threading;

public sealed class WpfFactTests
{
    [WpfFact]
    public async Task AsyncContinuationCanDisposeUiThreadAffineObject()
    {
        using var disposable = new DispatcherAffineDisposable();

        await Task.Yield();
    }

    private sealed class DispatcherAffineDisposable : DispatcherObject, IDisposable
    {
        public void Dispose()
            => Assert.True(CheckAccess(), "The object must be disposed on its owning Dispatcher thread.");
    }
}
