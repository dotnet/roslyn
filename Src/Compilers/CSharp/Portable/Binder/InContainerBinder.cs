// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// A binder that places the members of a symbol in scope.  If there is a container declaration
    /// with using directives, those are merged when looking up names.
    /// </summary>
    internal sealed class InContainerBinder : Binder
    {
        private readonly NamespaceOrTypeSymbol container;
        private readonly CSharpSyntaxNode declarationSyntax;
        private readonly bool allowStaticClassUsings;
        private Imports imports; // might be initialized lazily
        private ConsList<Imports> lazyImportsList;
        private readonly bool inUsing;

        /// <summary>
        /// Creates a binder for a container with imports (usings and extern aliases) that can be
        /// retrieved from <paramref name="declarationSyntax"/>.
        /// </summary>
        internal InContainerBinder(NamespaceOrTypeSymbol container, Binder next, CSharpSyntaxNode declarationSyntax, bool allowStaticClassUsings, bool inUsing)
            : base(next)
        {
            Debug.Assert((object)container != null);
            Debug.Assert(declarationSyntax != null);

            this.declarationSyntax = declarationSyntax;
            this.container = container;
            this.allowStaticClassUsings = allowStaticClassUsings;
            this.inUsing = inUsing;
        }

        /// <summary>
        /// Creates a binder with given imports.
        /// </summary>
        internal InContainerBinder(NamespaceOrTypeSymbol container, Binder next, Imports imports = null)
            : base(next)
        {
            Debug.Assert((object)container != null);

            this.container = container;
            this.imports = imports ?? Imports.Empty;
        }

        internal NamespaceOrTypeSymbol Container
        {
            get
            {
                return this.container;
            }
        }

        internal bool AllowStaticClassUsings
        {
            get
            {
                return this.allowStaticClassUsings;
            }
        }

        internal Imports GetImports()
        {
            return GetImports(basesBeingResolved: null);
        }

        private Imports GetImports(ConsList<Symbol> basesBeingResolved)
        {
            if (imports == null)
            {
                Interlocked.CompareExchange(ref imports, Imports.FromSyntax(declarationSyntax, this, basesBeingResolved, inUsing), null);
            }

            return imports;
        }

        internal override ConsList<Imports> ImportsList
        {
            get
            {
                if (lazyImportsList == null)
                {
                    ConsList<Imports> importsList = this.Next.ImportsList;
                    if (this.container.Kind == SymbolKind.Namespace)
                    {
                        importsList = new ConsList<Imports>(GetImports(), importsList);
                    }
                    Interlocked.CompareExchange(ref lazyImportsList, importsList, null);
                }

                Debug.Assert(lazyImportsList != null);

                return lazyImportsList;
            }
        }

        internal override Symbol ContainingMemberOrLambda
        {
            get
            {
                var merged = container as MergedNamespaceSymbol;
                return ((object)merged != null) ? merged.GetConstituentForCompilation(this.Compilation) : container;
            }
        }

        internal override bool IsAccessible(Symbol symbol, TypeSymbol accessThroughType, out bool failedThroughTypeCheck, ref HashSet<DiagnosticInfo> useSiteDiagnostics, ConsList<Symbol> basesBeingResolved = null)
        {
            var type = container as NamedTypeSymbol;
            if ((object)type != null)
            {
                return this.IsSymbolAccessibleConditional(symbol, type, accessThroughType, out failedThroughTypeCheck, ref useSiteDiagnostics);
            }
            else
            {
                return Next.IsAccessible(symbol, accessThroughType, out failedThroughTypeCheck, ref useSiteDiagnostics, basesBeingResolved);  // delegate to containing Binder, eventually checking assembly.
            }
        }

        internal override bool SupportsExtensionMethods
        {
            get { return true; }
        }

        internal override void GetCandidateExtensionMethods(
            bool searchUsingsNotNamespace,
            ArrayBuilder<MethodSymbol> methods,
            string name,
            int arity,
            LookupOptions options,
            bool isCallerSemanticModel)
        {
            if (searchUsingsNotNamespace)
            {
                this.GetImports().LookupExtensionMethodsInUsings(methods, name, arity, options, isCallerSemanticModel);
            }
            else
            {
                if (container.Kind == SymbolKind.Namespace)
                {
                    ((NamespaceSymbol)container).GetExtensionMethods(methods, name, arity, options);
                }
            }
        }

        protected override void LookupSymbolsInSingleBinder(
            LookupResult result, string name, int arity, ConsList<Symbol> basesBeingResolved, LookupOptions options, Binder originalBinder, bool diagnose, ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            Debug.Assert(result.IsClear);

            if (container.IsSubmissionClass)
            {
                this.LookupMembersInternal(result, container, name, arity, basesBeingResolved, options, originalBinder, diagnose, ref useSiteDiagnostics);
                return;
            }

            var imports = GetImports(basesBeingResolved);

            // first lookup members of the namespace
            if ((options & LookupOptions.NamespaceAliasesOnly) == 0)
            {
                this.LookupMembersInternal(result, container, name, arity, basesBeingResolved, options, originalBinder, diagnose, ref useSiteDiagnostics);

                if (result.IsMultiViable)
                {
                    // symbols cannot conflict with using alias names
                    if (arity == 0 && imports.IsUsingAlias(name, originalBinder.IsSemanticModelBinder))
                    {
                        CSDiagnosticInfo diagInfo = new CSDiagnosticInfo(ErrorCode.ERR_ConflictAliasAndMember, name, container);
                        var error = new ExtendedErrorTypeSymbol((NamespaceOrTypeSymbol)null, name, arity, diagInfo, unreported: true);
                        result.SetFrom(LookupResult.Good(error)); // force lookup to be done w/ error symbol as result
                    }

                    return;
                }
            }

            // next try using aliases or symbols in imported namespaces
            imports.LookupSymbol(originalBinder, result, name, arity, basesBeingResolved, options, diagnose, ref useSiteDiagnostics);
        }

        protected override void AddLookupSymbolsInfoInSingleBinder(LookupSymbolsInfo result, LookupOptions options, Binder originalBinder)
        {
            this.AddMemberLookupSymbolsInfo(result, container, options, originalBinder);

            // if we are looking only for labels we do not need to search through the imports
            if (!container.IsSubmissionClass && ((options & LookupOptions.LabelsOnly) == 0))
            {
                var imports = GetImports(basesBeingResolved: null);

                imports.AddLookupSymbolsInfoInAliases(this, result, options);

                // Add types within namespaces imported through usings, but don't add nested namespaces.
                LookupOptions usingOptions = (options & ~(LookupOptions.NamespaceAliasesOnly | LookupOptions.NamespacesOrTypesOnly)) | LookupOptions.MustNotBeNamespace;
                Imports.AddLookupSymbolsInfoInUsings(imports.Usings, this, result, usingOptions);
            }
        }

        protected override SourceLocalSymbol LookupLocal(SyntaxToken nameToken)
        {
            return null;
        }
    }
}