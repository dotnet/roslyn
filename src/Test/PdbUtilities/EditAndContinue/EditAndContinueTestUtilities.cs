// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using Microsoft.CodeAnalysis.Emit;
using Roslyn.Test.Utilities;

namespace Microsoft.CodeAnalysis.EditAndContinue.UnitTests;

internal class EditAndContinueTestUtilities
{
    public static EmitBaseline CreateInitialBaseline(Compilation compilation, ModuleMetadata module, Func<MethodDefinitionHandle, EditAndContinueMethodDebugInformation> debugInformationProvider)
    {
        var localSignatureProvider = new Func<MethodDefinitionHandle, StandaloneSignatureHandle>(methodHandle =>
        {
            try
            {
                return module.Module.GetMethodBodyOrThrow(methodHandle)?.LocalSignature ?? default;
            }
            catch (Exception e) when (e is BadImageFormatException || e is IOException)
            {
                throw new InvalidDataException(e.Message, e);
            }
        });

        var hasPortableDebugInformation = module.Module.PEReaderOpt.ReadDebugDirectory().Any(static entry => entry.IsPortableCodeView);

        return EmitBaseline.CreateInitialBaseline(compilation, module, debugInformationProvider, localSignatureProvider, hasPortableDebugInformation);
    }

    public static string EncLogRowToString(EditAndContinueLogEntry row)
    {
        TableIndex tableIndex;
        MetadataTokens.TryGetTableIndex(row.Handle.Kind, out tableIndex);

        return string.Format(
            "Row({0}, TableIndex.{1}, EditAndContinueOperation.{2})",
            MetadataTokens.GetRowNumber(row.Handle),
            tableIndex,
            row.Operation);
    }

    public static string EncMapRowToString(EntityHandle handle)
    {
        TableIndex tableIndex;
        MetadataTokens.TryGetTableIndex(handle.Kind, out tableIndex);

        return string.Format(
            "Handle({0}, TableIndex.{1})",
            MetadataTokens.GetRowNumber(handle),
            tableIndex);
    }

    public static string AttributeRowToString(CustomAttributeRow row)
    {
        TableIndex parentTableIndex, constructorTableIndex;
        MetadataTokens.TryGetTableIndex(row.ParentToken.Kind, out parentTableIndex);
        MetadataTokens.TryGetTableIndex(row.ConstructorToken.Kind, out constructorTableIndex);

        return string.Format(
            "new CustomAttributeRow(Handle({0}, TableIndex.{1}), Handle({2}, TableIndex.{3}))",
            MetadataTokens.GetRowNumber(row.ParentToken),
            parentTableIndex,
            MetadataTokens.GetRowNumber(row.ConstructorToken),
            constructorTableIndex);
    }

    public static bool IsDefinition(HandleKind kind)
        => kind is not (HandleKind.AssemblyReference or HandleKind.ModuleReference or HandleKind.TypeReference or HandleKind.MemberReference or HandleKind.TypeSpecification or HandleKind.MethodSpecification);

    public static void CheckNames(MetadataReader reader, IEnumerable<StringHandle> handles, params string[] expectedNames)
    {
        CheckNames(new[] { reader }, handles, expectedNames);
    }

    public static void CheckNames(IEnumerable<MetadataReader> readers, IEnumerable<StringHandle> handles, params string[] expectedNames)
    {
        var actualNames = readers.GetStrings(handles);
        AssertEx.Equal(expectedNames, actualNames);
    }

    public static void CheckNames(IReadOnlyList<MetadataReader> readers, IEnumerable<(StringHandle Namespace, StringHandle Name)> handles, params string[] expectedNames)
    {
        var actualNames = handles.Select(handlePair => string.Join(".", readers.GetString(handlePair.Namespace), readers.GetString(handlePair.Name))).ToArray();
        AssertEx.Equal(expectedNames, actualNames);
    }

    public static void CheckNames(IReadOnlyList<MetadataReader> readers, ImmutableArray<TypeDefinitionHandle> typeHandles, params string[] expectedNames)
        => CheckNames(readers, typeHandles, (reader, handle) => reader.GetTypeDefinition((TypeDefinitionHandle)handle).Name, handle => handle, expectedNames);

    public static void CheckNames(IReadOnlyList<MetadataReader> readers, ImmutableArray<MethodDefinitionHandle> methodHandles, params string[] expectedNames)
        => CheckNames(readers, methodHandles, (reader, handle) => reader.GetMethodDefinition((MethodDefinitionHandle)handle).Name, handle => handle, expectedNames);

    private static void CheckNames<THandle>(
        IReadOnlyList<MetadataReader> readers,
        ImmutableArray<THandle> entityHandles,
        Func<MetadataReader, Handle, StringHandle> getName,
        Func<THandle, Handle> toHandle,
        string[] expectedNames)
    {
        var aggregator = GetAggregator(readers);

        AssertEx.Equal(expectedNames, entityHandles.Select(handle =>
        {
            var genEntityHandle = aggregator.GetGenerationHandle(toHandle(handle), out int typeGeneration);
            var nameHandle = getName(readers[typeGeneration], genEntityHandle);

            var genNameHandle = (StringHandle)aggregator.GetGenerationHandle(nameHandle, out int nameGeneration);
            return readers[nameGeneration].GetString(genNameHandle);
        }));
    }

    public static MetadataAggregator GetAggregator(IReadOnlyList<MetadataReader> readers)
        => new MetadataAggregator(readers[0], readers.Skip(1).ToArray());
}
