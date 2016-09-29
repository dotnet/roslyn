﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using Microsoft.CodeAnalysis.Debugging;
using Microsoft.CodeAnalysis.Collections;

namespace Microsoft.CodeAnalysis.ExpressionEvaluator
{
    internal partial class MethodDebugInfo<TTypeSymbol, TLocalSymbol>
    {
        /// <exception cref="BadImageFormatException">Invalid data format.</exception>
        public static MethodDebugInfo<TTypeSymbol, TLocalSymbol> ReadFromPortable(MetadataReader reader, int methodToken, int ilOffset, EESymbolProvider<TTypeSymbol, TLocalSymbol> symbolProvider, bool isVisualBasicMethod)
        {
            string defaultNamespace;
            ImmutableArray<HoistedLocalScopeRecord> hoistedLocalScopes;
            ImmutableDictionary<int, ImmutableArray<bool>> dynamicLocalMap;
            ImmutableDictionary<int, ImmutableArray<string>> tupleLocalMap;
            ImmutableArray<ImmutableArray<ImportRecord>> importGroups;
            ImmutableArray<ExternAliasRecord> externAliases;
            ImmutableArray<string> localVariableNames;
            ImmutableArray<TLocalSymbol> localConstants;
            ILSpan reuseSpan;

            var methodHandle = (MethodDefinitionHandle)MetadataTokens.EntityHandle(methodToken);

            ReadLocalScopeInformation(
                reader,
                methodHandle,
                ilOffset,
                symbolProvider,
                isVisualBasicMethod,
                out importGroups,
                out externAliases,
                out localVariableNames,
                out dynamicLocalMap,
                out tupleLocalMap,
                out localConstants,
                out reuseSpan);
            ReadMethodCustomDebugInformation(reader, methodHandle, out hoistedLocalScopes, out defaultNamespace);

            return new MethodDebugInfo<TTypeSymbol, TLocalSymbol>(
                hoistedLocalScopes,
                importGroups,
                externAliases,
                dynamicLocalMap,
                tupleLocalMap,
                defaultNamespace,
                localVariableNames,
                localConstants,
                reuseSpan);
        }

