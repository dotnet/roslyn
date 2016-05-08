// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// A binder that places the members of a symbol in scope.  If there is a container declaration
    /// with using directives, those are merged when looking up names.
    /// </summary>
    internal sealed class InContainerBinder : Binder
    {
        private readonly NamespaceOrTypeSymbol _container;
        private readonly Func<ConsList<Symbol>, Imports> _computeImports;
        private Imports _lazyImports;
        private ImportChain _lazyImportChain;

        /// <summary>
        /// Creates a binder for a container with imports (usings and extern aliases) that can be
        /// retrieved from <paramref name="declarationSyntax"/>.
        /// </summary>
        internal InContainerBinder(NamespaceOrTypeSymbol container, Binder next, CSharpSyntaxNode declarationSyntax, bool inUsing)
            : base(next)
        {
            Debug.Assert((object)container != null);
            Debug.Assert(declarationSyntax != null);

            _container = container;
            _computeImports = basesBeingResolved => Imports.FromSyntax(declarationSyntax, this, basesBeingResolved, inUsing);
        }

        /// <summary>
        /// Creates a binder with given imports.
        /// </summary>
        internal InContainerBinder(NamespaceOrTypeSymbol container, Binder next, Imports imports = null)
            : base(next)
        {
            Debug.Assert((object)container != null || imports != null);

            _container = container;
            _lazyImports = imports ?? Imports.Empty;
        }

        /// <summary>
        /// Creates a binder with given import computation function.
        /// </summary>
        internal InContainerBinder(Binder next, Func<ConsList<Symbol>, Imports> computeImports)
            : base(next)
        {
            Debug.Assert(computeImports != null);

            _container = null;
            _computeImports = computeImports;
        }

        internal NamespaceOrTypeSymbol Container
        {
            get
            {
                return _container;
            }
        }

        internal override Imports GetImports(ConsList<Symbol> basesBeingResolved)
        {
            Debug.Assert(_lazyImports != null || _computeImports != null, "Have neither imports nor a way to compute them.");

            if (_lazyImports == null)
            {
                Interlocked.CompareExchange(ref _lazyImports, _computeImports(basesBeingResolved), null);
            }

            return _lazyImports;
        }

        internal override ImportChain ImportChain
        {
            get
            {
                if (_lazyImportChain == null)
                {
                    ImportChain importChain = this.Next.ImportChain;
                    if ((object)_container == null || _container.Kind == SymbolKind.Namespace)
                    {
                        importChain = new ImportChain(GetImports(basesBeingResolved: null), importChain);
                    }

                    Interlocked.CompareExchange(ref _lazyImportChain, importChain, null);
                }

                Debug.Assert(_lazyImportChain != null);

                return _lazyImportChain;
            }
        }

        internal override Symbol ContainingMemberOrLambda
        {
            get
            {
                var merged = _container as MergedNamespaceSymbol;
                return ((object)merged != null) ? merged.GetConstituentForCompilation(this.Compilation) : _container;
            }
        }

        private bool IsSubmissionClass
        {
            get { return (_container?.Kind == SymbolKind.NamedType) && ((NamedTypeSymbol)_container).IsSubmissionClass; }
        }

        private bool IsScriptClass
        {
            get { return (_container?.Kind == SymbolKind.NamedType) && ((NamedTypeSymbol)_container).IsScriptClass; }
        }

        internal override bool IsAccessibleHelper(Symbol symbol, TypeSymbol accessThroughType, out bool failedThroughTypeCheck, ref HashSet<DiagnosticInfo> useSiteDiagnostics, ConsList<Symbol> basesBeingResolved)
        {
            var type = _container as NamedTypeSymbol;
            if ((object)type != null)
            {
                return this.IsSymbolAccessibleConditional(symbol, type, accessThroughType, out failedThroughTypeCheck, ref useSiteDiagnostics);
            }
            else
            {
                return Next.IsAccessibleHelper(symbol, accessThroughType, out failedThroughTypeCheck, ref useSiteDiagnostics, basesBeingResolved);  // delegate to containing Binder, eventually checking assembly.
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
            Binder originalBinder)
        {
            if (searchUsingsNotNamespace)
            {
                this.GetImports(basesBeingResolved: null).LookupExtensionMethodsInUsings(methods, name, arity, options, originalBinder);
            }
            else if (_container?.Kind == SymbolKind.Namespace)
            {
                ((NamespaceSymbol)_container).GetExtensionMethods(methods, name, arity, options);
            }
            else if (IsSubmissionClass)
            {
                for (var submission = this.Compilation; submission != null; submission = submission.PreviousSubmission)
                {
                    submission.ScriptClass?.GetExtensionMethods(methods, name, arity, options);
                }
            }
        }

        internal override TypeSymbol GetIteratorElementType(YieldStatementSyntax node, DiagnosticBag diagnostics)
        {
            if (IsScriptClass)
            {
                // This is the scenario where a `yield return` exists in the script file as a global statement.
                // This method is to guard against hitting `BuckStopsHereBinder` and crash. 
                return this.Compilation.GetSpecialType(SpecialType.System_Object);
            }
            else
            {
                // This path would eventually throw, if we didn't have the case above.
                return Next.GetIteratorElementType(node, diagnostics);
            }
        }

        internal override void LookupSymbolsInSingleBinder(
            LookupResult result, string name, int arity, ConsList<Symbol> basesBeingResolved, LookupOptions options, Binder originalBinder, bool diagnose, ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            Debug.Assert(result.IsClear);

            if (IsSubmissionClass)
            {
                this.LookupMembersInternal(result, _container, name, arity, basesBeingResolved, options, originalBinder, diagnose, ref useSiteDiagnostics);
                return;
            }

            var imports = GetImports(basesBeingResolved);

            // first lookup members of the namespace
            if ((options & LookupOptions.NamespaceAliasesOnly) == 0 && _container != null)
            {
                this.LookupMembersInternal(result, _container, name, arity, basesBeingResolved, options, originalBinder, diagnose, ref useSiteDiagnostics);

                if (result.IsMultiViable)
                {
                    // symbols cannot conflict with using alias names
                    if (arity == 0 && imports.IsUsingAlias(name, originalBinder.IsSemanticModelBinder))
                    {
                        CSDiagnosticInfo diagInfo = new CSDiagnosticInfo(ErrorCode.ERR_ConflictAliasAndMember, name, _container);
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
            if (_container != null)
            {
                this.AddMemberLookupSymbolsInfo(result, _container, options, originalBinder);
            }

            // If we are looking only for labels we do not need to search through the imports.
            // Submission imports are handled by AddMemberLookupSymbolsInfo (above).
            if (!IsSubmissionClass && ((options & LookupOptions.LabelsOnly) == 0))
            {
                var imports = GetImports(basesBeingResolved: null);
                imports.AddLookupSymbolsInfo(result, options, originalBinder);
            }
        }

        protected override SourceLocalSymbol LookupLocal(SyntaxToken nameToken)
        {
            return null;
        }

        protected override LocalFunctionSymbol LookupLocalFunction(SyntaxToken nameToken)
        {
            return null;
        }
    }
}
