// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.Shared.TestHooks
{
    internal sealed partial class AsynchronousOperationListenerProvider
    {
        private sealed class NullListenerProvider : IAsynchronousOperationListenerProvider
        {
            public IAsynchronousOperationListener GetListener(string featureName) => NullListener;
        }
    }
}
