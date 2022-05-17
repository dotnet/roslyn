// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;
using static Microsoft.CodeAnalysis.Host.TemporaryStorageServiceFactory;

namespace Microsoft.CodeAnalysis.UnitTests.WorkspaceServiceTests
{
    [Trait(Traits.Feature, Traits.Features.Workspace)]
    public sealed class DirectMemoryAccessStreamReaderTests
    {
        [Fact]
        public void PeakRead()
        {
            using var _ = CreateReader("text", out var reader);
            Assert.Equal('t', reader.Read());
            Assert.Equal('e', reader.Peek());
            Assert.Equal('e', reader.Read());
            Assert.Equal('x', reader.Read());
            Assert.Equal('t', reader.Read());
            Assert.Equal(-1, reader.Peek());
            Assert.Equal(-1, reader.Read());
        }

        private static Disposer CreateReader(string text, out DirectMemoryAccessStreamReader reader)
        {
            var handle = GCHandle.Alloc(text.ToCharArray(), GCHandleType.Pinned);
            reader = DirectMemoryAccessStreamReader.TestAccessor.Create(handle.AddrOfPinnedObject(), text.Length);

#pragma warning disable RS0042 // Do not copy value
            return new Disposer(handle, reader);
#pragma warning restore RS0042 // Do not copy value
        }

        private sealed class Disposer : IDisposable
        {
            private GCHandle _handle;
            private readonly TextReader _textReader;

            public Disposer(GCHandle handle, TextReader textReader)
            {
#pragma warning disable RS0042 // Do not copy value
                _handle = handle;
#pragma warning restore RS0042 // Do not copy value
                _textReader = textReader;
            }

            public void Dispose()
            {
                _textReader.Dispose();
                _handle.Free();
            }
        }
    }
}
