// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using Microsoft.CodeAnalysis.Symbols;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;

namespace Microsoft.CodeAnalysis.EditAndContinue.UnitTests
{
    using static EditAndContinueTestUtilities;

    internal partial class EditAndContinueTest<TSelf> : IDisposable
    {
        internal sealed class GenerationVerifier(int ordinal, GenerationInfo generationInfo, ImmutableArray<MetadataReader> readers)
        {
            public readonly List<Exception> Exceptions = [];

            private MetadataReader MetadataReader
                => generationInfo.MetadataReader;

            private string GetAssertMessage(string message)
            {
                var ordinalDescription = ordinal == 0 ? "initial baseline" : $"generation {ordinal}";
                return $"Failure in {ordinalDescription}: {message}";
            }

            private void Verify(Action action)
            {
                try
                {
                    action();
                }
                catch (Exception e)
                {
                    Exceptions.Add(e);
                }
            }

            internal void VerifyTypeDefNames(params string[] expected)
                => Verify(() => AssertEntityNamesEqual("TypeDefs", expected, MetadataReader.GetTypeDefNames()));

            internal void VerifyMethodDefNames(params string[] expected)
                => Verify(() => AssertEntityNamesEqual("MethodDefs", expected, MetadataReader.GetMethodDefNames()));

            internal void VerifyTypeRefNames(params string[] expected)
                => Verify(() => AssertEntityNamesEqual("TypeRefs", expected, MetadataReader.GetTypeRefNames()));

            internal void VerifyMemberRefNames(params string[] expected)
                => Verify(() => AssertEntityNamesEqual("MemberRefs", expected, MetadataReader.GetMemberRefNames()));

            internal void VerifyFieldDefNames(params string[] expected)
                => Verify(() => AssertEntityNamesEqual("FieldDefs", expected, MetadataReader.GetFieldDefNames()));

            internal void VerifyPropertyDefNames(params string[] expected)
                => Verify(() => AssertEntityNamesEqual("PropertyDefs", expected, MetadataReader.GetPropertyDefNames()));

            private void AssertEntityNamesEqual(string entityKinds, string[] expected, StringHandle[] actual)
                => AssertEx.Equal(expected, readers.GetStrings(actual), message: GetAssertMessage($"{entityKinds} don't match"), itemSeparator: ", ", itemInspector: s => $"\"{s}\"");

            internal void VerifyDeletedMembers(params string[] expected)
                => Verify(() =>
                {
                    var actual = generationInfo.Baseline.DeletedMembers.Select(e => e.Key.ToString() + ": {" + string.Join(", ", e.Value.Select(v => v.Name)) + "}");
                    AssertEx.SetEqual(expected, actual, itemSeparator: ",\r\n", itemInspector: s => $"\"{s}\"");
                });

            internal void VerifyTableSize(TableIndex table, int expected)
                => Verify(() =>
                {
                    AssertEx.AreEqual(expected, MetadataReader.GetTableRowCount(table), message: GetAssertMessage($"{table} table size doesnt't match"));
                });

            internal void VerifyEncLog(IEnumerable<EditAndContinueLogEntry>? expected = null)
                => Verify(() =>
                {
                    AssertEx.Equal(
                        expected ?? Array.Empty<EditAndContinueLogEntry>(),
                        MetadataReader.GetEditAndContinueLogEntries(), itemInspector: EncLogRowToString, message: GetAssertMessage("EncLog doesn't match"));
                });

            internal void VerifyEncMap(IEnumerable<EntityHandle>? expected = null)
                => Verify(() =>
                {
                    AssertEx.Equal(
                        expected ?? Array.Empty<EntityHandle>(),
                        MetadataReader.GetEditAndContinueMapEntries(), itemInspector: EncMapRowToString, message: GetAssertMessage("EncMap doesn't match"));
                });

            internal void VerifyEncLogDefinitions(IEnumerable<EditAndContinueLogEntry>? expected = null)
                => Verify(() =>
                {
                    AssertEx.Equal(
                        expected ?? Array.Empty<EditAndContinueLogEntry>(),
                        MetadataReader.GetEditAndContinueLogEntries().Where(e => IsDefinition(e.Handle.Kind)), itemInspector: EncLogRowToString, message: GetAssertMessage("EncLog definitions don't match"));
                });

            internal void VerifyEncMapDefinitions(IEnumerable<EntityHandle>? expected = null)
                => Verify(() =>
                {
                    AssertEx.Equal(
                        expected ?? Array.Empty<EntityHandle>(),
                        MetadataReader.GetEditAndContinueMapEntries().Where(e => IsDefinition(e.Kind)), itemInspector: EncMapRowToString, message: GetAssertMessage("EncMap definitions don't match"));
                });

