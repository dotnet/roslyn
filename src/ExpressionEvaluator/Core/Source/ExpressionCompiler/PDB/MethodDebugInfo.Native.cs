// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection.Metadata;
using Microsoft.DiaSymReader;

namespace Microsoft.CodeAnalysis.ExpressionEvaluator
{
    partial struct MethodDebugInfo
    {
        public unsafe static bool TryReadMethodDebugInfo(ISymUnmanagedReader symReader, int methodToken, int methodVersion, ArrayBuilder<ISymUnmanagedScope> allScopes, out MethodDebugInfo info)
        {
            var symReader4 = symReader as ISymUnmanagedReader4;
            if (symReader4 != null)
            {
                byte* metadata;
                int size;

                // TODO: version
                int hr = symReader4.GetPortableDebugMetadata(out metadata, out size);
                SymUnmanagedReaderExtensions.ThrowExceptionForHR(hr);

                if (metadata != null)
                {
                    var mdReader = new MetadataReader(metadata, size);
                    try
                    {
                        info = ReadFromPortable(mdReader, methodToken);
                        return true;
                    }
                    catch (BadImageFormatException)
                    {
                        // bad CDI, ignore
                        info = default(MethodDebugInfo);
                        return false;
                    }
                }
            }

            try
            {
                info = ReadFromNative(symReader, methodToken, methodVersion, allScopes);
                return true;
            }
            catch (InvalidOperationException)
            {
                // bad CDI, ignore
                info = default(MethodDebugInfo);
                return false;
            }
        }

        private static MethodDebugInfo ReadFromNative(
            ISymUnmanagedReader reader,
            int methodToken,
            int methodVersion,
            ArrayBuilder<ISymUnmanagedScope> scopes)
        {
            ImmutableArray<string> externAliasStrings;
            var importStringGroups = reader.GetCSharpGroupedImportStrings(methodToken, methodVersion, out externAliasStrings);
            Debug.Assert(importStringGroups.IsDefault == externAliasStrings.IsDefault);

            ArrayBuilder<ImmutableArray<ImportRecord>> importRecordGroupBuilder = null;
            ArrayBuilder<ExternAliasRecord> externAliasRecordBuilder = null;
            if (!importStringGroups.IsDefault)
            {
                importRecordGroupBuilder = ArrayBuilder<ImmutableArray<ImportRecord>>.GetInstance(importStringGroups.Length);
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

                if (!externAliasStrings.IsDefault)
                {
                    externAliasRecordBuilder = ArrayBuilder<ExternAliasRecord>.GetInstance(externAliasStrings.Length);
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

                        externAliasRecordBuilder.Add(new NativeExternAliasRecord(alias, targetIdentity));
                    }
                }
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
                        .SelectAsArray(s => new HoistedLocalScopeRecord(s.StartOffset, s.EndOffset - s.StartOffset + 1));
                }

                CustomDebugInfoReader.GetCSharpDynamicLocalInfo(
                    customDebugInfoBytes,
                    methodToken,
                    methodVersion,
                    scopes,
                    out dynamicLocalMap,
                    out dynamicLocalConstantMap);
            }

            return new MethodDebugInfo(
                hoistedLocalScopeRecords,
                importRecordGroupBuilder?.ToImmutableAndFree() ?? ImmutableArray<ImmutableArray<ImportRecord>>.Empty,
                externAliasRecordBuilder?.ToImmutableAndFree() ?? ImmutableArray<ExternAliasRecord>.Empty,
                dynamicLocalMap,
                dynamicLocalConstantMap,
                defaultNamespaceName: ""); // Unused in C#.
        }

        public static void GetConstants<TTypeSymbol, TLocalSymbol>(
            ArrayBuilder<TLocalSymbol> builder,
            EESymbolProvider<TTypeSymbol, TLocalSymbol> symbolProvider,
            ArrayBuilder<ISymUnmanagedScope> scopes,
            ImmutableDictionary<string, ImmutableArray<bool>> dynamicLocalConstantMapOpt)
            where TTypeSymbol : class, ITypeSymbol
            where TLocalSymbol : class
        {
            foreach (var scope in scopes)
            {
                foreach (var constant in scope.GetConstants())
                {
                    string name = constant.GetName();
                    object rawValue = constant.GetValue();
                    var signature = constant.GetSignature();

                    TTypeSymbol type;
                    try
                    {
                        type = symbolProvider.DecodeLocalVariableType(signature);
                    }
                    catch (Exception e) when (e is UnsupportedSignatureContent || e is BadImageFormatException)
                    {
                        // ignore 
                        continue;
                    }

                    if (type.Kind == SymbolKind.ErrorType)
                    {
                        continue;
                    }

                    ConstantValue constantValue = PdbHelpers.GetSymConstantValue(type, rawValue);

                    // TODO (https://github.com/dotnet/roslyn/issues/1815): report error properly when the symbol is used
                    if (constantValue.IsBad)
                    {
                        continue;
                    }

                    var dynamicFlags = default(ImmutableArray<bool>);
                    dynamicLocalConstantMapOpt?.TryGetValue(name, out dynamicFlags);

                    builder.Add(symbolProvider.GetLocalConstant(name, type, constantValue, dynamicFlags));
                }
            }
        }

        /// <summary>
        /// Returns symbols for the locals emitted in the original method,
        /// based on the local signatures from the IL and the names and
        /// slots from the PDB. The actual locals are needed to ensure the
        /// local slots in the generated method match the original.
        /// </summary>
        public static void GetLocals<TLocalSymbol, TTypeSymbol>(
            ArrayBuilder<TLocalSymbol> builder,
            EESymbolProvider<TTypeSymbol, TLocalSymbol> symbolProvider,
            ImmutableArray<string> names,
            ImmutableArray<LocalInfo<TTypeSymbol>> localInfo,
            ImmutableDictionary<int, ImmutableArray<bool>> dynamicLocalMapOpt)
            where TTypeSymbol : class, ITypeSymbol
            where TLocalSymbol : class
        {
            if (localInfo.Length == 0)
            {
                // When debugging a .dmp without a heap, localInfo will be empty although
                // names may be non-empty if there is a PDB. Since there's no type info, the
                // locals are dropped. Note this means the local signature of any generated
                // method will not match the original signature, so new locals will overlap
                // original locals. That is ok since there is no live process for the debugger
                // to update (any modified values exist in the debugger only).
                return;
            }

            Debug.Assert(localInfo.Length >= names.Length);

            for (int i = 0; i < localInfo.Length; i++)
            {
                string name = (i < names.Length) ? names[i] : null;

                var dynamicFlags = default(ImmutableArray<bool>);
                dynamicLocalMapOpt?.TryGetValue(i, out dynamicFlags);

                builder.Add(symbolProvider.GetLocalVariable(name, i, localInfo[i], dynamicFlags));
            }
        }
    }
}