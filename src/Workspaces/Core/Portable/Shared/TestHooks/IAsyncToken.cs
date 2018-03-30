// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.CodeAnalysis.Shared.TestHooks
{
    internal interface IAsyncToken : IDisposable
    {
    }

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
