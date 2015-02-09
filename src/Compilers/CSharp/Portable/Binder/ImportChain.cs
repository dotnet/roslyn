// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.Emit;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed class ImportChain : Cci.IImportScope
    {
        public readonly Imports Imports;
        public readonly ImportChain ParentOpt;

        private ImmutableArray<Cci.UsedNamespaceOrType> _lazyTranslatedImports;

        public ImportChain(Imports imports, ImportChain parentOpt)
        {
            Debug.Assert(imports != null);

            Imports = imports;
            ParentOpt = parentOpt;
        }

        ImmutableArray<Cci.UsedNamespaceOrType> Cci.IImportScope.GetUsedNamespaces(EmitContext context)
        {
            if (_lazyTranslatedImports.IsDefault)
            {
                ImmutableInterlocked.InterlockedInitialize(ref _lazyTranslatedImports, TranslateImports(context));
            }

            return _lazyTranslatedImports;
        }

        private ImmutableArray<Cci.UsedNamespaceOrType> TranslateImports(EmitContext context)
        {
            var usedNamespaces = ArrayBuilder<Cci.UsedNamespaceOrType>.GetInstance();

            // NOTE: order based on dev12: extern aliases, then usings, then aliases namespaces and types

            ImmutableArray<AliasAndExternAliasDirective> externAliases = Imports.ExternAliases;
            if (!externAliases.IsDefault)
            {
                foreach (var alias in externAliases)
                {
                    usedNamespaces.Add(Cci.UsedNamespaceOrType.CreateExternAlias(alias.Alias.Name));
                }
            }

            ImmutableArray<NamespaceOrTypeAndUsingDirective> usings = Imports.Usings;
            if (!usings.IsDefault)
            {
                foreach (var nsOrType in usings)
                {
                    NamespaceOrTypeSymbol namespaceOrType = nsOrType.NamespaceOrType;
                    if (namespaceOrType.IsNamespace)
                    {
                        var ns = (NamespaceSymbol)namespaceOrType;
                        var assemblyRef = TryGetAssemblyScope(context, ns);
                        usedNamespaces.Add(Cci.UsedNamespaceOrType.CreateNamespace(ns, assemblyRef));
                    }
                    else
                    {
                        var typeRef = GetTypeReference(context, (TypeSymbol)namespaceOrType);
                        usedNamespaces.Add(Cci.UsedNamespaceOrType.CreateType(typeRef));
                    }
                }
            }

            Dictionary<string, AliasAndUsingDirective> aliasSymbols = Imports.UsingAliases;
            if (aliasSymbols != null)
            {
                foreach (var pair in aliasSymbols)
                {
                    var alias = pair.Key;
                    var symbol = pair.Value.Alias;
                    Debug.Assert(!symbol.IsExtern);

                    NamespaceOrTypeSymbol target = symbol.Target;
                    if (target.Kind == SymbolKind.Namespace)
                    {
                        var ns = (NamespaceSymbol)target;
                        var assemblyRef = TryGetAssemblyScope(context, ns);
                        usedNamespaces.Add(Cci.UsedNamespaceOrType.CreateNamespace(ns, assemblyRef, alias));
                    }
                    else
                    {
                        var typeRef = GetTypeReference(context, (TypeSymbol)target);
                        usedNamespaces.Add(Cci.UsedNamespaceOrType.CreateType(typeRef, alias));
                    }
                }
            }

            return usedNamespaces.ToImmutableAndFree();
        }

        private static Cci.ITypeReference GetTypeReference(EmitContext context, TypeSymbol type)
        {
            return context.ModuleBuilder.Translate(type, context.SyntaxNodeOpt, context.Diagnostics);
        }

        private Cci.IAssemblyReference TryGetAssemblyScope(EmitContext context, NamespaceSymbol @namespace)
        {
            AssemblySymbol containingAssembly = @namespace.ContainingAssembly;
            if ((object)containingAssembly != null && (object)containingAssembly != context.ModuleBuilder.CommonCompilation.Assembly)
            {
                var referenceManager = ((CSharpCompilation)context.ModuleBuilder.CommonCompilation).GetBoundReferenceManager();

                foreach (var referencedAssembly in referenceManager.ReferencedAssembliesMap.Values)
                {
                    if ((object)referencedAssembly.Symbol == containingAssembly)
                    {
                        if (!referencedAssembly.DeclarationsAccessibleWithoutAlias())
                        {
                            return context.ModuleBuilder.Translate(containingAssembly, context.Diagnostics);
                        }
                    }
                }
            }

            return null;
        }

        Cci.IImportScope Cci.IImportScope.Parent => ParentOpt;
    }
}
