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
        protected override unsafe IDisposable CreateReader(string text, out TextReader reader)
        {
            var pointer = Marshal.StringToHGlobalUni(text);
            reader = new DirectMemoryAccessStreamReader((char*)pointer, text.Length);
            return new Disposer(pointer, reader);
        }

        private sealed class Disposer : IDisposable
        {
            private readonly IntPtr _pointer;
            private readonly TextReader _textReader;

            public Disposer(IntPtr pointer, TextReader textReader)
            {
                _pointer = pointer;
                _textReader = textReader;
            }

            public void Dispose()
            {
                _textReader.Dispose();
                Marshal.FreeHGlobal(_pointer);
            }
        }
    }
}
