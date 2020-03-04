// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.DiaSymReader;

namespace Microsoft.CodeAnalysis.EditAndContinue
{
    internal sealed class DebuggeeModuleInfo : IDisposable
    {
        public ModuleMetadata Metadata { get; }

        private ISymUnmanagedReader5 _symReader;
        public ISymUnmanagedReader5 SymReader => _symReader;

        public DebuggeeModuleInfo(ModuleMetadata metadata, ISymUnmanagedReader5 symReader)
        {
            Debug.Assert(metadata != null);
            Debug.Assert(symReader != null);

            Metadata = metadata;
            _symReader = symReader;
        }

        public void Dispose()
        {
            Metadata?.Dispose();

            var symReader = Interlocked.Exchange(ref _symReader, null);
            if (symReader != null && Marshal.IsComObject(symReader))
            {
                Marshal.ReleaseComObject(symReader);
            }
        }
    }
}