            internal void VerifyCustomAttributes(IEnumerable<CustomAttributeRow>? expected = null)
                => Verify(() =>
                {
                    AssertEx.Equal(
                        expected ?? Array.Empty<CustomAttributeRow>(),
                        MetadataReader.GetCustomAttributeRows(), itemInspector: AttributeRowToString);
                });

            private IReadOnlyDictionary<ISymbolInternal, ImmutableArray<ISymbolInternal>> GetSynthesizedMembers()
                => (generationInfo.CompilationVerifier != null)
                        ? generationInfo.CompilationVerifier.TestData.Module!.GetAllSynthesizedMembers()
                        : generationInfo.Baseline.SynthesizedMembers;

            public void VerifySynthesizedMembers(params string[] expected)
                => VerifySynthesizedMembers(displayTypeKind: false, expected);

            public void VerifySynthesizedMembers(bool displayTypeKind, params string[] expected)
                => Verify(() =>
                {
                    var actual = GetSynthesizedMembers().Select(e =>
                        $"{(displayTypeKind && e.Key is INamedTypeSymbolInternal type ? (type.TypeKind == TypeKind.Struct ? "struct " : "class ") : "")}{e.Key}: " +
                        $"{{{string.Join(", ", e.Value.Select(v => v.Name))}}}");

                    AssertEx.SetEqual(expected, actual, itemSeparator: ",\r\n", itemInspector: s => $"\"{s}\"");
                });

            public void VerifySynthesizedFields(string typeName, params string[] expectedSynthesizedTypesAndMemberCounts)
                => Verify(() =>
                {
                    var actual = GetSynthesizedMembers()
                        .Single(e => e.Key.ToString() == typeName).Value.Where(s => s.Kind == SymbolKind.Field)
                        .Select(s => (IFieldSymbol)s.GetISymbol()).Select(f => f.Name + ": " + f.Type);

                    AssertEx.SetEqual(expectedSynthesizedTypesAndMemberCounts, actual, itemSeparator: "\r\n");
                });

            public void VerifyUpdatedMethodNames(params string[] expectedMethodNames)
                => Verify(() =>
                {
                    Debug.Assert(generationInfo.CompilationDifference != null);
                    CheckNames(readers, generationInfo.CompilationDifference.EmitResult.UpdatedMethods, expectedMethodNames);
                });

            public void VerifyChangedTypeNames(params string[] expectedTypeNames)
                => Verify(() =>
                {
                    Debug.Assert(generationInfo.CompilationDifference != null);
                    CheckNames(readers, generationInfo.CompilationDifference.EmitResult.ChangedTypes, expectedTypeNames);
                });

            internal void VerifyMethodBody(string qualifiedMemberName, string expectedILWithSequencePoints)
                => Verify(() => generationInfo.CompilationVerifier!.VerifyMethodBody(qualifiedMemberName, expectedILWithSequencePoints));

            internal void VerifyPdb(IEnumerable<int> methodTokens, string expectedPdb)
                => Verify(() => generationInfo.CompilationDifference!.VerifyPdb(methodTokens, expectedPdb, expectedIsRawXml: true));

            internal void VerifyPdb(string qualifiedMemberName, string expectedPdb, PdbValidationOptions options = default)
                => Verify(() => generationInfo.CompilationVerifier!.VerifyPdb(qualifiedMemberName, expectedPdb, options: options, expectedIsRawXml: true));

            internal void VerifyCustomDebugInformation(string qualifiedMemberName, string expectedPdb)
                => VerifyPdb(qualifiedMemberName, expectedPdb, PdbValidationOptions.ExcludeDocuments | PdbValidationOptions.ExcludeSequencePoints | PdbValidationOptions.ExcludeScopes);

            internal void VerifyIL(string expectedIL)
                => Verify(() =>
                {
                    Debug.Assert(generationInfo.CompilationDifference != null);
                    generationInfo.CompilationDifference.VerifyIL(expectedIL);
                });

            internal void VerifyIL(string qualifiedMemberName, string expectedIL)
                => Verify(() =>
                {
                    if (generationInfo.CompilationVerifier != null)
                    {
                        generationInfo.CompilationVerifier.VerifyIL(qualifiedMemberName, expectedIL);
                    }
                    else
                    {
                        Debug.Assert(generationInfo.CompilationDifference != null);
                        generationInfo.CompilationDifference.VerifyIL(qualifiedMemberName, expectedIL);
                    }
                });
        }
    }
}
