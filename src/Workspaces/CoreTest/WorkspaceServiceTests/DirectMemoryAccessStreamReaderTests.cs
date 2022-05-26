// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;
using static Microsoft.CodeAnalysis.Host.TemporaryStorageServiceFactory;

namespace Microsoft.CodeAnalysis.UnitTests.WorkspaceServiceTests
{
    [Trait(Traits.Feature, Traits.Features.Workspace)]
    public sealed class DirectMemoryAccessStreamReaderTests : TextReaderTestBase
    {
        protected override unsafe (IDisposable? disposer, TextReader reader) CreateReader(string text)
        {
            var pointer = Marshal.StringToHGlobalUni(text);
            return (new Disposer(pointer), new DirectMemoryAccessStreamReader((char*)pointer, text.Length));
        }

        private sealed class Disposer : IDisposable
        {
            private readonly IntPtr _pointer;

            public Disposer(IntPtr pointer)
            {
                _pointer = pointer;
            }

            public void Dispose()
            {
                Marshal.FreeHGlobal(_pointer);
            }
        }
    }
}
