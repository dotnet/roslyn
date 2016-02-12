// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Metadata;
using Microsoft.DiaSymReader;

namespace Microsoft.CodeAnalysis.ExpressionEvaluator
{
    internal partial class MethodDebugInfo<TTypeSymbol, TLocalSymbol>
    {
        public unsafe static MethodDebugInfo<TTypeSymbol, TLocalSymbol> ReadMethodDebugInfo(
            ISymUnmanagedReader symReader,
            EESymbolProvider<TTypeSymbol, TLocalSymbol> symbolProviderOpt, // TODO: only null in DTEE case where we looking for default namesapace
            int methodToken,
            int methodVersion,
            int ilOffset,
            bool isVisualBasicMethod)
        {
            // no symbols
            if (symReader == null)
            {
                return None;
            }

            var symReader4 = symReader as ISymUnmanagedReader4;
            if (symReader4 != null) // TODO: VB Portable PDBs
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
                        return ReadFromPortable(mdReader, methodToken, ilOffset, symbolProviderOpt, isVisualBasicMethod);
                    }
                    catch (BadImageFormatException)
                    {
                        // bad CDI, ignore
                        return None;
                    }
                }
            }

            var allScopes = ArrayBuilder<ISymUnmanagedScope>.GetInstance();
            var containingScopes = ArrayBuilder<ISymUnmanagedScope>.GetInstance();

            try
            {
                ImmutableArray<HoistedLocalScopeRecord> hoistedLocalScopeRecords;
                ImmutableArray<ImmutableArray<ImportRecord>> importRecordGroups;
                ImmutableArray<ExternAliasRecord> externAliasRecords;
                ImmutableDictionary<int, ImmutableArray<bool>> dynamicLocalMap;
                ImmutableDictionary<string, ImmutableArray<bool>> dynamicLocalConstantMap;
                string defaultNamespaceName;

                var symMethod = symReader.GetMethodByVersion(methodToken, methodVersion);
                if (symMethod != null)
                {
                    symMethod.GetAllScopes(allScopes, containingScopes, ilOffset, isScopeEndInclusive: isVisualBasicMethod);
                }

                if (isVisualBasicMethod)
                {
                    ReadVisualBasicImportsDebugInfo(
                        symReader,
                        methodToken,
                        methodVersion,
                        out importRecordGroups,
                        out defaultNamespaceName);

                    hoistedLocalScopeRecords = ImmutableArray<HoistedLocalScopeRecord>.Empty;
                    externAliasRecords = ImmutableArray<ExternAliasRecord>.Empty;
                    dynamicLocalMap = null;
                    dynamicLocalConstantMap = null;
                }
                else
                {
                    Debug.Assert(symbolProviderOpt != null);

                    ReadCSharpNativeImportsInfo(
                        symReader,
                        symbolProviderOpt,
                        methodToken,
                        methodVersion,
                        out importRecordGroups,
                        out externAliasRecords);

                    ReadCSharpNativeCustomDebugInfo(
                        symReader,
                        methodToken,
                        methodVersion,
                        allScopes,
                        out hoistedLocalScopeRecords,
                        out dynamicLocalMap,
                        out dynamicLocalConstantMap);

                    defaultNamespaceName = "";
                }

                var constantsBuilder = ArrayBuilder<TLocalSymbol>.GetInstance();
                if (symbolProviderOpt != null) // TODO
                {
                    GetConstants(constantsBuilder, symbolProviderOpt, containingScopes, dynamicLocalConstantMap);
                }

                var reuseSpan = GetReuseSpan(allScopes, ilOffset, isVisualBasicMethod);

                return new MethodDebugInfo<TTypeSymbol, TLocalSymbol>(
                    hoistedLocalScopeRecords,
                    importRecordGroups,
                    externAliasRecords,
                    dynamicLocalMap,
                    defaultNamespaceName,
                    containingScopes.GetLocalNames(),
                    constantsBuilder.ToImmutableAndFree(),
                    reuseSpan);
            }
            catch (InvalidOperationException)
            {
                // bad CDI, ignore
                return None;
            }
            finally
            {
                allScopes.Free();
                containingScopes.Free();
            }
        }

        private static void ReadCSharpNativeImportsInfo(
            ISymUnmanagedReader reader,
            EESymbolProvider<TTypeSymbol, TLocalSymbol> symbolProvider,
            int methodToken,
            int methodVersion,
            out ImmutableArray<ImmutableArray<ImportRecord>> importRecordGroups,
            out ImmutableArray<ExternAliasRecord> externAliasRecords)
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
                        if (TryCreateImportRecordFromCSharpImportString(symbolProvider, importString, out record))
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

                        externAliasRecordBuilder.Add(new ExternAliasRecord(alias, targetIdentity));
                    }
                }
            }

            importRecordGroups = importRecordGroupBuilder?.ToImmutableAndFree() ?? ImmutableArray<ImmutableArray<ImportRecord>>.Empty;
            externAliasRecords = externAliasRecordBuilder?.ToImmutableAndFree() ?? ImmutableArray<ExternAliasRecord>.Empty;
        }

        private static bool TryCreateImportRecordFromCSharpImportString(EESymbolProvider<TTypeSymbol, TLocalSymbol> symbolProvider, string importString, out ImportRecord record)
        {
            ImportTargetKind targetKind;
            string externAlias;
            string alias;
            string targetString;
            if (CustomDebugInfoReader.TryParseCSharpImportString(importString, out alias, out externAlias, out targetString, out targetKind))
            {
                ITypeSymbol type = null;
                if (targetKind == ImportTargetKind.Type)
                {
                    type = symbolProvider.GetTypeSymbolForSerializedType(targetString);
                    targetString = null;
                }

                record = new ImportRecord(
                    targetKind: targetKind,
                    alias: alias,
                    targetType: type,
                    targetString: targetString,
                    targetAssembly: null,
                    targetAssemblyAlias: externAlias);

                return true;
            }

            record = default(ImportRecord);
            return false;
        }

        private static void ReadCSharpNativeCustomDebugInfo(
            ISymUnmanagedReader reader,
            int methodToken,
            int methodVersion,
            IEnumerable<ISymUnmanagedScope> scopes,
            out ImmutableArray<HoistedLocalScopeRecord> hoistedLocalScopeRecords,
            out ImmutableDictionary<int, ImmutableArray<bool>> dynamicLocalMap,
            out ImmutableDictionary<string, ImmutableArray<bool>> dynamicLocalConstantMap)
        {
            hoistedLocalScopeRecords = ImmutableArray<HoistedLocalScopeRecord>.Empty;
            dynamicLocalMap = ImmutableDictionary<int, ImmutableArray<bool>>.Empty;
            dynamicLocalConstantMap = ImmutableDictionary<string, ImmutableArray<bool>>.Empty;

            byte[] customDebugInfoBytes = reader.GetCustomDebugInfoBytes(methodToken, methodVersion);
            if (customDebugInfoBytes == null)
            {
                return;
            }

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

        private static void ReadVisualBasicImportsDebugInfo(
            ISymUnmanagedReader reader,
            int methodToken,
            int methodVersion,
            out ImmutableArray<ImmutableArray<ImportRecord>> importRecordGroups,
            out string defaultNamespaceName)
        {
            importRecordGroups = ImmutableArray<ImmutableArray<ImportRecord>>.Empty;

            var importStrings = reader.GetVisualBasicImportStrings(methodToken, methodVersion);
            if (importStrings.IsDefault)
            {
                defaultNamespaceName = "";
                return;
            }

            defaultNamespaceName = null;
            var projectLevelImportRecords = ArrayBuilder<ImportRecord>.GetInstance();
            var fileLevelImportRecords = ArrayBuilder<ImportRecord>.GetInstance();

            foreach (string importString in importStrings)
            {
                Debug.Assert(importString != null);

                if (importString.Length > 0 && importString[0] == '*')
                {
                    string alias = null;
                    string target = null;
                    ImportTargetKind kind = 0;
                    ImportScope scope = 0;

                    if (!CustomDebugInfoReader.TryParseVisualBasicImportString(importString, out alias, out target, out kind, out scope))
                    {
                        Debug.WriteLine($"Unable to parse import string '{importString}'");
                        continue;
                    }
                    else if (kind == ImportTargetKind.Defunct)
                    {
                        continue;
                    }

                    Debug.Assert(alias == null); // The default namespace is never aliased.
                    Debug.Assert(target != null);
                    Debug.Assert(kind == ImportTargetKind.DefaultNamespace);

                    // We only expect to see one of these, but it looks like ProcedureContext::LoadImportsAndDefaultNamespaceNormal
                    // implicitly uses the last one if there are multiple.
                    Debug.Assert(defaultNamespaceName == null);

                    defaultNamespaceName = target;
                }
                else
                {
                    ImportRecord importRecord;
                    ImportScope scope = 0;

                    if (TryCreateImportRecordFromVisualBasicImportString(importString, out importRecord, out scope))
                    {
                        if (scope == ImportScope.Project)
                        {
                            projectLevelImportRecords.Add(importRecord);
                        }
                        else
                        {
                            Debug.Assert(scope == ImportScope.File || scope == ImportScope.Unspecified);
                            fileLevelImportRecords.Add(importRecord);
                        }
                    }
                    else
                    {
                        Debug.WriteLine($"Failed to parse import string {importString}");
                    }
                }
            }

            importRecordGroups = ImmutableArray.Create(
                fileLevelImportRecords.ToImmutableAndFree(),
                projectLevelImportRecords.ToImmutableAndFree());

            defaultNamespaceName = defaultNamespaceName ?? "";
        }

        private static bool TryCreateImportRecordFromVisualBasicImportString(string importString, out ImportRecord record, out ImportScope scope)
        {
            ImportTargetKind targetKind;
            string alias;
            string targetString;
            if (CustomDebugInfoReader.TryParseVisualBasicImportString(importString, out alias, out targetString, out targetKind, out scope))
            {
                record = new ImportRecord(
                    targetKind: targetKind,
                    alias: alias,
                    targetType: null,
                    targetString: targetString,
                    targetAssembly: null,
                    targetAssemblyAlias: null);

                return true;
            }

            record = default(ImportRecord);
            return false;
        }

        private static ILSpan GetReuseSpan(ArrayBuilder<ISymUnmanagedScope> scopes, int ilOffset, bool isEndInclusive)
        {
            return MethodContextReuseConstraints.CalculateReuseSpan(
                ilOffset,
                ILSpan.MaxValue,
                scopes.Select(scope => new ILSpan((uint)scope.GetStartOffset(), (uint)(scope.GetEndOffset() + (isEndInclusive ? 1 : 0)))));
        }

        public static void GetConstants(
            ArrayBuilder<TLocalSymbol> builder,
            EESymbolProvider<TTypeSymbol, TLocalSymbol> symbolProvider,
            ArrayBuilder<ISymUnmanagedScope> scopes,
            ImmutableDictionary<string, ImmutableArray<bool>> dynamicLocalConstantMapOpt)
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
        public static void GetLocals(
            ArrayBuilder<TLocalSymbol> builder,
            EESymbolProvider<TTypeSymbol, TLocalSymbol> symbolProvider,
            ImmutableArray<string> names,
            ImmutableArray<LocalInfo<TTypeSymbol>> localInfo,
            ImmutableDictionary<int, ImmutableArray<bool>> dynamicLocalMapOpt)
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