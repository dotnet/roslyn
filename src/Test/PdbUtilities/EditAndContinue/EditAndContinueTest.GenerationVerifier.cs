// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using Microsoft.CodeAnalysis.Symbols;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.Metadata.Tools;
using Roslyn.Test.Utilities;

namespace Microsoft.CodeAnalysis.EditAndContinue.UnitTests
{
    using static EditAndContinueTestUtilities;

    internal partial class EditAndContinueTest<TSelf> : IDisposable
    {
        internal sealed class GenerationVerifier(int ordinal, GenerationInfo generationInfo, ImmutableArray<MetadataReader> readers)
        {
            private readonly Lazy<MetadataVisualizer> _lazyVisualizer = new(() => new MetadataVisualizer(readers, TextWriter.Null, MetadataVisualizerOptions.None));
            public readonly List<Exception> Exceptions = [];

            private MetadataReader MetadataReader
                => generationInfo.MetadataReader;

            private MetadataVisualizer Visualizer
                => _lazyVisualizer.Value;

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

            internal void VerifyEventDefNames(params string[] expected)
                => Verify(() => AssertEntityNamesEqual("EventDefs", expected, MetadataReader.GetEventDefNames()));

            private void AssertEntityNamesEqual(string entityKinds, string[] expected, StringHandle[] actual)
                => AssertEx.Equal(expected, readers.GetStrings(actual), message: GetAssertMessage($"{entityKinds} don't match"), itemSeparator: ", ", itemInspector: s => $"\"{s}\"");

            internal void VerifyMethodDefs(params (string name, MethodAttributes attributes)[] expected)
                => Verify(() =>
                {
                    var actual = MetadataReader.MethodDefinitions.Select(handle =>
                    {
                        var def = MetadataReader.GetMethodDefinition(handle);
                        return (name: readers.GetString(def.Name), attributes: def.Attributes);
                    }).ToArray();

                    AssertEx.Equal(
                        expected,
                        actual,
                        message: GetAssertMessage($"MethodDefs don't match"),
                        itemSeparator: "," + Environment.NewLine,
                        itemInspector: s => $"(\"{s.name}\", {Inspect(s.attributes)})");
                });

            internal void VerifyPropertyDefs(params (string name, PropertyAttributes attributes)[] expected)
                => Verify(() =>
                {
                    var actual = MetadataReader.PropertyDefinitions.Select(handle =>
                    {
                        var def = MetadataReader.GetPropertyDefinition(handle);
                        return (name: readers.GetString(def.Name), attributes: def.Attributes);
                    }).ToArray();

                    AssertEx.Equal(
                        expected,
                        actual,
                        message: GetAssertMessage($"PropertyDefs don't match"),
                        itemSeparator: "," + Environment.NewLine,
                        itemInspector: s => $"(\"{s.name}\", {Inspect(s.attributes)})");
                });

            internal void VerifyEventDefs(params (string name, EventAttributes attributes)[] expected)
                => Verify(() =>
                {
                    var actual = MetadataReader.EventDefinitions.Select(handle =>
                    {
                        var def = MetadataReader.GetEventDefinition(handle);
                        return (name: readers.GetString(def.Name), attributes: def.Attributes);
                    }).ToArray();

                    AssertEx.Equal(
                        expected,
                        actual,
                        message: GetAssertMessage($"EventDefs don't match"),
                        itemSeparator: "," + Environment.NewLine,
                        itemInspector: s => $"(\"{s.name}\", {Inspect(s.attributes)})");
                });

