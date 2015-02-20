// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.ExpressionEvaluator;
using Microsoft.VisualStudio.SymReaderInterop;

namespace Microsoft.CodeAnalysis.CSharp.ExpressionEvaluator
{
    internal static class SymUnmanagedReaderExtensions
    {
        public static MethodDebugInfo GetMethodDebugInfo(
            this ISymUnmanagedReader reader,
            int methodToken,
            int methodVersion)
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

            return new MethodDebugInfo(
                importRecordGroupBuilder.ToImmutableAndFree(),
                externAliasRecordBuilder.ToImmutableAndFree(),
                defaultNamespaceName: ""); // Unused in C#.
        }

        // TODO (acasey): overload for portable format (GH #702)
    }
}