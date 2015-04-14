// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Symbols.Metadata.PE;
using Microsoft.CodeAnalysis.ExpressionEvaluator;
using Microsoft.DiaSymReader;
using Roslyn.Utilities;
using Portable = Microsoft.DiaSymReader.PortablePdb;

namespace Microsoft.CodeAnalysis.CSharp.ExpressionEvaluator
{
    internal static class SymUnmanagedReaderExtensions
    {
        public unsafe static MethodDebugInfo GetMethodDebugInfo(
            this ISymUnmanagedReader reader,
            PEModuleSymbol module,
            int methodToken,
            int methodVersion,
            string firstLocalName)
        {
            var portableImage = reader.GetPortablePdbImage();
            if (portableImage != null)
            {
                fixed (byte* portableImagePtr = portableImage)
                {
                    // TODO: cache
                    var portableReader = new MetadataReader(portableImagePtr, portableImage.Length);
                    return GetMethodDebugInfoPortable(portableReader, module, methodToken, methodVersion, firstLocalName);
                }
            }
            else
            {
                return GetMethodDebugInfoNative(reader, methodToken, methodVersion, firstLocalName);
            }
        }

        private static MethodDebugInfo GetMethodDebugInfoNative(
            ISymUnmanagedReader reader,
            int methodToken,
            int methodVersion,
            string firstLocalName)
        {
            ImmutableArray<string> externAliasStrings;
            var importStringGroups = reader.GetCSharpGroupedImportStrings(methodToken, methodVersion, out externAliasStrings);
            Debug.Assert(importStringGroups.IsDefault == externAliasStrings.IsDefault);

            if (importStringGroups.IsDefault)
            {
                return default(MethodDebugInfo);
            }

            var importRecordGroupBuilder = ArrayBuilder<ImmutableArray<ImportRecord>>.GetInstance(importStringGroups.Length);
            foreach (var importStringGroup in importStringGroups)
            {
                var groupBuilder = ArrayBuilder<ImportRecord>.GetInstance(importStringGroup.Length);
                foreach (var importString in importStringGroup)
                {
                    ImportRecord record;
                    if (NativeImportRecord.TryCreateFromCSharpImportString(importString, out record))
                    {
                        groupBuilder.Add(record);
                    }
                    else
                    {
                        Debug.WriteLine($"Failed to parse import string {importString}");
                    }
                }
                importRecordGroupBuilder.Add(groupBuilder.ToImmutableAndFree());
            }

            var externAliasRecordBuilder = ArrayBuilder<ExternAliasRecord>.GetInstance(externAliasStrings.Length);
            foreach (string externAliasString in externAliasStrings)
            {
                string alias;
                string externAlias;
                string target;
                ImportTargetKind kind;
                if (!CustomDebugInfoReader.TryParseCSharpImportString(externAliasString, out alias, out externAlias, out target, out kind))
                {
                    Debug.WriteLine($"Unable to parse extern alias '{externAliasString}'");
                    continue;
                }

                Debug.Assert(kind == ImportTargetKind.Assembly, "Programmer error: How did a non-assembly get in the extern alias list?");
                Debug.Assert(alias != null); // Name of the extern alias.
                Debug.Assert(externAlias == null); // Not used.
                Debug.Assert(target != null); // Name of the target assembly.

                AssemblyIdentity targetIdentity;
                if (!AssemblyIdentity.TryParseDisplayName(target, out targetIdentity))
                {
                    Debug.WriteLine($"Unable to parse target of extern alias '{externAliasString}'");
                    continue;
                }

                externAliasRecordBuilder.Add(new NativeExternAliasRecord<AssemblySymbol>(alias, targetIdentity));
            }

            var hoistedLocalScopeRecords = ImmutableArray<HoistedLocalScopeRecord>.Empty;
            var dynamicLocalMap = ImmutableDictionary<int, ImmutableArray<bool>>.Empty;
            var dynamicLocalConstantMap = ImmutableDictionary<string, ImmutableArray<bool>>.Empty;

            byte[] customDebugInfoBytes = reader.GetCustomDebugInfoBytes(methodToken, methodVersion);

            if (customDebugInfoBytes != null)
            {
                var customDebugInfoRecord = CustomDebugInfoReader.TryGetCustomDebugInfoRecord(customDebugInfoBytes, CustomDebugInfoKind.StateMachineHoistedLocalScopes);
                if (!customDebugInfoRecord.IsDefault)
                {
                    hoistedLocalScopeRecords = CustomDebugInfoReader.DecodeStateMachineHoistedLocalScopesRecord(customDebugInfoRecord)
                        .SelectAsArray(s => HoistedLocalScopeRecord.FromNative(s.StartOffset, s.EndOffset));
                }

                CustomDebugInfoReader.GetCSharpDynamicLocalInfo(
                    customDebugInfoBytes,
                    methodToken,
                    methodVersion,
                    firstLocalName,
                    out dynamicLocalMap,
                    out dynamicLocalConstantMap);
            }

            return new MethodDebugInfo(
                hoistedLocalScopeRecords,
                importRecordGroupBuilder.ToImmutableAndFree(),
                externAliasRecordBuilder.ToImmutableAndFree(),
                dynamicLocalMap,
                dynamicLocalConstantMap,
                defaultNamespaceName: ""); // Unused in C#.
        }

