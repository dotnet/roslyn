// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal class ExternalUsingsBinder : Binder
    {
        internal ExternalUsingsBinder(Binder next)
            : base(next)
        {
        }

        internal override void LookupSymbolsInSingleBinder(
            LookupResult result, string name, int arity, ConsList<Symbol> basesBeingResolved, LookupOptions options, Binder originalBinder, bool diagnose, ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            if (!ShouldLookInUsings(options))
            {
                return;
            }

            LookupResult tmp = LookupResult.GetInstance();

            // usings:
            Imports.LookupSymbolInUsings(Compilation.ExternalUsings, originalBinder, tmp, name, arity, basesBeingResolved, options, diagnose, ref useSiteDiagnostics);

            // if we found a viable result in imported namespaces, use it instead of unviable symbols found in source:
            if (tmp.IsMultiViable)
            {
                result.MergeEqual(tmp);
            }

            tmp.Free();
        }

        protected override void AddLookupSymbolsInfoInSingleBinder(
            LookupSymbolsInfo result, LookupOptions options, Binder originalBinder)
        {
            if (!ShouldLookInUsings(options))
            {
                return;
            }

            // Add types within namespaces imported through usings, but don't add nested namespaces.
            LookupOptions usingOptions = (options & ~(LookupOptions.NamespaceAliasesOnly | LookupOptions.NamespacesOrTypesOnly)) | LookupOptions.MustNotBeNamespace;

            Imports.AddLookupSymbolsInfoInUsings(Compilation.ExternalUsings, originalBinder, result, usingOptions);
        }

        private static bool ShouldLookInUsings(LookupOptions options)
        {
            return (options & (LookupOptions.NamespaceAliasesOnly | LookupOptions.LabelsOnly)) == 0;
        }

        internal override bool SupportsExtensionMethods
        {
            get
            {
                return true;
            }
        }

        internal override void GetCandidateExtensionMethods(
            bool searchUsingsNotNamespace,
            ArrayBuilder<MethodSymbol> methods,
            string name,
            int arity,
            LookupOptions options,
            bool isCallerSemanticModel)
        {
            if (!searchUsingsNotNamespace)
            {
                return;
            }

            foreach (var @using in Compilation.ExternalUsings)
            {
                if (@using.NamespaceOrType.Kind == SymbolKind.Namespace)
                {
                    ((NamespaceSymbol)@using.NamespaceOrType).GetExtensionMethods(methods, name, arity, options);
                }
            }
        }
    }
}