        private static void ReadLocalScopeInformation(
            MetadataReader reader,
            MethodDefinitionHandle methodHandle,
            int ilOffset,
            EESymbolProvider<TTypeSymbol, TLocalSymbol> symbolProvider,
            bool isVisualBasicMethod,
            out ImmutableArray<ImmutableArray<ImportRecord>> importGroups,
            out ImmutableArray<ExternAliasRecord> externAliases,
            out ImmutableArray<string> localVariableNames,
            out ImmutableDictionary<int, ImmutableArray<bool>> dynamicLocalMap,
            out ImmutableDictionary<int, ImmutableArray<string>> tupleLocalMap,
            out ImmutableArray<TLocalSymbol> localConstants,
            out ILSpan reuseSpan)
        {
            var localVariableNamesBuilder = ArrayBuilder<string>.GetInstance();
            var localConstantsBuilder = ArrayBuilder<TLocalSymbol>.GetInstance();

            ImmutableDictionary<int, ImmutableArray<bool>>.Builder lazyDynamicLocalsBuilder = null;
            ImmutableDictionary<int, ImmutableArray<string>>.Builder lazyTupleLocalsBuilder = null;

            var innerMostImportScope = default(ImportScopeHandle);
            uint reuseSpanStart = 0;
            uint reuseSpanEnd = uint.MaxValue;
            try
            {
                foreach (var scopeHandle in reader.GetLocalScopes(methodHandle))
                {
                    try
                    {
                        var scope = reader.GetLocalScope(scopeHandle);
                        if (ilOffset < scope.StartOffset)
                        {
                            // scopes are sorted by StartOffset, hence all scopes that follow can't contain ilOffset
                            reuseSpanEnd = Math.Min(reuseSpanEnd, (uint)scope.StartOffset);
                            break;
                        }

                        if (ilOffset >= scope.EndOffset)
                        {
                            // ilOffset is not in this scope, go to next one
                            reuseSpanStart = Math.Max(reuseSpanStart, (uint)scope.EndOffset);
                            continue;
                        }

                        // reuse span is a subspan of the inner-most scope containing the IL offset:
                        reuseSpanStart = Math.Max(reuseSpanStart, (uint)scope.StartOffset);
                        reuseSpanEnd = Math.Min(reuseSpanEnd, (uint)scope.EndOffset);

                        // imports (use the inner-most):
                        innerMostImportScope = scope.ImportScope;

                        // locals (from all contained scopes):
                        foreach (var variableHandle in scope.GetLocalVariables())
                        {
                            var variable = reader.GetLocalVariable(variableHandle);
                            if ((variable.Attributes & LocalVariableAttributes.DebuggerHidden) != 0)
                            {
                                continue;
                            }

                            localVariableNamesBuilder.SetItem(variable.Index, reader.GetString(variable.Name));

                            var dynamicFlags = ReadDynamicCustomDebugInformation(reader, variableHandle);
                            if (!dynamicFlags.IsDefault)
                            {
                                lazyDynamicLocalsBuilder = lazyDynamicLocalsBuilder ?? ImmutableDictionary.CreateBuilder<int, ImmutableArray<bool>>();
                                lazyDynamicLocalsBuilder[variable.Index] = dynamicFlags;
                            }

                            var tupleElementNames = ReadTupleCustomDebugInformation(reader, variableHandle);
                            if (!tupleElementNames.IsDefault)
                            {
                                lazyTupleLocalsBuilder = lazyTupleLocalsBuilder ?? ImmutableDictionary.CreateBuilder<int, ImmutableArray<string>>();
                                lazyTupleLocalsBuilder[variable.Index] = tupleElementNames;
                            }
                        }

                        // constants (from all contained scopes):
                        foreach (var constantHandle in scope.GetLocalConstants())
                        {
                            var constant = reader.GetLocalConstant(constantHandle);

                            TTypeSymbol typeSymbol;
                            ConstantValue value;
                            var sigReader = reader.GetBlobReader(constant.Signature);
                            symbolProvider.DecodeLocalConstant(ref sigReader, out typeSymbol, out value);

                            var name = reader.GetString(constant.Name);
                            var dynamicFlags = ReadDynamicCustomDebugInformation(reader, constantHandle);
                            var tupleElementNames = ReadTupleCustomDebugInformation(reader, constantHandle);
                            localConstantsBuilder.Add(symbolProvider.GetLocalConstant(name, typeSymbol, value, dynamicFlags, tupleElementNames));
                        }
                    }
                    catch (Exception e) when (e is UnsupportedSignatureContent || e is BadImageFormatException)
                    {
                        // ignore scopes with invalid data
                    }
                }
            }
            finally
            {
                localVariableNames = localVariableNamesBuilder.ToImmutableAndFree();
                localConstants = localConstantsBuilder.ToImmutableAndFree();
                dynamicLocalMap = lazyDynamicLocalsBuilder?.ToImmutable();
                tupleLocalMap = lazyTupleLocalsBuilder?.ToImmutable();
                reuseSpan = new ILSpan(reuseSpanStart, reuseSpanEnd);
            }

            var importGroupsBuilder = ArrayBuilder<ImmutableArray<ImportRecord>>.GetInstance();
            var externAliasesBuilder = ArrayBuilder<ExternAliasRecord>.GetInstance();

            try
            {
                if (!innerMostImportScope.IsNil)
                {
                    PopulateImports(reader, innerMostImportScope, symbolProvider, isVisualBasicMethod, importGroupsBuilder, externAliasesBuilder);
                }
            }
            catch (Exception e) when (e is UnsupportedSignatureContent || e is BadImageFormatException)
            {
                // ignore invalid imports
            }

            importGroups = importGroupsBuilder.ToImmutableAndFree();
            externAliases = externAliasesBuilder.ToImmutableAndFree();
        }

        private static string ReadUtf8String(MetadataReader reader, BlobHandle handle)
        {
            var bytes = reader.GetBlobBytes(handle);
            return Encoding.UTF8.GetString(bytes, 0, bytes.Length);
        }

        /// <summary>
        /// Read UTF8 string with null terminator.
        /// </summary>
        private static string ReadUtf8String(ref BlobReader reader)
        {
            var builder = ArrayBuilder<byte>.GetInstance();
            while (true)
            {
                var b = reader.ReadByte();
                if (b == 0)
                {
                    break;
                }
                builder.Add(b);
            }
            var bytes = builder.ToArrayAndFree();
            return Encoding.UTF8.GetString(bytes, 0, bytes.Length);
        }