        private static MethodDebugInfo GetMethodDebugInfoPortable(
            MetadataReader reader,
            PEModuleSymbol module,
            int methodToken,
            int methodVersion,
            string firstLocalName)
        {
            var methodHandle = (MethodDefinitionHandle)MetadataTokens.Handle(methodToken);

            var outermostScopeHandle = GetOutermostScope(reader, methodHandle);
            if (outermostScopeHandle.IsNil)
            {
                // method has no user code, hence no imports
                return default(MethodDebugInfo);
            }

            var importScopeHandle = reader.GetLocalScope(outermostScopeHandle).ImportScope;

            ImmutableArray<ImmutableArray<ImportRecord>> importChain;
            ImmutableArray<ExternAliasRecord> externAliases;
            GetImportChain(reader, importScopeHandle, module, out importChain, out externAliases);

            var hoistedLocalScopes = GetStateMachineHoistedLocalScopes(reader, methodHandle);

            ImmutableDictionary<int, ImmutableArray<bool>> dynamicLocalVariableMap;
            ImmutableDictionary<string, ImmutableArray<bool>> dynamicLocalConstantMap;
            GetDynamicLocalMaps(reader, methodHandle, out dynamicLocalVariableMap, out dynamicLocalConstantMap);

            return new MethodDebugInfo(
                hoistedLocalScopes,
                importChain,
                externAliases,
                dynamicLocalVariableMap,
                dynamicLocalConstantMap,
                defaultNamespaceName: ""); // Unused in C#.
        }

        private static string GetUtf8String(MetadataReader metadataReader, BlobHandle blobHandle)
        {
            var bytes = metadataReader.GetBlobBytes(blobHandle);
            return Encoding.UTF8.GetString(bytes, 0, bytes.Length);
        }

        private static LocalScopeHandle GetOutermostScope(MetadataReader reader, MethodDefinitionHandle method)
        {
            foreach (var scope in reader.GetLocalScopes(method))
            {
                return scope;
            }

            return default(LocalScopeHandle);
        }

        private static void GetImportChain(
            MetadataReader reader, 
            ImportScopeHandle importScopeHandle,
            PEModuleSymbol module,
            out ImmutableArray<ImmutableArray<ImportRecord>> importChain,
            out ImmutableArray<ExternAliasRecord> externAliases)
        {
            var result = ArrayBuilder<ImmutableArray<ImportRecord>>.GetInstance();
            var importsBuilder = ArrayBuilder<ImportRecord>.GetInstance();
            var aliasesBuilder = ArrayBuilder<ExternAliasRecord>.GetInstance();

            while (!importScopeHandle.IsNil)
            {
                var importScope = reader.GetImportScope(importScopeHandle);

                var importsReader = reader.GetImportsReader(importScope.Imports);
                while (importsReader.MoveNext())
                {
                    var importDefinition = importsReader.Current;
                    var alias = (importDefinition.Alias.IsNil) ? null : GetUtf8String(reader, importDefinition.Alias);

                    if (importDefinition.Kind == ImportDefinitionKind.AliasAssemblyReference)
                    {
                        aliasesBuilder.Add(new PortableExternAliasRecord(alias, module, module.Module, importDefinition.TargetAssembly));
                    }
                    else
                    {
                        var targetHandle = importDefinition.TargetType;
                        Handle targetTypeHandle;
                        string targetNamespaceName;
                        if (targetHandle.Kind == HandleKind.Blob)
                        {
                            targetTypeHandle = default(Handle);
                            targetNamespaceName = GetUtf8String(reader, importDefinition.TargetNamespace);
                        }
                        else
                        {
                            targetTypeHandle = targetHandle;
                            targetNamespaceName = null;
                        }

                        importsBuilder.Add(new PortableImportRecord(ToImportTargetKind(importDefinition.Kind), alias, importDefinition.TargetAssembly, targetTypeHandle, targetNamespaceName));
                    }
                }

                if (importsBuilder.Count > 0)
                {
                    result.Add(importsBuilder.ToImmutable());
                    importsBuilder.Clear();
                }

                importScopeHandle = importScope.Parent;
            }

            importsBuilder.Free();
            importChain = result.ToImmutableAndFree();
            externAliases = aliasesBuilder.ToImmutableAndFree();
        }

