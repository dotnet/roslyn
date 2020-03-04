// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis.Emit;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests.Emit
{
    public class EmitBaselineTests
    {
        [Fact]
        public void CreateInitialBaseline_Errors()
        {
            var debugInfoProvider = new Func<MethodDefinitionHandle, EditAndContinueMethodDebugInformation>(_ => default);
            var localSigProvider = new Func<MethodDefinitionHandle, StandaloneSignatureHandle>(_ => default);
            var peModule = ModuleMetadata.CreateFromImage(TestResources.Basic.Members);
            var peReader = peModule.Module.PEReaderOpt;

            var mdBytes = peReader.GetMetadata().GetContent();
            var mdBytesHandle = GCHandle.Alloc(mdBytes.DangerousGetUnderlyingArray(), GCHandleType.Pinned);
            var mdModule = ModuleMetadata.CreateFromMetadata(mdBytesHandle.AddrOfPinnedObject(), mdBytes.Length);

            Assert.Throws<ArgumentNullException>(() => EmitBaseline.CreateInitialBaseline(null, debugInfoProvider));
            Assert.Throws<ArgumentNullException>(() => EmitBaseline.CreateInitialBaseline(peModule, null));
            Assert.Throws<ArgumentException>(() => EmitBaseline.CreateInitialBaseline(mdModule, debugInfoProvider));

            Assert.Throws<ArgumentNullException>(() => EmitBaseline.CreateInitialBaseline(null, debugInfoProvider, localSigProvider, true));
            Assert.Throws<ArgumentNullException>(() => EmitBaseline.CreateInitialBaseline(peModule, null, localSigProvider, true));
            Assert.Throws<ArgumentNullException>(() => EmitBaseline.CreateInitialBaseline(mdModule, debugInfoProvider, null, true));
            Assert.NotNull(EmitBaseline.CreateInitialBaseline(mdModule, debugInfoProvider, localSigProvider, true));
        }
    }
}
