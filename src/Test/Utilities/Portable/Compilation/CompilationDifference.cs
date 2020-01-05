// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.DiaSymReader.Tools;
using Microsoft.Metadata.Tools;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Test.Utilities
{
    public sealed class CompilationDifference
    {
        public readonly ImmutableArray<byte> MetadataDelta;
        public readonly ImmutableArray<byte> ILDelta;
        public readonly ImmutableArray<byte> PdbDelta;
        internal readonly CompilationTestData TestData;
        public readonly EmitDifferenceResult EmitResult;
        public readonly ImmutableArray<MethodDefinitionHandle> UpdatedMethods;

        internal CompilationDifference(
            ImmutableArray<byte> metadata,
            ImmutableArray<byte> il,
            ImmutableArray<byte> pdb,
            CompilationTestData testData,
            EmitDifferenceResult result,
            ImmutableArray<MethodDefinitionHandle> methodHandles)
        {
            MetadataDelta = metadata;
            ILDelta = il;
            PdbDelta = pdb;
            TestData = testData;
            EmitResult = result;
            UpdatedMethods = methodHandles;
        }

        public EmitBaseline NextGeneration
        {
            get
            {
                return EmitResult.Baseline;
            }
        }

        internal PinnedMetadata GetMetadata()
        {
            return new PinnedMetadata(MetadataDelta);
        }

        public void VerifyIL(
            string expectedIL,
            [CallerLineNumber]int callerLine = 0,
            [CallerFilePath]string callerPath = null)
        {
            string actualIL = ILDelta.GetMethodIL();
            AssertEx.AssertEqualToleratingWhitespaceDifferences(expectedIL, actualIL, escapeQuotes: true, expectedValueSourcePath: callerPath, expectedValueSourceLine: callerLine);
        }

        public void VerifyLocalSignature(
            string qualifiedMethodName,
            string expectedSignature,
            [CallerLineNumber]int callerLine = 0,
            [CallerFilePath]string callerPath = null)
        {
            var ilBuilder = TestData.GetMethodData(qualifiedMethodName).ILBuilder;
            string actualSignature = ILBuilderVisualizer.LocalSignatureToString(ilBuilder, ToLocalInfo);
            AssertEx.AssertEqualToleratingWhitespaceDifferences(expectedSignature, actualSignature, escapeQuotes: true, expectedValueSourcePath: callerPath, expectedValueSourceLine: callerLine);
        }

        internal void VerifyIL(
            string qualifiedMethodName,
            string expectedIL,
            Func<Cci.ILocalDefinition, ILVisualizer.LocalInfo> mapLocal = null,
            MethodDefinitionHandle methodToken = default,
            [CallerFilePath]string callerPath = null,
            [CallerLineNumber]int callerLine = 0)
        {
            var ilBuilder = TestData.GetMethodData(qualifiedMethodName).ILBuilder;

            Dictionary<int, string> sequencePointMarkers = null;
            if (!methodToken.IsNil)
            {
                string actualPdb = PdbToXmlConverter.DeltaPdbToXml(new ImmutableMemoryStream(PdbDelta), new[] { MetadataTokens.GetToken(methodToken) });
                sequencePointMarkers = ILValidation.GetSequencePointMarkers(actualPdb);

                Assert.True(sequencePointMarkers.Count > 0, $"No sequence points found in:{Environment.NewLine}{actualPdb}");
            }

            string actualIL = ILBuilderVisualizer.ILBuilderToString(ilBuilder, mapLocal ?? ToLocalInfo, sequencePointMarkers);
            AssertEx.AssertEqualToleratingWhitespaceDifferences(expectedIL, actualIL, escapeQuotes: true, expectedValueSourcePath: callerPath, expectedValueSourceLine: callerLine);
        }

        internal string GetMethodIL(string qualifiedMethodName)
        {
            return ILBuilderVisualizer.ILBuilderToString(this.TestData.GetMethodData(qualifiedMethodName).ILBuilder, ToLocalInfo);
        }

        private static ILVisualizer.LocalInfo ToLocalInfo(Cci.ILocalDefinition local)
        {
            var signature = local.Signature;
            if (signature == null)
            {
                return new ILVisualizer.LocalInfo(local.Name, local.Type, local.IsPinned, local.IsReference);
            }
            else
            {
                // Decode simple types only.
                var typeName = (signature.Length == 1) ? GetTypeName((SignatureTypeCode)signature[0]) : null;
                return new ILVisualizer.LocalInfo(null, typeName ?? "[unchanged]", false, false);
            }
        }

        private static string GetTypeName(SignatureTypeCode typeCode)
        {
            switch (typeCode)
            {
                case SignatureTypeCode.Boolean: return "[bool]";
                case SignatureTypeCode.Int32: return "[int]";
                case SignatureTypeCode.String: return "[string]";
                case SignatureTypeCode.Object: return "[object]";
                default: return null;
            }
        }

        public void VerifySynthesizedMembers(params string[] expectedSynthesizedTypesAndMemberCounts)
        {
            var actual = EmitResult.Baseline.SynthesizedMembers.Select(e => e.Key.ToString() + ": {" + string.Join(", ", e.Value.Select(v => v.Name)) + "}");
            AssertEx.SetEqual(expectedSynthesizedTypesAndMemberCounts, actual, itemSeparator: "\r\n");
        }

        public void VerifySynthesizedFields(string typeName, params string[] expectedSynthesizedTypesAndMemberCounts)
        {
            var actual = EmitResult.Baseline.SynthesizedMembers.Single(e => e.Key.ToString() == typeName).Value.Where(s => s.Kind == SymbolKind.Field).Select(s => (IFieldSymbol)s.GetISymbol()).Select(f => f.Name + ": " + f.Type);
            AssertEx.SetEqual(expectedSynthesizedTypesAndMemberCounts, actual, itemSeparator: "\r\n");
        }

        public void VerifyUpdatedMethods(params string[] expectedMethodTokens)
        {
            AssertEx.Equal(
                expectedMethodTokens,
                UpdatedMethods.Select(methodHandle => $"0x{MetadataTokens.GetToken(methodHandle):X8}"));
        }
    }
}
