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
    /// A binder that brings extern aliases into the scope and deals with looking up names in them.
    /// </summary>
    internal abstract class WithExternAliasesBinder : Binder
    {
        internal WithExternAliasesBinder(Binder next)
            : base(next)
        {
        }

        internal abstract override ImmutableArray<AliasAndExternAliasDirective> ExternAliases
        {
            get;
        }

        internal override void LookupSymbolsInSingleBinder(
            LookupResult result, string name, int arity, ConsList<TypeSymbol> basesBeingResolved, LookupOptions options, Binder originalBinder, bool diagnose, ref CompoundUseSiteInfo<AssemblySymbol> useSiteInfo)
        {
            Debug.Assert(result.IsClear);

            LookupSymbolInAliases(
                ImmutableDictionary<string, AliasAndUsingDirective>.Empty,
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
                    ImmutableDictionary<string, AliasAndUsingDirective>.Empty,
                    this.ExternAliases,
                    result, options, originalBinder);
            }
        }

        protected sealed override SourceLocalSymbol? LookupLocal(SyntaxToken nameToken)
        {
            return null;
        }

        protected sealed override LocalFunctionSymbol? LookupLocalFunction(SyntaxToken nameToken)
        {
            return null;
        }

        internal sealed override uint LocalScopeDepth => Binder.ExternalScope;

        internal static WithExternAliasesBinder Create(SourceNamespaceSymbol declaringSymbol, CSharpSyntaxNode declarationSyntax, Binder next)
        {
            return new FromSyntax(declaringSymbol, declarationSyntax, next);
        }

        internal static WithExternAliasesBinder Create(ImmutableArray<AliasAndExternAliasDirective> externAliases, Binder next)
        {
            return new FromSymbols(externAliases, next);
        }

        private sealed class FromSyntax : WithExternAliasesBinder
        {
            private readonly SourceNamespaceSymbol _declaringSymbol;
            private readonly CSharpSyntaxNode _declarationSyntax;
            private ImmutableArray<AliasAndExternAliasDirective> _lazyExternAliases;

            internal FromSyntax(SourceNamespaceSymbol declaringSymbol, CSharpSyntaxNode declarationSyntax, Binder next)
                : base(next)
            {
                Debug.Assert(declarationSyntax.Kind() is SyntaxKind.CompilationUnit or SyntaxKind.NamespaceDeclaration or SyntaxKind.FileScopedNamespaceDeclaration);
                _declaringSymbol = declaringSymbol;
                _declarationSyntax = declarationSyntax;
            }

            internal override ImmutableArray<AliasAndExternAliasDirective> ExternAliases
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
        }

        private sealed class FromSymbols : WithExternAliasesBinder
        {
            private readonly ImmutableArray<AliasAndExternAliasDirective> _externAliases;

            internal FromSymbols(ImmutableArray<AliasAndExternAliasDirective> externAliases, Binder next)
                : base(next)
            {
                Debug.Assert(!externAliases.IsDefault);
                _externAliases = externAliases;
            }

            internal override ImmutableArray<AliasAndExternAliasDirective> ExternAliases
            {
                get
                {
                    return _externAliases;
                }
            }
        }
    }
}
