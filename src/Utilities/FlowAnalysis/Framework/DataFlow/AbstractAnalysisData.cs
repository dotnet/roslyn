// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.CodeAnalysis.FlowAnalysis.DataFlow
{
    internal abstract class AbstractAnalysisData : IDisposable
    {
        public bool IsDisposed { get; private set; }

        protected virtual void Dispose(bool disposing)
        {
            IsDisposed = true;
        }

#pragma warning disable CA1063 // Implement IDisposable Correctly - We want to ensure that we cleanup managed resources even when object was not explicitly disposed.
        ~AbstractAnalysisData()
#pragma warning restore CA1063 // Implement IDisposable Correctly
        {
            Dispose(true);  // We want to explicitly cleanup managed resources, so pass 'true'
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
