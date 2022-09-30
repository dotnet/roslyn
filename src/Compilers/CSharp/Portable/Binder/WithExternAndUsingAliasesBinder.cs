// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// A binder that brings both extern and using aliases into the scope and deals with looking up names in them.
    /// </summary>
    internal abstract class WithExternAndUsingAliasesBinder : WithExternAliasesBinder
    {
        private ImportChain? _lazyImportChain;

        protected WithExternAndUsingAliasesBinder(WithUsingNamespacesAndTypesBinder next)
            : base(next)
        {
#if DEBUG
            Debug.Assert(!next.WithImportChainEntry);
#endif 
        }

        internal abstract override ImmutableArray<AliasAndUsingDirective> UsingAliases { get; }

        protected abstract ImmutableDictionary<string, AliasAndUsingDirective> GetUsingAliasesMap(ConsList<TypeSymbol>? basesBeingResolved);

        internal override void LookupSymbolsInSingleBinder(
            LookupResult result, string name, int arity, ConsList<TypeSymbol> basesBeingResolved, LookupOptions options, Binder originalBinder, bool diagnose, ref CompoundUseSiteInfo<AssemblySymbol> useSiteInfo)
        {
            Debug.Assert(result.IsClear);

            LookupSymbolInAliases(
                this.GetUsingAliasesMap(basesBeingResolved),
                this.ExternAliases,
                originalBinder,
                result,
                name,
                arity,
                basesBeingResolved,
                options,
                diagnose,
                ref useSiteInfo);
        }

        internal override void AddLookupSymbolsInfoInSingleBinder(LookupSymbolsInfo result, LookupOptions options, Binder originalBinder)
        {
            // If we are looking only for labels we do not need to search through the imports.
            if ((options & LookupOptions.LabelsOnly) == 0)
            {
                AddLookupSymbolsInfoInAliases(
                    this.GetUsingAliasesMap(basesBeingResolved: null),
                    this.ExternAliases,
                    result, options, originalBinder);
            }
        }

        internal override ImportChain ImportChain
        {
            get
            {
                if (_lazyImportChain == null)
                {
                    Debug.Assert(this.Next is WithUsingNamespacesAndTypesBinder);
                    Interlocked.CompareExchange(ref _lazyImportChain, BuildImportChain(), null);
                }

                Debug.Assert(_lazyImportChain != null);

                return _lazyImportChain;
            }
        }

        protected abstract ImportChain BuildImportChain();

        internal bool IsUsingAlias(string name, bool callerIsSemanticModel, ConsList<TypeSymbol>? basesBeingResolved)
        {
            return IsUsingAlias(this.GetUsingAliasesMap(basesBeingResolved), name, callerIsSemanticModel);
        }

        /// <summary>
        /// This overload is added to shadow the one from the base.
        /// </summary>
        [Obsolete("Use other overloads", error: true)]
        internal static new WithExternAndUsingAliasesBinder Create(SourceNamespaceSymbol declaringSymbol, CSharpSyntaxNode declarationSyntax, Binder next)
        {
            throw ExceptionUtilities.Unreachable;
        }

        internal static WithExternAndUsingAliasesBinder Create(SourceNamespaceSymbol declaringSymbol, CSharpSyntaxNode declarationSyntax, WithUsingNamespacesAndTypesBinder next)
        {
            return new FromSyntax(declaringSymbol, declarationSyntax, next);
        }

        internal static WithExternAndUsingAliasesBinder Create(ImmutableArray<AliasAndExternAliasDirective> externAliases, ImmutableDictionary<string, AliasAndUsingDirective> usingAliases, WithUsingNamespacesAndTypesBinder next)
        {
            return new FromSymbols(externAliases, usingAliases, next);
        }

        private sealed class FromSyntax : WithExternAndUsingAliasesBinder
        {
            private readonly SourceNamespaceSymbol _declaringSymbol;
            private readonly CSharpSyntaxNode _declarationSyntax;
            private ImmutableArray<AliasAndExternAliasDirective> _lazyExternAliases;
            private ImmutableArray<AliasAndUsingDirective> _lazyUsingAliases;
            private ImmutableDictionary<string, AliasAndUsingDirective>? _lazyUsingAliasesMap;
            private QuickAttributeChecker? _lazyQuickAttributeChecker;

            internal FromSyntax(SourceNamespaceSymbol declaringSymbol, CSharpSyntaxNode declarationSyntax, WithUsingNamespacesAndTypesBinder next)
                : base(next)
            {
                Debug.Assert(declarationSyntax.Kind() is SyntaxKind.CompilationUnit or SyntaxKind.NamespaceDeclaration or SyntaxKind.FileScopedNamespaceDeclaration);
                _declaringSymbol = declaringSymbol;
                _declarationSyntax = declarationSyntax;
            }

            internal sealed override ImmutableArray<AliasAndExternAliasDirective> ExternAliases
            {
                get
                {
                    if (_lazyExternAliases.IsDefault)
                    {
                        ImmutableInterlocked.InterlockedInitialize(ref _lazyExternAliases, _declaringSymbol.GetExternAliases(_declarationSyntax));
                    }

                    return _lazyExternAliases;
                }
            }

            internal override ImmutableArray<AliasAndUsingDirective> UsingAliases
            {
                get
                {
                    if (_lazyUsingAliases.IsDefault)
                    {
                        ImmutableInterlocked.InterlockedInitialize(ref _lazyUsingAliases, _declaringSymbol.GetUsingAliases(_declarationSyntax, basesBeingResolved: null));
                    }

                    return _lazyUsingAliases;
                }
            }

            protected override ImmutableDictionary<string, AliasAndUsingDirective> GetUsingAliasesMap(ConsList<TypeSymbol>? basesBeingResolved)
            {
                if (_lazyUsingAliasesMap is null)
                {
                    Interlocked.CompareExchange(ref _lazyUsingAliasesMap, _declaringSymbol.GetUsingAliasesMap(_declarationSyntax, basesBeingResolved), null);
                }

                return _lazyUsingAliasesMap;
            }

            /// <summary>
            /// Get <see cref="QuickAttributeChecker"/> that can be used to quickly
            /// check for certain attribute applications in context of this binder.
            /// </summary>
            internal override QuickAttributeChecker QuickAttributeChecker
            {
                get
                {
                    if (_lazyQuickAttributeChecker == null)
                    {
                        QuickAttributeChecker result = this.Next!.QuickAttributeChecker;

                        SyntaxList<UsingDirectiveSyntax> usingDirectives;
                        switch (_declarationSyntax)
                        {
                            case CompilationUnitSyntax compilationUnit:
                                // Take global aliases from other compilation units into account
                                foreach (var declaration in ((SourceNamespaceSymbol)Compilation.SourceModule.GlobalNamespace).MergedDeclaration.Declarations)
                                {
                                    if (declaration.HasGlobalUsings && compilationUnit.SyntaxTree != declaration.SyntaxReference.SyntaxTree)
                                    {
                                        result = result.AddAliasesIfAny(((CompilationUnitSyntax)declaration.SyntaxReference.GetSyntax()).Usings, onlyGlobalAliases: true);
                                    }
                                }

                                usingDirectives = compilationUnit.Usings;
                                break;

                            case BaseNamespaceDeclarationSyntax namespaceDecl:
                                usingDirectives = namespaceDecl.Usings;
                                break;

                            default:
                                throw ExceptionUtilities.UnexpectedValue(_declarationSyntax);
                        }

                        result = result.AddAliasesIfAny(usingDirectives);

                        _lazyQuickAttributeChecker = result;
                    }

                    return _lazyQuickAttributeChecker;
                }
            }

            protected override ImportChain BuildImportChain()
            {
                var previous = Next!.ImportChain;

                if (_declarationSyntax is BaseNamespaceDeclarationSyntax namespaceDecl)
                {
                    // For each dotted name add an empty entry in the chain
                    var name = namespaceDecl.Name;

                    while (name is QualifiedNameSyntax dotted)
                    {
                        previous = new ImportChain(Imports.Empty, previous);
                        name = dotted.Left;
                    }
                }

                return new ImportChain(_declaringSymbol.GetImports(_declarationSyntax, basesBeingResolved: null), previous);
            }
        }

        private sealed class FromSymbols : WithExternAndUsingAliasesBinder
        {
            private readonly ImmutableArray<AliasAndExternAliasDirective> _externAliases;
            private readonly ImmutableDictionary<string, AliasAndUsingDirective> _usingAliases;

            internal FromSymbols(ImmutableArray<AliasAndExternAliasDirective> externAliases, ImmutableDictionary<string, AliasAndUsingDirective> usingAliases, WithUsingNamespacesAndTypesBinder next)
                : base(next)
            {
                Debug.Assert(!externAliases.IsDefault);
                _externAliases = externAliases;
                _usingAliases = usingAliases;
            }

            internal override ImmutableArray<AliasAndExternAliasDirective> ExternAliases
            {
                get
                {
                    return _externAliases;
                }
            }

            internal override ImmutableArray<AliasAndUsingDirective> UsingAliases
            {
                get
                {
                    return _usingAliases.SelectAsArray(static pair => pair.Value);
                }
            }

            protected override ImmutableDictionary<string, AliasAndUsingDirective> GetUsingAliasesMap(ConsList<TypeSymbol>? basesBeingResolved)
            {
                return _usingAliases;
            }

            protected override ImportChain BuildImportChain()
            {
                return new ImportChain(Imports.Create(_usingAliases, ((WithUsingNamespacesAndTypesBinder)Next!).GetUsings(basesBeingResolved: null), _externAliases), Next!.ImportChain);
            }
        }
    }
}
