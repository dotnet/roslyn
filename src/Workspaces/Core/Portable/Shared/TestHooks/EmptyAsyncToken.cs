// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.Shared.TestHooks
{
    internal sealed class EmptyAsyncToken : IAsyncToken
    {
        public static IAsyncToken Instance { get; } = new EmptyAsyncToken();

        private EmptyAsyncToken()
        {
        }

        public void Dispose()
        {
            // Empty by design requirement: operations which use IAsyncToken are free to optimize code sequences by
            // eliding calls to EmptyAsyncToken.Dispose() with the understanding that it doesn't do anything.
        }
    }
}
