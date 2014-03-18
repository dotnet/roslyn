// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Xml.Linq;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.MetadataUtilities;
using Roslyn.Test.PdbUtilities;
using Roslyn.Test.Utilities;

namespace Microsoft.CodeAnalysis.Test.Utilities
{
    internal sealed class CompilationDifference
    {
        public readonly ImmutableArray<byte> MetadataBlob;
        public readonly ImmutableArray<byte> ILBlob;
        public readonly EmitBaseline NextGeneration;
        public readonly Stream Pdb;
        public readonly CompilationTestData TestData;
        public readonly EmitResult Result;

        public CompilationDifference(
            ImmutableArray<byte> metadata, 
            ImmutableArray<byte> il, 
            Stream pdbStream, 
            EmitBaseline nextGeneration,
            CompilationTestData testData,
            EmitResult result)
        {
            this.MetadataBlob = metadata;
            this.ILBlob = il;
            this.Pdb = pdbStream;
            this.NextGeneration = nextGeneration;
            this.TestData = testData;
            this.Result = result;
        }

        public PinnedMetadata GetMetadata()
        {
            return new PinnedMetadata(MetadataBlob);
        }

        public void VerifyIL(string expectedIL)
        {
            string actualIL = ILBlob.GetMethodIL();
            AssertEx.AssertEqualToleratingWhitespaceDifferences(expectedIL, actualIL);
        }

        public void VerifyIL(string qualifiedMethodName, string expectedIL)
        {
            var ilBuilder = TestData.GetMethodData(qualifiedMethodName).ILBuilder;
            string actualIL = ILBuilderVisualizer.ILBuilderToString(ilBuilder);
            AssertEx.AssertEqualToleratingWhitespaceDifferences(expectedIL, actualIL);
        }

        public void VerifyPdb(IEnumerable<uint> methodTokens, string expectedPdb)
        {
            string actualPdb = PdbToXmlConverter.DeltaPdbToXml(Pdb, methodTokens);
            TestBase.AssertXmlEqual(expectedPdb, actualPdb);
        }
    }
}
