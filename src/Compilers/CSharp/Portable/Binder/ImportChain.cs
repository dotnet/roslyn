// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;

namespace Microsoft.CodeAnalysis.CSharp
{
    [DebuggerDisplay("{GetDebuggerDisplay(),nq}")]
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

        private string GetDebuggerDisplay()
        {
            return $"{Imports.GetDebuggerDisplay()} ^ {ParentOpt?.GetHashCode() ?? 0}";
        }

        ImmutableArray<Cci.UsedNamespaceOrType> Cci.IImportScope.GetUsedNamespaces()
        {
            // The imports should have been translated during code gen.
            Debug.Assert(!_lazyTranslatedImports.IsDefault);
            return _lazyTranslatedImports;
        }

        public Cci.IImportScope Translate(Emit.PEModuleBuilder moduleBuilder, DiagnosticBag diagnostics)
        {
            for (var scope = this; scope != null; scope = scope.ParentOpt)
            {
                if (!scope._lazyTranslatedImports.IsDefault)
                {
                    break;
                }

                ImmutableInterlocked.InterlockedInitialize(ref scope._lazyTranslatedImports, scope.TranslateImports(moduleBuilder, diagnostics));
            }

            return this;
        }

        private ImmutableArray<Cci.UsedNamespaceOrType> TranslateImports(Emit.PEModuleBuilder moduleBuilder, DiagnosticBag diagnostics)
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
                        var assemblyRef = TryGetAssemblyScope(ns, moduleBuilder, diagnostics);
                        usedNamespaces.Add(Cci.UsedNamespaceOrType.CreateNamespace(ns, assemblyRef));
                    }
                    else if (!namespaceOrType.ContainingAssembly.IsLinked)
                    {
                        // We skip alias imports of embedded types to be consistent with imports of aliased embedded types and with VB.
                        var typeRef = GetTypeReference((TypeSymbol)namespaceOrType, nsOrType.UsingDirective, moduleBuilder, diagnostics);
                        usedNamespaces.Add(Cci.UsedNamespaceOrType.CreateType(typeRef));
                    }
                }
            }

            ImmutableDictionary<string, AliasAndUsingDirective> aliasSymbols = Imports.UsingAliases;
            if (!aliasSymbols.IsEmpty)
            {
                var aliases = ArrayBuilder<string>.GetInstance(aliasSymbols.Count);
                aliases.AddRange(aliasSymbols.Keys);
                aliases.Sort(StringComparer.Ordinal); // Actual order doesn't matter - just want to be deterministic.

                foreach (var alias in aliases)
                {
                    var aliasAndUsingDirective = aliasSymbols[alias];
                    var symbol = aliasAndUsingDirective.Alias;
                    var syntax = aliasAndUsingDirective.UsingDirective;
                    Debug.Assert(!symbol.IsExtern);

                    NamespaceOrTypeSymbol target = symbol.Target;
                    if (target.Kind == SymbolKind.Namespace)
                    {
                        var ns = (NamespaceSymbol)target;
                        var assemblyRef = TryGetAssemblyScope(ns, moduleBuilder, diagnostics);
                        usedNamespaces.Add(Cci.UsedNamespaceOrType.CreateNamespace(ns, assemblyRef, alias));
                    }
                    else if (!target.ContainingAssembly.IsLinked)
                    {
                        // We skip alias imports of embedded types to avoid breaking existing code that
                        // imports types that can't be embedded but doesn't use them anywhere else in the code.
                        var typeRef = GetTypeReference((TypeSymbol)target, syntax, moduleBuilder, diagnostics);
                        usedNamespaces.Add(Cci.UsedNamespaceOrType.CreateType(typeRef, alias));
                    }
                }

                aliases.Free();
            }

            return usedNamespaces.ToImmutableAndFree();
        }

        private static Cci.ITypeReference GetTypeReference(TypeSymbol type, SyntaxNode syntaxNode, Emit.PEModuleBuilder moduleBuilder, DiagnosticBag diagnostics)
        {
            return moduleBuilder.Translate(type, syntaxNode, diagnostics);
        }

        private Cci.IAssemblyReference TryGetAssemblyScope(NamespaceSymbol @namespace, Emit.PEModuleBuilder moduleBuilder, DiagnosticBag diagnostics)
        {
            AssemblySymbol containingAssembly = @namespace.ContainingAssembly;
            if ((object)containingAssembly != null && (object)containingAssembly != moduleBuilder.CommonCompilation.Assembly)
            {
                var referenceManager = ((CSharpCompilation)moduleBuilder.CommonCompilation).GetBoundReferenceManager();

                for (int i = 0; i < referenceManager.ReferencedAssemblies.Length; i++)
                {
                    if ((object)referenceManager.ReferencedAssemblies[i] == containingAssembly)
                    {
                        if (!referenceManager.DeclarationsAccessibleWithoutAlias(i))
                        {
                            return moduleBuilder.Translate(containingAssembly, diagnostics);
                        }
                    }
                }
            }

            return null;
        }

        Cci.IImportScope Cci.IImportScope.Parent => ParentOpt;
    }
}
