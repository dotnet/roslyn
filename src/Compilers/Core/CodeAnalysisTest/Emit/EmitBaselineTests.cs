// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests.Emit
{
    public class EmitBaselineTests
    {
        [Fact]
        public unsafe void CreateInitialBaseline_Errors()
        {
            var debugInfoProvider = new Func<MethodDefinitionHandle, EditAndContinueMethodDebugInformation>(_ => default);
            var localSigProvider = new Func<MethodDefinitionHandle, StandaloneSignatureHandle>(_ => default);
            var peModule = ModuleMetadata.CreateFromImage(TestResources.Basic.Members);
            var peReader = peModule.Module.PEReaderOpt;
            var compilation = CSharpCompilation.Create("test");

            var mdBytes = peReader.GetMetadata().GetContent();
            fixed (byte* mdBytesPointer = mdBytes.AsSpan())
            {
                var mdModule = ModuleMetadata.CreateFromMetadata((IntPtr)mdBytesPointer, mdBytes.Length);

                Assert.Throws<ArgumentNullException>(() => EmitBaseline.CreateInitialBaseline(null, peModule, debugInfoProvider, localSigProvider, true));
                Assert.Throws<ArgumentNullException>(() => EmitBaseline.CreateInitialBaseline(compilation, null, debugInfoProvider, localSigProvider, true));
                Assert.Throws<ArgumentNullException>(() => EmitBaseline.CreateInitialBaseline(compilation, peModule, null, localSigProvider, true));
                Assert.Throws<ArgumentNullException>(() => EmitBaseline.CreateInitialBaseline(compilation, mdModule, debugInfoProvider, null, true));
                Assert.NotNull(EmitBaseline.CreateInitialBaseline(compilation, mdModule, debugInfoProvider, localSigProvider, true));
            }
        }
    }
}
