// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Debugging;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.DiaSymReader;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.ExpressionEvaluator
{
    internal partial class MethodDebugInfo<TTypeSymbol, TLocalSymbol>
    {
        private struct LocalNameAndScope : IEquatable<LocalNameAndScope>
        {
            internal readonly string LocalName;
            internal readonly int ScopeStart;
            internal readonly int ScopeEnd;

            internal LocalNameAndScope(string localName, int scopeStart, int scopeEnd)
            {
                LocalName = localName;
                ScopeStart = scopeStart;
                ScopeEnd = scopeEnd;
            }

            public bool Equals(LocalNameAndScope other)
            {
                return ScopeStart == other.ScopeStart &&
                    ScopeEnd == other.ScopeEnd &&
                    string.Equals(LocalName, other.LocalName, StringComparison.Ordinal);
            }

            public override bool Equals(object obj)
            {
                throw new NotImplementedException();
            }

            public override int GetHashCode()
            {
                return Hash.Combine(
                    Hash.Combine(ScopeStart, ScopeEnd),
                    LocalName.GetHashCode());
            }
        }

        internal const int S_OK = 0x0;
        internal const int E_FAIL = unchecked((int)0x80004005);
        internal const int E_NOTIMPL = unchecked((int)0x80004001);
        private static readonly IntPtr s_ignoreIErrorInfo = new IntPtr(-1);

        public unsafe static MethodDebugInfo<TTypeSymbol, TLocalSymbol> ReadMethodDebugInfo(
            ISymUnmanagedReader3 symReader,
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

            if (symReader is ISymUnmanagedReader5 symReader5)
            {
                int hr = symReader5.GetPortableDebugMetadataByVersion(methodVersion, out byte* metadata, out int size);
                ThrowExceptionForHR(hr);

                if (hr == S_OK)
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
                var symMethod = symReader.GetMethodByVersion(methodToken, methodVersion);
                if (symMethod != null)
                {
                    symMethod.GetAllScopes(allScopes, containingScopes, ilOffset, isScopeEndInclusive: isVisualBasicMethod);
                }

                ImmutableArray<ImmutableArray<ImportRecord>> importRecordGroups;
                ImmutableArray<ExternAliasRecord> externAliasRecords;
                string defaultNamespaceName;

                if (isVisualBasicMethod)
                {
                    ReadVisualBasicImportsDebugInfo(
                        symReader,
                        methodToken,
                        methodVersion,
                        out importRecordGroups,
                        out defaultNamespaceName);

                    externAliasRecords = ImmutableArray<ExternAliasRecord>.Empty;
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

                    defaultNamespaceName = "";
                }

                // VB should read hoisted scope information from local variables:
                var hoistedLocalScopeRecords = isVisualBasicMethod ?
                    default(ImmutableArray<HoistedLocalScopeRecord>) :
                    ImmutableArray<HoistedLocalScopeRecord>.Empty;

                ImmutableDictionary<int, ImmutableArray<bool>> dynamicLocalMap = null;
                ImmutableDictionary<string, ImmutableArray<bool>> dynamicLocalConstantMap = null;
                ImmutableDictionary<int, ImmutableArray<string>> tupleLocalMap = null;
                ImmutableDictionary<LocalNameAndScope, ImmutableArray<string>> tupleLocalConstantMap = null;

                byte[] customDebugInfo = GetCustomDebugInfoBytes(symReader, methodToken, methodVersion);
                if (customDebugInfo != null)
                {
                    if (!isVisualBasicMethod)
                    {
                        var customDebugInfoRecord = CustomDebugInfoReader.TryGetCustomDebugInfoRecord(customDebugInfo, CustomDebugInfoKind.StateMachineHoistedLocalScopes);
                        if (!customDebugInfoRecord.IsDefault)
                        {
                            hoistedLocalScopeRecords = CustomDebugInfoReader.DecodeStateMachineHoistedLocalScopesRecord(customDebugInfoRecord)
                                .SelectAsArray(s => new HoistedLocalScopeRecord(s.StartOffset, s.Length));
                        }

                        GetCSharpDynamicLocalInfo(
                            customDebugInfo,
                            allScopes,
                            out dynamicLocalMap,
                            out dynamicLocalConstantMap);
                    }

                    GetTupleElementNamesLocalInfo(
                        customDebugInfo,
                        out tupleLocalMap,
                        out tupleLocalConstantMap);
                }

                var constantsBuilder = ArrayBuilder<TLocalSymbol>.GetInstance();
                if (symbolProviderOpt != null) // TODO
                {
                    GetConstants(constantsBuilder, symbolProviderOpt, containingScopes, dynamicLocalConstantMap, tupleLocalConstantMap);
                }

                var reuseSpan = GetReuseSpan(allScopes, ilOffset, isVisualBasicMethod);

                return new MethodDebugInfo<TTypeSymbol, TLocalSymbol>(
                    hoistedLocalScopeRecords,
                    importRecordGroups,
                    externAliasRecords,
                    dynamicLocalMap,
                    tupleLocalMap,
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

        private static void ThrowExceptionForHR(int hr)
        {
            // E_FAIL indicates "no info".
            // E_NOTIMPL indicates a lack of ISymUnmanagedReader support (in a particular implementation).
            if (hr < 0 && hr != E_FAIL && hr != E_NOTIMPL)
            {
                Marshal.ThrowExceptionForHR(hr, s_ignoreIErrorInfo);
            }
        }

        /// <summary>
        /// Get the blob of binary custom debug info for a given method.
        /// </summary>
        private static byte[] GetCustomDebugInfoBytes(ISymUnmanagedReader3 reader, int methodToken, int methodVersion)
        {
            try
            {
                return reader.GetCustomDebugInfo(methodToken, methodVersion);
            }
            catch (ArgumentOutOfRangeException)
            {
                // Sometimes the debugger returns the HRESULT for ArgumentOutOfRangeException, rather than E_FAIL,
                // for methods without custom debug info (https://github.com/dotnet/roslyn/issues/4138).
                return null;
            }
        }

        /// <summary>
        /// Get the (unprocessed) import strings for a given method.
        /// </summary>
        /// <remarks>
        /// Doesn't consider forwarding.
        /// 
        /// CONSIDER: Dev12 doesn't just check the root scope - it digs around to find the best
        /// match based on the IL offset and then walks up to the root scope (see PdbUtil::GetScopeFromOffset).
        /// However, it's not clear that this matters, since imports can't be scoped in VB.  This is probably
        /// just based on the way they were extracting locals and constants based on a specific scope.
        /// 
        /// Returns empty array if there are no import strings for the specified method.
        /// </remarks>
        private static ImmutableArray<string> GetImportStrings(ISymUnmanagedReader reader, int methodToken, int methodVersion)
        {
            var method = reader.GetMethodByVersion(methodToken, methodVersion);
            if (method == null)
            {
                // In rare circumstances (only bad PDBs?) GetMethodByVersion can return null.
                // If there's no debug info for the method, then no import strings are available.
                return ImmutableArray<string>.Empty;
            }

            ISymUnmanagedScope rootScope = method.GetRootScope();
            if (rootScope == null)
            {
                Debug.Assert(false, "Expected a root scope.");
                return ImmutableArray<string>.Empty;
            }

            var childScopes = rootScope.GetChildren();
            if (childScopes.Length == 0)
            {
                // It seems like there should always be at least one child scope, but we've
                // seen PDBs where that is not the case.
                return ImmutableArray<string>.Empty;
            }

            // As in NamespaceListWrapper::Init, we only consider namespaces in the first
            // child of the root scope.
            ISymUnmanagedScope firstChildScope = childScopes[0];

            var namespaces = firstChildScope.GetNamespaces();
            if (namespaces.Length == 0)
            {
                // It seems like there should always be at least one namespace (i.e. the global
                // namespace), but we've seen PDBs where that is not the case.
                return ImmutableArray<string>.Empty;
            }

            return ImmutableArray.CreateRange(namespaces.Select(n => n.GetName()));
        }

        private static void ReadCSharpNativeImportsInfo(
            ISymUnmanagedReader3 reader,
            EESymbolProvider<TTypeSymbol, TLocalSymbol> symbolProvider,
            int methodToken,
            int methodVersion,
            out ImmutableArray<ImmutableArray<ImportRecord>> importRecordGroups,
            out ImmutableArray<ExternAliasRecord> externAliasRecords)
        {
            ImmutableArray<string> externAliasStrings;

            var importStringGroups = CustomDebugInfoReader.GetCSharpGroupedImportStrings(
                methodToken,
                KeyValuePairUtil.Create(reader, methodVersion),
                getMethodCustomDebugInfo: (token, arg) => GetCustomDebugInfoBytes(arg.Key, token, arg.Value),
                getMethodImportStrings: (token, arg) => GetImportStrings(arg.Key, token, arg.Value),
                externAliasStrings: out externAliasStrings);

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

        /// <exception cref="InvalidOperationException">Bad data.</exception>
        private static void GetCSharpDynamicLocalInfo(
            byte[] customDebugInfo,
            IEnumerable<ISymUnmanagedScope> scopes,
            out ImmutableDictionary<int, ImmutableArray<bool>> dynamicLocalMap,
            out ImmutableDictionary<string, ImmutableArray<bool>> dynamicLocalConstantMap)
        {
            dynamicLocalMap = null;
            dynamicLocalConstantMap = null;

            var record = CustomDebugInfoReader.TryGetCustomDebugInfoRecord(customDebugInfo, CustomDebugInfoKind.DynamicLocals);
            if (record.IsDefault)
            {
                return;
            }

            var localKindsByName = PooledDictionary<string, LocalKind>.GetInstance();
            GetLocalKindByName(localKindsByName, scopes);

            ImmutableDictionary<int, ImmutableArray<bool>>.Builder localBuilder = null;
            ImmutableDictionary<string, ImmutableArray<bool>>.Builder constantBuilder = null;

            var dynamicLocals = CustomDebugInfoReader.DecodeDynamicLocalsRecord(record);
            foreach (var dynamicLocal in dynamicLocals)
            {
                int slot = dynamicLocal.SlotId;
                var flags = dynamicLocal.Flags;
                if (slot == 0)
                {
                    LocalKind kind;
                    var name = dynamicLocal.LocalName;
                    localKindsByName.TryGetValue(name, out kind);
                    switch (kind)
                    {
                        case LocalKind.DuplicateName:
                            // Drop locals with ambiguous names.
                            continue;
                        case LocalKind.ConstantName:
                            constantBuilder = constantBuilder ?? ImmutableDictionary.CreateBuilder<string, ImmutableArray<bool>>();
                            constantBuilder[name] = flags;
                            continue;
                    }
                }
                localBuilder = localBuilder ?? ImmutableDictionary.CreateBuilder<int, ImmutableArray<bool>>();
                localBuilder[slot] = flags;
            }


            if (localBuilder != null)
            {
                dynamicLocalMap = localBuilder.ToImmutable();
            }

            if (constantBuilder != null)
            {
                dynamicLocalConstantMap = constantBuilder.ToImmutable();
            }

            localKindsByName.Free();
        }

        private enum LocalKind { DuplicateName, VariableName, ConstantName }

        /// <summary>
        /// Dynamic CDI encodes slot id and name for each dynamic local variable, but only name for a constant. 
        /// Constants have slot id set to 0. As a result there is a potential for ambiguity. If a variable in a slot 0
        /// and a constant defined anywhere in the method body have the same name we can't say which one 
        /// the dynamic flags belong to (if there is a dynamic record for at least one of them).
        /// 
        /// This method returns the local kind (variable, constant, or duplicate) based on name.
        /// </summary>
        private static void GetLocalKindByName(Dictionary<string, LocalKind> localNames, IEnumerable<ISymUnmanagedScope> scopes)
        {
            Debug.Assert(localNames.Count == 0);

            var localSlot0 = scopes.SelectMany(scope => scope.GetLocals()).FirstOrDefault(variable => variable.GetSlot() == 0);
            if (localSlot0 != null)
            {
                localNames.Add(localSlot0.GetName(), LocalKind.VariableName);
            }

            foreach (var scope in scopes)
            {
                foreach (var constant in scope.GetConstants())
                {
                    string name = constant.GetName();
                    localNames[name] = localNames.ContainsKey(name) ? LocalKind.DuplicateName : LocalKind.ConstantName;
                }
            }
        }

        private static void GetTupleElementNamesLocalInfo(
            byte[] customDebugInfo,
            out ImmutableDictionary<int, ImmutableArray<string>> tupleLocalMap,
            out ImmutableDictionary<LocalNameAndScope, ImmutableArray<string>> tupleLocalConstantMap)
        {
            tupleLocalMap = null;
            tupleLocalConstantMap = null;

            var record = CustomDebugInfoReader.TryGetCustomDebugInfoRecord(customDebugInfo, CustomDebugInfoKind.TupleElementNames);
            if (record.IsDefault)
            {
                return;
            }

            ImmutableDictionary<int, ImmutableArray<string>>.Builder localBuilder = null;
            ImmutableDictionary<LocalNameAndScope, ImmutableArray<string>>.Builder constantBuilder = null;

            var tuples = CustomDebugInfoReader.DecodeTupleElementNamesRecord(record);
            foreach (var tuple in tuples)
            {
                var slotIndex = tuple.SlotIndex;
                var elementNames = tuple.ElementNames;
                if (slotIndex < 0)
                {
                    constantBuilder = constantBuilder ?? ImmutableDictionary.CreateBuilder<LocalNameAndScope, ImmutableArray<string>>();
                    var localAndScope = new LocalNameAndScope(tuple.LocalName, tuple.ScopeStart, tuple.ScopeEnd);
                    constantBuilder[localAndScope] = elementNames;
                }
                else
                {
                    localBuilder = localBuilder ?? ImmutableDictionary.CreateBuilder<int, ImmutableArray<string>>();
                    localBuilder[slotIndex] = elementNames;
                }
            }

            if (localBuilder != null)
            {
                tupleLocalMap = localBuilder.ToImmutable();
            }

            if (constantBuilder != null)
            {
                tupleLocalConstantMap = constantBuilder.ToImmutable();
            }
        }

        private static void ReadVisualBasicImportsDebugInfo(
            ISymUnmanagedReader reader,
            int methodToken,
            int methodVersion,
            out ImmutableArray<ImmutableArray<ImportRecord>> importRecordGroups,
            out string defaultNamespaceName)
        {
            importRecordGroups = ImmutableArray<ImmutableArray<ImportRecord>>.Empty;

            var importStrings = CustomDebugInfoReader.GetVisualBasicImportStrings(
                methodToken,
                KeyValuePairUtil.Create(reader, methodVersion),
                (token, arg) => GetImportStrings(arg.Key, token, arg.Value));

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
                    VBImportScopeKind scope = 0;

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
                    VBImportScopeKind scope = 0;

                    if (TryCreateImportRecordFromVisualBasicImportString(importString, out importRecord, out scope))
                    {
                        if (scope == VBImportScopeKind.Project)
                        {
                            projectLevelImportRecords.Add(importRecord);
                        }
                        else
                        {
                            Debug.Assert(scope == VBImportScopeKind.File || scope == VBImportScopeKind.Unspecified);
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

        private static bool TryCreateImportRecordFromVisualBasicImportString(string importString, out ImportRecord record, out VBImportScopeKind scope)
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

        private static void GetConstants(
            ArrayBuilder<TLocalSymbol> builder,
            EESymbolProvider<TTypeSymbol, TLocalSymbol> symbolProvider,
            ArrayBuilder<ISymUnmanagedScope> scopes,
            ImmutableDictionary<string, ImmutableArray<bool>> dynamicLocalConstantMapOpt,
            ImmutableDictionary<LocalNameAndScope, ImmutableArray<string>> tupleLocalConstantMapOpt)
        {
            foreach (var scope in scopes)
            {
                foreach (var constant in scope.GetConstants())
                {
                    string name = constant.GetName();
                    object rawValue = constant.GetValue();
                    var signature = constant.GetSignature().ToImmutableArray();

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
                    if (dynamicLocalConstantMapOpt != null)
                    {
                        dynamicLocalConstantMapOpt.TryGetValue(name, out dynamicFlags);
                    }

                    var tupleElementNames = default(ImmutableArray<string>);
                    if (tupleLocalConstantMapOpt != null)
                    {
                        int scopeStart = scope.GetStartOffset();
                        int scopeEnd = scope.GetEndOffset();
                        tupleLocalConstantMapOpt.TryGetValue(new LocalNameAndScope(name, scopeStart, scopeEnd), out tupleElementNames);
                    }

                    builder.Add(symbolProvider.GetLocalConstant(name, type, constantValue, dynamicFlags, tupleElementNames));
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
            ImmutableDictionary<int, ImmutableArray<bool>> dynamicLocalMapOpt,
            ImmutableDictionary<int, ImmutableArray<string>> tupleLocalConstantMapOpt)
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

                var tupleElementNames = default(ImmutableArray<string>);
                tupleLocalConstantMapOpt?.TryGetValue(i, out tupleElementNames);

                builder.Add(symbolProvider.GetLocalVariable(name, i, localInfo[i], dynamicFlags, tupleElementNames));
            }
        }
    }
}
