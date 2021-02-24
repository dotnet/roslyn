// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// A binder that represent scope introduced by 'using' directives and deals with looking up names in it.
    /// </summary>
    internal sealed class ImportsBinder : Binder
    {
        private readonly NamespaceOrTypeSymbol _container;
        private readonly Func<ConsList<TypeSymbol>, Imports> _computeImports;
        private Imports _lazyImports;
        private ImportChain _lazyImportChain;
        private QuickAttributeChecker _lazyQuickAttributeChecker;
        private readonly SyntaxList<UsingDirectiveSyntax> _usingsSyntax;

        /// <summary>
        /// Creates a binder imports (usings and extern aliases) within a <paramref name="container"/> that can be
        /// retrieved from <paramref name="declarationSyntax"/>.
        /// </summary>
        internal ImportsBinder(NamespaceOrTypeSymbol container, Binder next, CSharpSyntaxNode declarationSyntax, bool inUsing)
            : base(next)
        {
            Debug.Assert((object)container != null);
            Debug.Assert(declarationSyntax != null);

            _container = container;
            _computeImports = basesBeingResolved => Imports.FromSyntax(declarationSyntax, this, basesBeingResolved, inUsing);

            if (!inUsing)
            {
                if (declarationSyntax.Kind() == SyntaxKind.CompilationUnit)
                {
                    var compilationUnit = (CompilationUnitSyntax)declarationSyntax;
                    _usingsSyntax = compilationUnit.Usings;
                }
                else if (declarationSyntax.Kind() == SyntaxKind.NamespaceDeclaration)
                {
                    var namespaceDecl = (NamespaceDeclarationSyntax)declarationSyntax;
                    _usingsSyntax = namespaceDecl.Usings;
                }
            }
        }

        /// <summary>
        /// Creates a binder with given imports.
        /// </summary>
        internal ImportsBinder(NamespaceOrTypeSymbol container, Binder next, Imports imports = null)
            : base(next)
        {
            Debug.Assert((object)container != null || imports != null);

            _container = container;
            _lazyImports = imports ?? Imports.Empty;
        }

        /// <summary>
        /// Creates a binder with given import computation function.
        /// </summary>
        internal ImportsBinder(Binder next, Func<ConsList<TypeSymbol>, Imports> computeImports)
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

        private bool IsSubmissionClass
        {
            get { return (_container?.Kind == SymbolKind.NamedType) && ((NamedTypeSymbol)_container).IsSubmissionClass; }
        }

        internal override Imports GetImports(ConsList<TypeSymbol> basesBeingResolved)
        {
            Debug.Assert(_lazyImports != null || _computeImports != null, "Have neither imports nor a way to compute them.");

            if (_lazyImports == null)
            {
                Interlocked.CompareExchange(ref _lazyImports, _computeImports(basesBeingResolved), null);
            }

            return _lazyImports;
        }

        /// <summary>
        /// Look for a type forwarder for the given type in any referenced assemblies, checking any using namespaces in
        /// the current imports.
        /// </summary>
        /// <param name="name">The metadata name of the (potentially) forwarded type, without qualifiers.</param>
        /// <param name="qualifierOpt">Will be used to return the namespace of the found forwarder, 
        /// if any.</param>
        /// <param name="diagnostics">Will be used to report non-fatal errors during look up.</param>
        /// <param name="location">Location to report errors on.</param>
        /// <returns>Returns the Assembly to which the type is forwarded, or null if none is found.</returns>
        /// <remarks>
        /// Since this method is intended to be used for error reporting, it stops as soon as it finds
        /// any type forwarder (or an error to report). It does not check other assemblies for consistency or better results.
        /// </remarks>
        protected override AssemblySymbol GetForwardedToAssemblyInUsingNamespaces(string name, ref NamespaceOrTypeSymbol qualifierOpt, BindingDiagnosticBag diagnostics, Location location)
        {
            var imports = GetImports(basesBeingResolved: null);
            foreach (var typeOrNamespace in imports.Usings)
            {
                var fullName = typeOrNamespace.NamespaceOrType + "." + name;
                var result = GetForwardedToAssembly(fullName, diagnostics, location);
                if (result != null)
                {
                    qualifierOpt = typeOrNamespace.NamespaceOrType;
                    return result;
                }
            }

            return base.GetForwardedToAssemblyInUsingNamespaces(name, ref qualifierOpt, diagnostics, location);
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
                    QuickAttributeChecker result = this.Next.QuickAttributeChecker;

                    if ((object)_container == null || _container.Kind == SymbolKind.Namespace)
                    {
                        result = result.AddAliasesIfAny(_usingsSyntax);
                    }

                    _lazyQuickAttributeChecker = result;
                }

                return _lazyQuickAttributeChecker;
            }
        }

        internal override bool SupportsExtensionMethods
        {
            get { return true; }
        }

        internal override void GetCandidateExtensionMethods(
            ArrayBuilder<MethodSymbol> methods,
            string name,
            int arity,
            LookupOptions options,
            Binder originalBinder)
        {
            this.GetImports(basesBeingResolved: null).LookupExtensionMethodsInUsings(methods, name, arity, options, originalBinder);
        }

        internal override void LookupSymbolsInSingleBinder(
            LookupResult result, string name, int arity, ConsList<TypeSymbol> basesBeingResolved, LookupOptions options, Binder originalBinder, bool diagnose, ref CompoundUseSiteInfo<AssemblySymbol> useSiteInfo)
        {
            Debug.Assert(result.IsClear);

            var imports = GetImports(basesBeingResolved);

            // Try using aliases or symbols in imported namespaces
            imports.LookupSymbol(originalBinder, result, name, arity, basesBeingResolved, options, diagnose, ref useSiteInfo);
        }

        protected override void AddLookupSymbolsInfoInSingleBinder(LookupSymbolsInfo result, LookupOptions options, Binder originalBinder)
        {
            // If we are looking only for labels we do not need to search through the imports.
            // Submission imports are handled by AddMemberLookupSymbolsInfo in InContainerBinder.AddLookupSymbolsInfoInSingleBinder.
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

        internal override uint LocalScopeDepth => Binder.ExternalScope;
    }
}
