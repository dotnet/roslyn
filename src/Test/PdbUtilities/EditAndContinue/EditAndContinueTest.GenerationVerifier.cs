// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using Roslyn.Test.Utilities;

namespace Microsoft.CodeAnalysis.EditAndContinue.UnitTests
{
    using static EditAndContinueTestUtilities;

    internal partial class EditAndContinueTest<TSelf> : IDisposable
    {
        internal sealed class GenerationVerifier
        {
            private readonly int _ordinal;
            private readonly MetadataReader _metadataReader;
            private readonly IEnumerable<MetadataReader> _readers;
            private readonly GenerationInfo _generationInfo;

            public GenerationVerifier(int ordinal, GenerationInfo generationInfo, IEnumerable<MetadataReader> readers)
            {
                _ordinal = ordinal;
                _metadataReader = generationInfo.MetadataReader;
                _readers = readers;
                _generationInfo = generationInfo;
            }

            private string GetAssertMessage(string message)
            {
                var ordinalDescription = _ordinal == 0 ? "initial baseline" : $"generation {_ordinal}";
                return $"Failure in {ordinalDescription}: {message}";
            }

            internal void VerifyTypeDefNames(params string[] expected)
            {
                var actual = _readers.GetStrings(_metadataReader.GetTypeDefNames());
                AssertEx.Equal(expected, actual, message: GetAssertMessage("TypeDefs don't match"));
            }

            internal void VerifyMethodDefNames(params string[] expected)
            {
                var actual = _readers.GetStrings(_metadataReader.GetMethodDefNames());
                AssertEx.Equal(expected, actual, message: GetAssertMessage("MethodDefs don't match"));
            }

            internal void VerifyMemberRefNames(params string[] expected)
            {
                var actual = _readers.GetStrings(_metadataReader.GetMemberRefNames());
                AssertEx.Equal(expected, actual, message: GetAssertMessage("MemberRefs don't match"));
            }

            internal void VerifyFieldDefNames(params string[] expected)
            {
                var actual = _readers.GetStrings(_metadataReader.GetFieldDefNames());
                AssertEx.Equal(expected, actual, message: GetAssertMessage("FieldDefs don't match"));
            }

            internal void VerifyPropertyDefNames(params string[] expected)
            {
                var actual = _readers.GetStrings(_metadataReader.GetPropertyDefNames());
                AssertEx.Equal(expected, actual, message: GetAssertMessage("PropertyDefs don't match"));
            }

            internal void VerifyDeletedMembers(params string[] expected)
            {
                var actual = _generationInfo.Baseline.DeletedMembers.Select(e => e.Key.ToString() + ": {" + string.Join(", ", e.Value.Select(v => v.Name)) + "}");
                AssertEx.SetEqual(expected, actual, itemSeparator: ",\r\n", itemInspector: s => $"\"{s}\"");
            }

            internal void VerifyTableSize(TableIndex table, int expected)
            {
                AssertEx.AreEqual(expected, _metadataReader.GetTableRowCount(table), message: GetAssertMessage($"{table} table size doesnt't match"));
            }

            internal void VerifyEncLog(IEnumerable<EditAndContinueLogEntry>? expected = null)
            {
                AssertEx.Equal(
                    expected ?? Array.Empty<EditAndContinueLogEntry>(),
                    _metadataReader.GetEditAndContinueLogEntries(), itemInspector: EncLogRowToString, message: GetAssertMessage("EncLog doesn't match"));
            }

            internal void VerifyEncMap(IEnumerable<EntityHandle>? expected = null)
            {
                AssertEx.Equal(
                    expected ?? Array.Empty<EntityHandle>(),
                    _metadataReader.GetEditAndContinueMapEntries(), itemInspector: EncMapRowToString, message: GetAssertMessage("EncMap doesn't match"));
            }

            internal void VerifyEncLogDefinitions(IEnumerable<EditAndContinueLogEntry>? expected = null)
            {
                AssertEx.Equal(
                    expected ?? Array.Empty<EditAndContinueLogEntry>(),
                    _metadataReader.GetEditAndContinueLogEntries().Where(e => IsDefinition(e.Handle.Kind)), itemInspector: EncLogRowToString, message: GetAssertMessage("EncLog definitions don't match"));
            }

            internal void VerifyEncMapDefinitions(IEnumerable<EntityHandle>? expected = null)
            {
                AssertEx.Equal(
                    expected ?? Array.Empty<EntityHandle>(),
                    _metadataReader.GetEditAndContinueMapEntries().Where(e => IsDefinition(e.Kind)), itemInspector: EncMapRowToString, message: GetAssertMessage("EncMap definitions don't match"));
            }

            internal void VerifyCustomAttributes(IEnumerable<CustomAttributeRow>? expected = null)
            {
                AssertEx.Equal(
                    expected ?? Array.Empty<CustomAttributeRow>(),
                    _metadataReader.GetCustomAttributeRows(), itemInspector: AttributeRowToString);
            }

            public void VerifySynthesizedMembers(params string[] expected)
            {
                var actual = _generationInfo.Baseline.SynthesizedMembers
                    .Select(e => e.Key.ToString() + ": {" + string.Join(", ", e.Value.Select(v => v.Name)) + "}");

                AssertEx.SetEqual(expected, actual, itemSeparator: ",\r\n", itemInspector: s => $"\"{s}\"");
            }

            public void VerifySynthesizedFields(string typeName, params string[] expectedSynthesizedTypesAndMemberCounts)
            {
                var actual = _generationInfo.Baseline.SynthesizedMembers
                    .Single(e => e.Key.ToString() == typeName).Value.Where(s => s.Kind == SymbolKind.Field)
                    .Select(s => (IFieldSymbol)s.GetISymbol()).Select(f => f.Name + ": " + f.Type);

                AssertEx.SetEqual(expectedSynthesizedTypesAndMemberCounts, actual, itemSeparator: "\r\n");
            }

            internal void VerifyMethodBody(string qualifiedMemberName, string expectedILWithSequencePoints)
                => _generationInfo.CompilationVerifier!.VerifyMethodBody(qualifiedMemberName, expectedILWithSequencePoints);

            internal void VerifyIL(string expectedIL)
                => _generationInfo.CompilationDifference!.VerifyIL(expectedIL);

            internal void VerifyIL(string qualifiedMemberName, string expectedIL)
                => _generationInfo.CompilationDifference!.VerifyIL(qualifiedMemberName, expectedIL);
        }
    }
}
