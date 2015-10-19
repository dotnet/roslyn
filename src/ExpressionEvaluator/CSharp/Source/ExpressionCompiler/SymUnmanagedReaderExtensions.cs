// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.ExpressionEvaluator;
using Microsoft.DiaSymReader;

namespace Microsoft.CodeAnalysis.CSharp.ExpressionEvaluator
{
    internal static class SymUnmanagedReaderExtensions
    {
        public static MethodDebugInfo GetMethodDebugInfo(
            this ISymUnmanagedReader reader,
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

                        externAliasRecordBuilder.Add(new NativeExternAliasRecord<AssemblySymbol>(alias, targetIdentity));
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
                        .SelectAsArray(s => HoistedLocalScopeRecord.FromNative(s.StartOffset, s.EndOffset));
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

        // TODO (https://github.com/dotnet/roslyn/issues/702): overload for portable format
    }
}