        /// <exception cref="BadImageFormatException">Invalid data format.</exception>
        private static void PopulateImports(
            MetadataReader reader,
            ImportScopeHandle handle,
            EESymbolProvider<TTypeSymbol, TLocalSymbol> symbolProvider,
            bool isVisualBasicMethod,
            ArrayBuilder<ImmutableArray<ImportRecord>> importGroupsBuilder,
            ArrayBuilder<ExternAliasRecord> externAliasesBuilder)
        {
            var importGroupBuilder = ArrayBuilder<ImportRecord>.GetInstance();

            while (!handle.IsNil)
            {
                var importScope = reader.GetImportScope(handle);

                foreach (ImportDefinition import in importScope.GetImports())
                {
                    switch (import.Kind)
                    {
                        case ImportDefinitionKind.ImportNamespace:
                            importGroupBuilder.Add(new ImportRecord(
                                ImportTargetKind.Namespace,
                                targetString: ReadUtf8String(reader, import.TargetNamespace)));
                            break;

                        case ImportDefinitionKind.ImportAssemblyNamespace:
                            importGroupBuilder.Add(new ImportRecord(
                                ImportTargetKind.Namespace,
                                targetString: ReadUtf8String(reader, import.TargetNamespace),
                                targetAssembly: symbolProvider.GetReferencedAssembly(import.TargetAssembly)));
                            break;

                        case ImportDefinitionKind.ImportType:
                            importGroupBuilder.Add(new ImportRecord(
                                ImportTargetKind.Type,
                                targetType: symbolProvider.GetType(import.TargetType)));
                            break;

                        case ImportDefinitionKind.ImportXmlNamespace:
                            importGroupBuilder.Add(new ImportRecord(
                                ImportTargetKind.XmlNamespace,
                                alias: ReadUtf8String(reader, import.Alias),
                                targetString: ReadUtf8String(reader, import.TargetNamespace)));
                            break;

                        case ImportDefinitionKind.ImportAssemblyReferenceAlias:
                            importGroupBuilder.Add(new ImportRecord(
                                ImportTargetKind.Assembly,
                                alias: ReadUtf8String(reader, import.Alias)));
                            break;

                        case ImportDefinitionKind.AliasAssemblyReference:
                            externAliasesBuilder.Add(new ExternAliasRecord(
                                alias: ReadUtf8String(reader, import.Alias),
                                targetAssembly: symbolProvider.GetReferencedAssembly(import.TargetAssembly)));
                            break;

                        case ImportDefinitionKind.AliasNamespace:
                            importGroupBuilder.Add(new ImportRecord(
                                ImportTargetKind.Namespace,
                                alias: ReadUtf8String(reader, import.Alias),
                                targetString: ReadUtf8String(reader, import.TargetNamespace)));
                            break;

                        case ImportDefinitionKind.AliasAssemblyNamespace:
                            importGroupBuilder.Add(new ImportRecord(
                                ImportTargetKind.Namespace,
                                alias: ReadUtf8String(reader, import.Alias),
                                targetString: ReadUtf8String(reader, import.TargetNamespace),
                                targetAssembly: symbolProvider.GetReferencedAssembly(import.TargetAssembly)));
                            break;

                        case ImportDefinitionKind.AliasType:
                            importGroupBuilder.Add(new ImportRecord(
                                ImportTargetKind.Type,
                                alias: ReadUtf8String(reader, import.Alias),
                                targetType: symbolProvider.GetType(import.TargetType)));
                            break;
                    }
                }

                // VB always expects two import groups (even if they are empty).
                // TODO: consider doing this for C# as well and handle empty groups in the binder.
                if (isVisualBasicMethod || importGroupBuilder.Count > 0)
                {
                    importGroupsBuilder.Add(importGroupBuilder.ToImmutable());
                    importGroupBuilder.Clear();
                }

                handle = importScope.Parent;
            }

            importGroupBuilder.Free();
        }

