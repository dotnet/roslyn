// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Symbols;
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
        private readonly CSharpSyntaxNode _declarationSyntax;
        private readonly bool _allowStaticClassUsings;
        private Imports _imports; // might be initialized lazily
        private ImportChain _lazyImportChain;
        private readonly bool _inUsing;

        /// <summary>
        /// Creates a binder for a container with imports (usings and extern aliases) that can be
        /// retrieved from <paramref name="declarationSyntax"/>.
        /// </summary>
        internal InContainerBinder(NamespaceOrTypeSymbol container, Binder next, CSharpSyntaxNode declarationSyntax, bool allowStaticClassUsings, bool inUsing)
            : base(next)
        {
            Debug.Assert((object)container != null);
            Debug.Assert(declarationSyntax != null);

            _declarationSyntax = declarationSyntax;
            _container = container;
            _allowStaticClassUsings = allowStaticClassUsings;
            _inUsing = inUsing;
        }

        /// <summary>
        /// Creates a binder with given imports.
        /// </summary>
        internal InContainerBinder(NamespaceOrTypeSymbol container, Binder next, Imports imports = null)
            : base(next)
        {
            Debug.Assert((object)container != null);

            _container = container;
            _imports = imports ?? Imports.Empty;
        }

        internal NamespaceOrTypeSymbol Container
        {
            get
            {
                return _container;
            }
        }

        internal bool AllowStaticClassUsings
        {
            get
            {
                return _allowStaticClassUsings;
            }
        }

        internal Imports GetImports()
        {
            return GetImports(basesBeingResolved: null);
        }

        private Imports GetImports(ConsList<Symbol> basesBeingResolved)
        {
            if (_imports == null)
            {
                Interlocked.CompareExchange(ref _imports, Imports.FromSyntax(_declarationSyntax, this, basesBeingResolved, _inUsing), null);
            }

            return _imports;
        }

        internal override ImportChain ImportChain
        {
            get
            {
                if (_lazyImportChain == null)
                {
                    ImportChain importChain = this.Next.ImportChain;
                    if (_container.Kind == SymbolKind.Namespace)
                    {
                        importChain = new ImportChain(GetImports(), importChain);
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

        internal bool IsSubmissionClass
        {
            get { return (_container.Kind == SymbolKind.NamedType) && ((NamedTypeSymbol)_container).IsSubmissionClass; }
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
            bool isCallerSemanticModel)
        {
            if (searchUsingsNotNamespace)
            {
                this.GetImports().LookupExtensionMethodsInUsings(methods, name, arity, options, isCallerSemanticModel);
            }
            else
            {
                if (_container.Kind == SymbolKind.Namespace)
                {
                    ((NamespaceSymbol)_container).GetExtensionMethods(methods, name, arity, options);
                }
                else if (((NamedTypeSymbol)_container).IsScriptClass)
                {
                    for (var submission = this.Compilation; submission != null; submission = submission.PreviousSubmission)
                    {
                        var scriptClass = submission.ScriptClass;
                        if ((object)scriptClass != null)
                        {
                            scriptClass.GetExtensionMethods(methods, name, arity, options);
                        }
                    }
                }
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
            if ((options & LookupOptions.NamespaceAliasesOnly) == 0)
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
            this.AddMemberLookupSymbolsInfo(result, _container, options, originalBinder);

            // if we are looking only for labels we do not need to search through the imports
            if (!IsSubmissionClass && ((options & LookupOptions.LabelsOnly) == 0))
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