            internal static string Inspect<TEnum>(TEnum value) where TEnum : Enum
                => string.Join(" | ", value.ToString().Split(',').Select(s => $"{typeof(TEnum).Name}.{s.Trim()}"));

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
                        expected ?? [],
                        MetadataReader.GetEditAndContinueLogEntries(), itemInspector: EncLogRowToString, message: GetAssertMessage("EncLog doesn't match"));
                });

            internal void VerifyEncMap(IEnumerable<EntityHandle>? expected = null)
                => Verify(() =>
                {
                    AssertEx.Equal(
                        expected ?? [],
                        MetadataReader.GetEditAndContinueMapEntries(), itemInspector: EncMapRowToString, message: GetAssertMessage("EncMap doesn't match"));
                });

            internal void VerifyEncLogDefinitions(IEnumerable<EditAndContinueLogEntry>? expected = null)
                => Verify(() =>
                {
                    AssertEx.Equal(
                        expected ?? [],
                        MetadataReader.GetEditAndContinueLogEntries().Where(e => IsDefinition(e.Handle.Kind)), itemInspector: EncLogRowToString, message: GetAssertMessage("EncLog definitions don't match"));
                });

            internal void VerifyEncMapDefinitions(IEnumerable<EntityHandle>? expected = null)
                => Verify(() =>
                {
                    AssertEx.Equal(
                        expected ?? [],
                        MetadataReader.GetEditAndContinueMapEntries().Where(e => IsDefinition(e.Kind)), itemInspector: EncMapRowToString, message: GetAssertMessage("EncMap definitions don't match"));
                });

            internal void VerifyCustomAttributes(IEnumerable<CustomAttributeRow>? expected = null)
                => Verify(() =>
                {
                    AssertEx.Equal(
                        expected ?? [],
                        MetadataReader.GetCustomAttributeRows(),
                        itemInspector: AttributeRowToString);
                });

            internal void VerifyCustomAttributes(params string[] expected)
                => VerifyCustomAttributes(expected, includeNil: false);

            internal void VerifyCustomAttributes(string[] expected, bool includeNil)
                => Verify(() =>
                {
                    AssertEx.SequenceEqual(
                        expected ?? [],
                        MetadataReader.GetCustomAttributeRows()
                            // CustomAttribute table entries might be zeroed out in the delta.
                            .Where(row => includeNil || !row.ParentToken.IsNil || !row.ConstructorToken.IsNil)
                            .Select(row => row.ParentToken.IsNil && row.ConstructorToken.IsNil
                                ? "<nil>"
                                : $"[{Visualizer.GetQualifiedName(row.ConstructorToken)}] {InspectCustomAttributeTarget(row.ParentToken)}"));
                });

            private string InspectCustomAttributeTarget(EntityHandle handle)
                => handle.Kind switch
                {
                    HandleKind.AssemblyDefinition => "<assembly>",
                    HandleKind.ModuleDefinition => "<module>",
                    _ => Visualizer.GetQualifiedName(handle)
                };

            private IReadOnlyDictionary<ISymbolInternal, ImmutableArray<ISymbolInternal>> GetSynthesizedMembers()
                => generationInfo.CompilationVerifier?.TestData.Module!.GetAllSynthesizedMembers() ?? generationInfo.Baseline.SynthesizedMembers;

            public ImmutableArray<ISymbolInternal> GetSynthesizedTypes()
            {
                var map = generationInfo.CompilationVerifier?.TestData.Module!.GetAllSynthesizedTypes() ?? generationInfo.Baseline.SynthesizedTypes;

                return
                [
                    .. map.AnonymousTypes.Values.Select(t => t.Type.GetInternalSymbol()!),
                    .. map.AnonymousDelegates.Values.Select(t => t.Delegate.GetInternalSymbol()!),
                    .. map.AnonymousDelegatesWithIndexedNames.Values.SelectMany(t => t.Select(d => d.Type.GetInternalSymbol()!))
                ];
            }

            public void VerifySynthesizedMembers(params string[] expected)
                => VerifySynthesizedMembers(displayTypeKind: false, expected);

            public void VerifySynthesizedMembers(bool displayTypeKind, params string[] expected)
                => Verify(() => CompilationDifference.VerifySynthesizedMembers(GetSynthesizedMembers(), displayTypeKind, expected));

            public void VerifySynthesizedTypes(params string[] expected)
                => Verify(() => CompilationDifference.VerifySynthesizedSymbols(GetSynthesizedTypes(), expected));

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
                 => Verify(() =>
                 {
                     if (generationInfo.CompilationVerifier != null)
                     {
                         generationInfo.CompilationVerifier.VerifyMethodBody(qualifiedMemberName, expectedILWithSequencePoints);
                     }
                     else
                     {
                         Debug.Assert(generationInfo.CompilationDifference != null);
                         var updatedMethods = generationInfo.CompilationDifference.EmitResult.UpdatedMethods;

                         Debug.Assert(updatedMethods.Length == 1, "Only supported for a single method update");
                         var updatedMethodToken = updatedMethods.Single();

                         generationInfo.CompilationDifference.VerifyIL(qualifiedMemberName, expectedILWithSequencePoints, methodToken: updatedMethodToken);
                     }
                 });

            internal void VerifyPdb(IEnumerable<int> methodTokens, string expectedPdb)
                => Verify(() => generationInfo.CompilationDifference.VerifyPdb(methodTokens, expectedPdb, expectedIsRawXml: true));

            internal void VerifyPdb(string qualifiedMemberName, string expectedPdb, PdbValidationOptions options = default)
                => Verify(() => generationInfo.CompilationVerifier.VerifyPdb(qualifiedMemberName, expectedPdb, options: options, expectedIsRawXml: true));

            internal void VerifyCustomDebugInformation(string qualifiedMemberName, string expectedPdb)
                => VerifyPdb(qualifiedMemberName, expectedPdb, PdbValidationOptions.ExcludeDocuments | PdbValidationOptions.ExcludeSequencePoints | PdbValidationOptions.ExcludeScopes);

            internal void VerifyEncFieldRvaData(string expected)
                => Verify(() =>
                {
                    Debug.Assert(generationInfo.CompilationDifference != null);

                    var actual = ILValidation.DumpEncDeltaFieldData(generationInfo.CompilationDifference.ILDelta, readers);
                    AssertEx.AssertEqualToleratingWhitespaceDifferences(expected, actual, escapeQuotes: false);
                });

            internal void VerifyIL(string expected)
                => Verify(() =>
                {
                    Debug.Assert(generationInfo.CompilationDifference != null);

                    var actual = ILValidation.DumpEncDeltaMethodBodies(generationInfo.CompilationDifference.ILDelta, readers);
                    AssertEx.AssertEqualToleratingWhitespaceDifferences(expected, actual, escapeQuotes: false);
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

            public void VerifyLocalSignature(string qualifiedMethodName, string expectedSignature)
                => Verify(() =>
                {
                    var testData = generationInfo.CompilationVerifier?.TestData ?? generationInfo.CompilationDifference!.TestData;
                    var ilBuilder = testData.GetMethodData(qualifiedMethodName).ILBuilder;
                    var actualSignature = ILBuilderVisualizer.LocalSignatureToString(ilBuilder);
                    AssertEx.AssertEqualToleratingWhitespaceDifferences(expectedSignature, actualSignature, escapeQuotes: true);
                });
        }
    }
}
