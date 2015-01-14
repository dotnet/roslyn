// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.CodeAnalysis.Internal.Log
{
    /// <summary>
    /// no op log block
    /// </summary>
    internal sealed class EmptyLogBlock : IDisposable
    {
        public static readonly EmptyLogBlock Instance = new EmptyLogBlock();

        public void Dispose()
        {
        }
    }
}
