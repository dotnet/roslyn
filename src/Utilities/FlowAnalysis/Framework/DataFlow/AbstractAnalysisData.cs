// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.CodeAnalysis.FlowAnalysis.DataFlow
{
    internal abstract class AbstractAnalysisData : IDisposable
    {
        public bool Disposed { get; private set; }

        protected virtual void Dispose(bool disposing)
        {
        }

#pragma warning disable CA1063 // Implement IDisposable Correctly - GC.SuppressFinalize invocation not necessary.
        public void Dispose()
#pragma warning restore CA1063 // Implement IDisposable Correctly
        {
            if (!Disposed)
            {
                Dispose(true);
                Disposed = true;
            }
        }
    }
}