        private static ImportTargetKind ToImportTargetKind(ImportDefinitionKind kind)
        {
            switch (kind)
            {
                case ImportDefinitionKind.ImportNamespace:
                    return ImportTargetKind.Namespace;

                case ImportDefinitionKind.AliasNamespace:
                    return ImportTargetKind.Namespace;

                case ImportDefinitionKind.ImportAssemblyNamespace:
                    return ImportTargetKind.Namespace;

                case ImportDefinitionKind.AliasAssemblyNamespace:
                    return ImportTargetKind.Namespace;

                case ImportDefinitionKind.ImportType:
                    return ImportTargetKind.Type;

                case ImportDefinitionKind.AliasType:
                    return ImportTargetKind.Type;

                case ImportDefinitionKind.ImportXmlNamespace:
                    return ImportTargetKind.XmlNamespace;

                case ImportDefinitionKind.ImportAssemblyReferenceAlias:
                    return ImportTargetKind.Assembly;

                case ImportDefinitionKind.AliasAssemblyReference:
                    // Should have created an ExternAliasRecord for this.
                    throw ExceptionUtilities.UnexpectedValue(kind);

                default:
                    throw new BadImageFormatException();
            }
        }

        private static ImmutableArray<HoistedLocalScopeRecord> GetStateMachineHoistedLocalScopes(MetadataReader reader, MethodDefinitionHandle methodHandle)
        {
            foreach (var cdiHandle in reader.GetCustomDebugInformation(methodHandle))
            {
                var cdi = reader.GetCustomDebugInformation(cdiHandle);
                if (reader.GetGuid(cdi.Kind) == PortableCustomDebugInfoKinds.StateMachineHoistedLocalScopes)
                {
                    var cdiReader = reader.GetBlobReader(cdi.Value);

                    var builder = ArrayBuilder<HoistedLocalScopeRecord>.GetInstance();
                    while (cdiReader.RemainingBytes > 0)
                    {
                        // TODO: range checks

                        int start = cdiReader.ReadInt32();
                        int length = cdiReader.ReadInt32();

                        builder.Add(HoistedLocalScopeRecord.FromPortable(start, length));
                    }

                    return builder.ToImmutableAndFree();
                }
            }

            return ImmutableArray<HoistedLocalScopeRecord>.Empty;
        }

        private static void GetDynamicLocalMaps(
            MetadataReader reader,
            MethodDefinitionHandle methodHandle,
            out ImmutableDictionary<int, ImmutableArray<bool>> variables,
            out ImmutableDictionary<string, ImmutableArray<bool>>  constants)
        {
            // TODO: avoid reading eagerly, lookup variable/constant when needed

            var variableBuilder = ImmutableDictionary.CreateBuilder<int, ImmutableArray<bool>>();
            var constantBuilder = ImmutableDictionary.CreateBuilder<string, ImmutableArray<bool>>();

            foreach (var scopeHandle in reader.GetLocalScopes(methodHandle))
            {
                var scope = reader.GetLocalScope(scopeHandle);
                foreach (var localHandle in scope.GetLocalVariables())
                {
                    foreach (var cdiHandle in reader.GetCustomDebugInformation(localHandle))
                    {
                        var cdi = reader.GetCustomDebugInformation(cdiHandle);

                        // TODO: rename DynamicLocalVariables in spec
                        if (reader.GetGuid(cdi.Kind) == PortableCustomDebugInfoKinds.DynamicLocalVariables)
                        {
                            var local = reader.GetLocalVariable(localHandle);
                            variableBuilder.Add(local.Index, DecodeDynamicBitSequence(reader.GetBlobReader(cdi.Value)));
                            break;
                        }
                    }
                }

                foreach (var constantHandle in scope.GetLocalConstants())
                {
                    foreach (var cdiHandle in reader.GetCustomDebugInformation(constantHandle))
                    {
                        var cdi = reader.GetCustomDebugInformation(cdiHandle);

                        // TODO: rename DynamicLocalVariables in spec
                        if (reader.GetGuid(cdi.Kind) == PortableCustomDebugInfoKinds.DynamicLocalVariables)
                        {
                            var constant = reader.GetLocalConstant(constantHandle);
                            var name = reader.GetString(constant.Name);
                            constantBuilder.Add(name, DecodeDynamicBitSequence(reader.GetBlobReader(cdi.Value)));
                            break;
                        }
                    }
                }
            }

            variables = variableBuilder.ToImmutableDictionary();
            constants = constantBuilder.ToImmutableDictionary();
        }

        private static ImmutableArray<bool> DecodeDynamicBitSequence(BlobReader reader)
        {
            var builder = ArrayBuilder<bool>.GetInstance();

            while (reader.RemainingBytes > 0)
            {
                var b = reader.ReadByte();
                for (int i = 0; i < 8; i++)
                {
                    builder.Add((b & (1 << i)) != 0);
                }
            }

            return builder.ToImmutableArray();
        }
    }
}