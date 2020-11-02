// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Shared.TestHooks
{
    internal sealed partial class AsynchronousOperationListenerProvider
    {
        private sealed class NullOperationListener : IAsynchronousOperationListener
        {
            public IAsyncToken BeginAsyncOperation(
                string name,
                object? tag = null,
                [CallerFilePath] string filePath = "",
                [CallerLineNumber] int lineNumber = 0) => EmptyAsyncToken.Instance;

            public async Task<bool> Delay(TimeSpan delay, CancellationToken cancellationToken)
            {
                await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
                return true;
            }
        }
    }
}
