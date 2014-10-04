// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis.Emit;
using Xunit;
using System.Reflection.Metadata;

namespace Microsoft.CodeAnalysis.UnitTests.Emit
{
    public class EmitBaselineTests
    {
        [Fact]
        public void CreateInitialBaseline()
        {
            var provider = new Func<MethodHandle, EditAndContinueMethodDebugInformation>(_ => default(EditAndContinueMethodDebugInformation));
            var peModule = ModuleMetadata.CreateFromImage(TestResources.MetadataTests.Basic.Members);
            var peReader = peModule.Module.PEReaderOpt;

            var mdBytes = peReader.GetMetadata().GetContent();
            var mdBytesHandle = PinnedImmutableArray.Create(mdBytes);
            var mdModule = ModuleMetadata.CreateFromMetadata(mdBytesHandle.Pointer, mdBytes.Length);

            Assert.Throws<ArgumentNullException>(() => EmitBaseline.CreateInitialBaseline(null, provider));
            Assert.Throws<ArgumentNullException>(() => EmitBaseline.CreateInitialBaseline(peModule, null));
            Assert.Throws<ArgumentException>(() => EmitBaseline.CreateInitialBaseline(mdModule, provider));
        }
    }
}