        /// <exception cref="BadImageFormatException">Invalid data format.</exception>
        private static void ReadMethodCustomDebugInformation(
            MetadataReader reader,
            MethodDefinitionHandle methodHandle,
            out ImmutableArray<HoistedLocalScopeRecord> hoistedLocalScopes,
            out string defaultNamespace)
        {
            CustomDebugInformation info;

            hoistedLocalScopes = TryGetCustomDebugInformation(reader, methodHandle, PortableCustomDebugInfoKinds.StateMachineHoistedLocalScopes, out info) ?
                DecodeHoistedLocalScopes(reader.GetBlobReader(info.Value)) :
                ImmutableArray<HoistedLocalScopeRecord>.Empty;

            // TODO: consider looking this up once per module (not for every method)
            defaultNamespace = TryGetCustomDebugInformation(reader, EntityHandle.ModuleDefinition, PortableCustomDebugInfoKinds.DefaultNamespace, out info) ?
                DecodeDefaultNamespace(reader.GetBlobReader(info.Value)) :
                null;
            defaultNamespace = defaultNamespace ?? "";
        }

        /// <exception cref="BadImageFormatException">Invalid data format.</exception>
        private static ImmutableArray<bool> ReadDynamicCustomDebugInformation(MetadataReader reader, EntityHandle variableOrConstantHandle)
        {
            CustomDebugInformation info;
            if (TryGetCustomDebugInformation(reader, variableOrConstantHandle, PortableCustomDebugInfoKinds.DynamicLocalVariables, out info))
            {
                return DecodeDynamicFlags(reader.GetBlobReader(info.Value));
            }
            return default(ImmutableArray<bool>);
        }

        /// <exception cref="BadImageFormatException">Invalid data format.</exception>
        private static ImmutableArray<string> ReadTupleCustomDebugInformation(MetadataReader reader, EntityHandle variableOrConstantHandle)
        {
            CustomDebugInformation info;
            if (TryGetCustomDebugInformation(reader, variableOrConstantHandle, PortableCustomDebugInfoKinds.TupleElementNames, out info))
            {
                return DecodeTupleElementNames(reader.GetBlobReader(info.Value));
            }
            return default(ImmutableArray<string>);
        }

        /// <exception cref="BadImageFormatException">Invalid data format.</exception>
        private static bool TryGetCustomDebugInformation(MetadataReader reader, EntityHandle handle, Guid kind, out CustomDebugInformation customDebugInfo)
        {
            bool foundAny = false;
            customDebugInfo = default(CustomDebugInformation);
            foreach (var infoHandle in reader.GetCustomDebugInformation(handle))
            {
                var info = reader.GetCustomDebugInformation(infoHandle);
                var id = reader.GetGuid(info.Kind);
                if (id == kind)
                {
                    if (foundAny)
                    {
                        throw new BadImageFormatException();
                    }
                    customDebugInfo = info;
                    foundAny = true;
                }
            }
            return foundAny;
        }

        /// <exception cref="BadImageFormatException">Invalid data format.</exception>
        private static ImmutableArray<bool> DecodeDynamicFlags(BlobReader reader)
        {
            var builder = ImmutableArray.CreateBuilder<bool>(reader.Length * 8);

            while (reader.RemainingBytes > 0)
            {
                int b = reader.ReadByte();
                for (int i = 1; i < 0x100; i <<= 1)
                {
                    builder.Add((b & i) != 0);
                }
            }

            return builder.MoveToImmutable();
        }

        /// <exception cref="BadImageFormatException">Invalid data format.</exception>
        private static ImmutableArray<string> DecodeTupleElementNames(BlobReader reader)
        {
            var builder = ArrayBuilder<string>.GetInstance();
            while (reader.RemainingBytes > 0)
            {
                var value = ReadUtf8String(ref reader);
                builder.Add(value.Length == 0 ? null : value);
            }
            return builder.ToImmutableAndFree();
        }

        /// <exception cref="BadImageFormatException">Invalid data format.</exception>
        private static ImmutableArray<HoistedLocalScopeRecord> DecodeHoistedLocalScopes(BlobReader reader)
        {
            var result = ArrayBuilder<HoistedLocalScopeRecord>.GetInstance();

            do
            {
                int startOffset = reader.ReadInt32();
                int length = reader.ReadInt32();

                result.Add(new HoistedLocalScopeRecord(startOffset, length));
            }
            while (reader.RemainingBytes > 0);

            return result.ToImmutableAndFree();
        }

        private static string DecodeDefaultNamespace(BlobReader reader)
        {
            return reader.ReadUTF8(reader.Length);
        }
    }
}
