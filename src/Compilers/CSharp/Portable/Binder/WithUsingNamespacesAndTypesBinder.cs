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
    /// A binder that represents a scope introduced by 'using' namespace or type directives and deals with looking up names in it.
    /// </summary>
    internal abstract class WithUsingNamespacesAndTypesBinder : Binder
    {
        private readonly bool _withImportChainEntry;
        private ImportChain? _lazyImportChain;

        protected WithUsingNamespacesAndTypesBinder(Binder next, bool withImportChainEntry)
            : base(next)
        {
            _withImportChainEntry = withImportChainEntry;
        }

#if DEBUG
        internal bool WithImportChainEntry => _withImportChainEntry;
#endif

        internal abstract ImmutableArray<NamespaceOrTypeAndUsingDirective> GetUsings(ConsList<TypeSymbol>? basesBeingResolved);

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
        protected override AssemblySymbol? GetForwardedToAssemblyInUsingNamespaces(string name, ref NamespaceOrTypeSymbol qualifierOpt, BindingDiagnosticBag diagnostics, Location location)
        {
            foreach (var typeOrNamespace in GetUsings(basesBeingResolved: null))
            {
                var result = GetForwardedToAssembly(
                    MetadataTypeName.FromNamespaceAndTypeName(typeOrNamespace.NamespaceOrType.ToString(), name),
                    diagnostics,
                    location);
                if (result != null)
                {
                    qualifierOpt = typeOrNamespace.NamespaceOrType;
                    return result;
                }
            }

            return base.GetForwardedToAssemblyInUsingNamespaces(name, ref qualifierOpt, diagnostics, location);
        }

        internal override bool SupportsExtensions
        {
            get { return true; }
        }

        internal override void GetCandidateExtensionMethodsInSingleBinder(
            ArrayBuilder<MethodSymbol> methods,
            string name,
            int arity,
            LookupOptions options,
            Binder originalBinder)
        {
            Debug.Assert(methods.Count == 0);

            bool callerIsSemanticModel = originalBinder.IsSemanticModelBinder;

            // We need to avoid collecting multiple candidates for an extension method imported both through a namespace and a static class
            // We will look for duplicates only if both of the following flags are set to true
            bool seenNamespaceWithExtensionMethods = false;
            bool seenStaticClassWithExtensionMethods = false;

            foreach (var nsOrType in this.GetUsings(basesBeingResolved: null))
            {
                switch (nsOrType.NamespaceOrType.Kind)
                {
                    case SymbolKind.Namespace:
                        {
                            var count = methods.Count;
                            ((NamespaceSymbol)nsOrType.NamespaceOrType).GetExtensionMethods(methods, name, arity, options);

                            // If we found any extension methods, then consider this using as used.
                            if (methods.Count != count)
                            {
                                MarkImportDirective(nsOrType.UsingDirectiveReference, callerIsSemanticModel);
                                seenNamespaceWithExtensionMethods = true;
                            }

                            break;
                        }

                    case SymbolKind.NamedType:
                        {
                            var count = methods.Count;
                            ((NamedTypeSymbol)nsOrType.NamespaceOrType).GetExtensionMethods(methods, name, arity, options);

                            // If we found any extension methods, then consider this using as used.
                            if (methods.Count != count)
                            {
                                MarkImportDirective(nsOrType.UsingDirectiveReference, callerIsSemanticModel);
                                seenStaticClassWithExtensionMethods = true;
                            }

                            break;
                        }
                }
            }

            if (seenNamespaceWithExtensionMethods && seenStaticClassWithExtensionMethods)
            {
                methods.RemoveDuplicates();
            }
        }

        internal override void GetCandidateExtensionMembersInSingleBinder(ArrayBuilder<Symbol> members, string? name, string? alternativeName, int arity, LookupOptions options, Binder originalBinder)
        {
            Debug.Assert(members.Count == 0);

            bool callerIsSemanticModel = originalBinder.IsSemanticModelBinder;

            // We need to avoid collecting multiple candidates for an extension declaration imported both through a namespace and a static class
            // We will look for duplicates only if both of the following flags are set to true
            bool seenNamespaceWithExtensions = false;
            bool seenStaticClassWithExtensions = false;

            foreach (var nsOrType in this.GetUsings(basesBeingResolved: null))
            {
                if (nsOrType.NamespaceOrType is NamespaceSymbol ns)
                {
                    var count = members.Count;
                    ns.GetExtensionMembers(members, name, alternativeName, arity, options, originalBinder.FieldsBeingBound);
                    // If we found any extension declarations, then consider this using as used.
                    if (members.Count != count)
                    {
                        MarkImportDirective(nsOrType.UsingDirectiveReference, callerIsSemanticModel);
                        seenNamespaceWithExtensions = true;
                    }
                }
                else if (nsOrType.NamespaceOrType is NamedTypeSymbol namedType)
                {
                    var count = members.Count;
                    namedType.GetExtensionMembers(members, name, alternativeName, arity, options, originalBinder.FieldsBeingBound);
                    // If we found any extension declarations, then consider this using as used.
                    if (members.Count != count)
                    {
                        MarkImportDirective(nsOrType.UsingDirectiveReference, callerIsSemanticModel);
                        seenStaticClassWithExtensions = true;
                    }
                }
            }

            if (seenNamespaceWithExtensions && seenStaticClassWithExtensions)
            {
                members.RemoveDuplicates();
            }
        }

        internal override void LookupSymbolsInSingleBinder(
            LookupResult result, string name, int arity, ConsList<TypeSymbol>? basesBeingResolved, LookupOptions options, Binder originalBinder, bool diagnose, ref CompoundUseSiteInfo<AssemblySymbol> useSiteInfo)
        {
            Debug.Assert(result.IsClear);

            bool callerIsSemanticModel = originalBinder.IsSemanticModelBinder;

            foreach (var typeOrNamespace in this.GetUsings(basesBeingResolved))
            {
                ImmutableArray<Symbol> candidates = Binder.GetCandidateMembers(typeOrNamespace.NamespaceOrType, name, options, originalBinder: originalBinder);
                foreach (Symbol symbol in candidates)
                {
                    if (!IsValidLookupCandidateInUsings(symbol))
                    {
                        continue;
                    }

                    // Found a match in our list of normal using directives.  Mark the directive
                    // as being seen so that it won't be reported to the user as something that
                    // can be removed.
                    var res = originalBinder.CheckViability(symbol, arity, options, null, diagnose, ref useSiteInfo, basesBeingResolved);
                    if (res.Kind == LookupResultKind.Viable)
                    {
                        MarkImportDirective(typeOrNamespace.UsingDirectiveReference, callerIsSemanticModel);
                    }

                    result.MergeEqual(res);
                }
            }
        }

        private static bool IsValidLookupCandidateInUsings(Symbol symbol)
        {
            Debug.Assert(!symbol.IsExtensionBlockMember());
            switch (symbol.Kind)
            {
                // lookup via "using namespace" ignores namespaces inside
                case SymbolKind.Namespace:
                    return false;

                // lookup via "using static" ignores extension methods and non-static methods
                case SymbolKind.Method:
                    if (!symbol.IsStatic || ((MethodSymbol)symbol).IsExtensionMethod)
                    {
                        return false;
                    }

                    break;

                // types are considered static members for purposes of "using static" feature
                // regardless of whether they are declared with "static" modifier or not
                case SymbolKind.NamedType:
                    break;

                // lookup via "using static" ignores non-static members
                default:
                    if (!symbol.IsStatic)
                    {
                        return false;
                    }

                    break;
            }

            return true;
        }

        internal override void AddLookupSymbolsInfoInSingleBinder(LookupSymbolsInfo result, LookupOptions options, Binder originalBinder)
        {
            // If we are looking only for labels we do not need to search through the imports.
            if ((options & LookupOptions.LabelsOnly) == 0)
            {
                // Add types within namespaces imported through usings, but don't add nested namespaces.
                options = (options & ~(LookupOptions.NamespaceAliasesOnly | LookupOptions.NamespacesOrTypesOnly)) | LookupOptions.MustNotBeNamespace;

                // look in all using namespaces
                foreach (var namespaceSymbol in this.GetUsings(basesBeingResolved: null))
                {
                    foreach (var member in namespaceSymbol.NamespaceOrType.GetMembersUnordered())
                    {
                        if (IsValidLookupCandidateInUsings(member) && originalBinder.CanAddLookupSymbolInfo(member, options, result, null))
                        {
                            result.AddSymbol(member, member.Name, member.GetArity());
                        }
                    }
                }
            }
        }

        protected override SourceLocalSymbol? LookupLocal(SyntaxToken nameToken)
        {
            return null;
        }

        protected override LocalFunctionSymbol? LookupLocalFunction(SyntaxToken nameToken)
        {
            return null;
        }

        internal override ImportChain? ImportChain
        {
            get
            {
                if (_lazyImportChain == null)
                {
                    ImportChain? importChain = this.Next!.ImportChain;

                    if (_withImportChainEntry)
                    {
                        importChain = new ImportChain(GetImports(), importChain);
                    }

                    Interlocked.CompareExchange(ref _lazyImportChain, importChain, null);
                }

                Debug.Assert(_lazyImportChain != null || !_withImportChainEntry);

                return _lazyImportChain;
            }
        }

        protected abstract Imports GetImports();

        internal static WithUsingNamespacesAndTypesBinder Create(SourceNamespaceSymbol declaringSymbol, CSharpSyntaxNode declarationSyntax, Binder next, bool withPreviousSubmissionImports = false, bool withImportChainEntry = false)
        {
            if (withPreviousSubmissionImports)
            {
                return new FromSyntaxWithPreviousSubmissionImports(declaringSymbol, declarationSyntax, next, withImportChainEntry);
            }

            return new FromSyntax(declaringSymbol, declarationSyntax, next, withImportChainEntry);
        }

        internal static WithUsingNamespacesAndTypesBinder Create(ImmutableArray<NamespaceOrTypeAndUsingDirective> namespacesOrTypes, Binder next, bool withImportChainEntry = false)
        {
            return new FromNamespacesOrTypes(namespacesOrTypes, next, withImportChainEntry);
        }

        private sealed class FromSyntax : WithUsingNamespacesAndTypesBinder
        {
            private readonly SourceNamespaceSymbol _declaringSymbol;
            private readonly CSharpSyntaxNode _declarationSyntax;
            private ImmutableArray<NamespaceOrTypeAndUsingDirective> _lazyUsings;

            internal FromSyntax(SourceNamespaceSymbol declaringSymbol, CSharpSyntaxNode declarationSyntax, Binder next, bool withImportChainEntry)
                : base(next, withImportChainEntry)
            {
                Debug.Assert(declarationSyntax.Kind() is SyntaxKind.CompilationUnit or SyntaxKind.NamespaceDeclaration or SyntaxKind.FileScopedNamespaceDeclaration);
                _declaringSymbol = declaringSymbol;
                _declarationSyntax = declarationSyntax;
            }

            internal override ImmutableArray<NamespaceOrTypeAndUsingDirective> GetUsings(ConsList<TypeSymbol>? basesBeingResolved)
            {
                if (_lazyUsings.IsDefault)
                {
                    ImmutableInterlocked.InterlockedInitialize(ref _lazyUsings, _declaringSymbol.GetUsingNamespacesOrTypes(_declarationSyntax, basesBeingResolved));
                }

                return _lazyUsings;
            }

            protected override Imports GetImports()
            {
                return _declaringSymbol.GetImports(_declarationSyntax, basesBeingResolved: null);
            }
        }

        private sealed class FromSyntaxWithPreviousSubmissionImports : WithUsingNamespacesAndTypesBinder
        {
            private readonly SourceNamespaceSymbol _declaringSymbol;
            private readonly CSharpSyntaxNode _declarationSyntax;
            private Imports? _lazyFullImports;

            internal FromSyntaxWithPreviousSubmissionImports(SourceNamespaceSymbol declaringSymbol, CSharpSyntaxNode declarationSyntax, Binder next, bool withImportChainEntry)
                : base(next, withImportChainEntry)
            {
                Debug.Assert(declarationSyntax.IsKind(SyntaxKind.CompilationUnit) || declarationSyntax.IsKind(SyntaxKind.NamespaceDeclaration));
                _declaringSymbol = declaringSymbol;
                _declarationSyntax = declarationSyntax;
            }

            internal override ImmutableArray<NamespaceOrTypeAndUsingDirective> GetUsings(ConsList<TypeSymbol>? basesBeingResolved)
            {
                return GetImports(basesBeingResolved).Usings;
            }

            private Imports GetImports(ConsList<TypeSymbol>? basesBeingResolved)
            {
                if (_lazyFullImports is null)
                {
                    Interlocked.CompareExchange(ref _lazyFullImports,
                                                _declaringSymbol.DeclaringCompilation.GetPreviousSubmissionImports().Concat(_declaringSymbol.GetImports(_declarationSyntax, basesBeingResolved)),
                                                null);
                }

                return _lazyFullImports;
            }

            protected override Imports GetImports()
            {
                return GetImports(basesBeingResolved: null);
            }
        }

        private sealed class FromNamespacesOrTypes : WithUsingNamespacesAndTypesBinder
        {
            private readonly ImmutableArray<NamespaceOrTypeAndUsingDirective> _usings;

            internal FromNamespacesOrTypes(ImmutableArray<NamespaceOrTypeAndUsingDirective> namespacesOrTypes, Binder next, bool withImportChainEntry)
                : base(next, withImportChainEntry)
            {
                Debug.Assert(!namespacesOrTypes.IsDefault);
                _usings = namespacesOrTypes;
            }

            internal override ImmutableArray<NamespaceOrTypeAndUsingDirective> GetUsings(ConsList<TypeSymbol>? basesBeingResolved)
            {
                return _usings;
            }

            protected override Imports GetImports()
            {
                return Imports.Create(ImmutableDictionary<string, AliasAndUsingDirective>.Empty, _usings, ImmutableArray<AliasAndExternAliasDirective>.Empty);
            }
        }
    }
}
