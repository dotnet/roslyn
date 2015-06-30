﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// Represents symbols imported to the binding scope via using namespace, using alias, and extern alias.
    /// </summary>
    internal sealed class Imports
    {
        internal static readonly Imports Empty = new Imports(null, null,
            ImmutableArray<NamespaceOrTypeAndUsingDirective>.Empty, ImmutableArray<AliasAndExternAliasDirective>.Empty, null);

        private readonly CSharpCompilation _compilation;
        private readonly DiagnosticBag _diagnostics;

        // completion state that tracks whether validation was done/not done/currently in process. 
        private SymbolCompletionState _state;

        public readonly Dictionary<string, AliasAndUsingDirective> UsingAliases;
        public readonly ImmutableArray<NamespaceOrTypeAndUsingDirective> Usings;
        public readonly ImmutableArray<AliasAndExternAliasDirective> ExternAliases;

        private Imports(
            CSharpCompilation compilation,
            Dictionary<string, AliasAndUsingDirective> usingAliases,
            ImmutableArray<NamespaceOrTypeAndUsingDirective> usings,
            ImmutableArray<AliasAndExternAliasDirective> externs,
            DiagnosticBag diagnostics)
        {
            Debug.Assert(!usings.IsDefault && !externs.IsDefault);

            _compilation = compilation;
            this.UsingAliases = usingAliases;
            this.Usings = usings;
            _diagnostics = diagnostics;
            this.ExternAliases = externs;
        }

        public static Imports FromSyntax(
            CSharpSyntaxNode declarationSyntax,
            InContainerBinder binder,
            ConsList<Symbol> basesBeingResolved,
            bool inUsing)
        {
            SyntaxList<UsingDirectiveSyntax> usingDirectives;
            SyntaxList<ExternAliasDirectiveSyntax> externAliasDirectives;
            if (declarationSyntax.Kind() == SyntaxKind.CompilationUnit)
            {
                var compilation = (CompilationUnitSyntax)declarationSyntax;
                // using directives are not in scope within using directives
                usingDirectives = inUsing ? default(SyntaxList<UsingDirectiveSyntax>) : compilation.Usings;
                externAliasDirectives = compilation.Externs;
            }
            else if (declarationSyntax.Kind() == SyntaxKind.NamespaceDeclaration)
            {
                var namespaceDecl = (NamespaceDeclarationSyntax)declarationSyntax;
                // using directives are not in scope within using directives
                usingDirectives = inUsing ? default(SyntaxList<UsingDirectiveSyntax>) : namespaceDecl.Usings;
                externAliasDirectives = namespaceDecl.Externs;
            }
            else
            {
                return Empty;
            }

            if (usingDirectives.Count == 0 && externAliasDirectives.Count == 0)
            {
                return Empty;
            }

            // define all of the extern aliases first. They may used by the target of a using

            // using Bar=Foo::Bar;
            // using Foo::Baz;
            // extern alias Foo;

            var diagnostics = new DiagnosticBag();

            var externAliases = BuildExternAliases(externAliasDirectives, binder, diagnostics);
            var usings = ArrayBuilder<NamespaceOrTypeAndUsingDirective>.GetInstance();
            Dictionary<string, AliasAndUsingDirective> usingAliases = null;

            if (usingDirectives.Count > 0)
            {
                // A binder that contains the extern aliases but not the usings. The resolution of the target of a using directive or alias 
                // should not make use of other peer usings.
                var usingsBinder = binder.IsSubmissionClass ?
                    // Top-level usings in interactive code are resolved in the context of global namespace, w/o extern aliases:
                    new InContainerBinder(binder.Compilation.GlobalNamespace, new BuckStopsHereBinder(binder.Compilation)) :
                    new InContainerBinder(binder.Container, binder.Next,
                        new Imports(binder.Compilation, null, ImmutableArray<NamespaceOrTypeAndUsingDirective>.Empty, externAliases, null));

                var uniqueUsings = PooledHashSet<NamespaceOrTypeSymbol>.GetInstance();

                foreach (var usingDirective in usingDirectives)
                {
                    binder.Compilation.RecordImport(usingDirective);

                    if (usingDirective.Alias != null)
                    {
                        if (usingDirective.StaticKeyword != default(SyntaxToken))
                        {
                            diagnostics.Add(ErrorCode.ERR_NoAliasHere, usingDirective.Alias.Name.Location);
                        }

                        string identifierValueText = usingDirective.Alias.Name.Identifier.ValueText;
                        if (usingAliases != null && usingAliases.ContainsKey(identifierValueText))
                        {
                            // Suppress diagnostics if we're already broken.
                            if (!usingDirective.Name.IsMissing)
                            {
                                // The using alias '{0}' appeared previously in this namespace
                                diagnostics.Add(ErrorCode.ERR_DuplicateAlias, usingDirective.Alias.Name.Location, identifierValueText);
                            }
                        }
                        else
                        {
                            // an O(m*n) algorithm here but n (number of extern aliases) will likely be very small.
                            foreach (var externAlias in externAliases)
                            {
                                if (externAlias.Alias.Name == identifierValueText)
                                {
                                    // The using alias '{0}' appeared previously in this namespace
                                    diagnostics.Add(ErrorCode.ERR_DuplicateAlias, usingDirective.Location, identifierValueText);
                                    break;
                                }
                            }

                            if (usingAliases == null)
                            {
                                usingAliases = new Dictionary<string, AliasAndUsingDirective>();
                            }

                            // construct the alias sym with the binder for which we are building imports. That
                            // way the alias target can make use of extern alias definitions.
                            usingAliases.Add(identifierValueText, new AliasAndUsingDirective(new AliasSymbol(usingsBinder, usingDirective), usingDirective));
                        }
                    }
                    else
                    {
                        if (usingDirective.Name.IsMissing)
                        {
                            //don't try to lookup namespaces inserted by parser error recovery
                            continue;
                        }

                        var imported = usingsBinder.BindNamespaceOrTypeSymbol(usingDirective.Name, diagnostics, basesBeingResolved);
                        if (imported.Kind == SymbolKind.Namespace)
                        {
                            if (usingDirective.StaticKeyword != default(SyntaxToken))
                            {
                                diagnostics.Add(ErrorCode.ERR_BadUsingType, usingDirective.Name.Location, imported);
                            }
                            else if (uniqueUsings.Contains(imported))
                            {
                                diagnostics.Add(ErrorCode.WRN_DuplicateUsing, usingDirective.Name.Location, imported);
                            }
                            else
                            {
                                uniqueUsings.Add(imported);
                                usings.Add(new NamespaceOrTypeAndUsingDirective(imported, usingDirective));
                            }
                        }
                        else if (imported.Kind == SymbolKind.NamedType)
                        {
                            if (usingDirective.StaticKeyword == default(SyntaxToken))
                            {
                                diagnostics.Add(ErrorCode.ERR_BadUsingNamespace, usingDirective.Name.Location, imported);
                            }
                            else
                            {
                                var importedType = (NamedTypeSymbol)imported;
                                if (uniqueUsings.Contains(importedType))
                                {
                                    diagnostics.Add(ErrorCode.WRN_DuplicateUsing, usingDirective.Name.Location, importedType);
                                }
                                else
                                {
                                    uniqueUsings.Add(importedType);
                                    usings.Add(new NamespaceOrTypeAndUsingDirective(importedType, usingDirective));
                                }
                            }
                        }
                        else if (imported.Kind != SymbolKind.ErrorType)
                        {
                            // Do not report additional error if the symbol itself is erroneous.

                            // error: '<symbol>' is a '<symbol kind>' but is used as 'type or namespace'
                            diagnostics.Add(ErrorCode.ERR_BadSKknown, usingDirective.Name.Location,
                                usingDirective.Name,
                                imported.GetKindText(),
                                MessageID.IDS_SK_TYPE_OR_NAMESPACE.Localize());
                        }
                    }
                }

                uniqueUsings.Free();
            }

            if (diagnostics.IsEmptyWithoutResolution)
            {
                diagnostics = null;
            }

            return new Imports(binder.Compilation, usingAliases, usings.ToImmutableAndFree(), externAliases, diagnostics);
        }

        public static Imports FromGlobalUsings(CSharpCompilation compilation)
        {
            var usings = compilation.Options.Usings;
            var diagnostics = new DiagnosticBag();
            var usingsBinder = new InContainerBinder(compilation.GlobalNamespace, new BuckStopsHereBinder(compilation));
            var boundUsings = ArrayBuilder<NamespaceOrTypeAndUsingDirective>.GetInstance();

            foreach (string ns in usings)
            {
                if (!ns.IsValidClrNamespaceName())
                {
                    continue;
                }

                string[] identifiers = ns.Split('.');
                NameSyntax qualifiedName = SyntaxFactory.IdentifierName(identifiers[0]);

                for (int j = 1; j < identifiers.Length; j++)
                {
                    qualifiedName = SyntaxFactory.QualifiedName(left: qualifiedName, right: SyntaxFactory.IdentifierName(identifiers[j]));
                }

                boundUsings.Add(new NamespaceOrTypeAndUsingDirective(usingsBinder.BindNamespaceOrTypeSymbol(qualifiedName, diagnostics), null));
            }

            if (diagnostics.IsEmptyWithoutResolution)
            {
                diagnostics = null;
            }

            return new Imports(compilation, null, boundUsings.ToImmutableAndFree(), ImmutableArray<AliasAndExternAliasDirective>.Empty, diagnostics);
        }

        public static Imports FromCustomDebugInfo(
            CSharpCompilation compilation,
            Dictionary<string, AliasAndUsingDirective> usingAliases,
            ImmutableArray<NamespaceOrTypeAndUsingDirective> usings,
            ImmutableArray<AliasAndExternAliasDirective> externs)
        {
            return new Imports(compilation, usingAliases, usings, externs, null);
        }

        private static ImmutableArray<AliasAndExternAliasDirective> BuildExternAliases(
            SyntaxList<ExternAliasDirectiveSyntax> syntaxList,
            InContainerBinder binder,
            DiagnosticBag diagnostics)
        {
            CSharpCompilation compilation = binder.Compilation;

            var builder = ArrayBuilder<AliasAndExternAliasDirective>.GetInstance();

            foreach (ExternAliasDirectiveSyntax aliasSyntax in syntaxList)
            {
                compilation.RecordImport(aliasSyntax);

                // Extern aliases not allowed in interactive submissions:
                if (compilation.IsSubmission)
                {
                    diagnostics.Add(ErrorCode.ERR_ExternAliasNotAllowed, aliasSyntax.Location);
                    continue;
                }

                // some n^2 action, but n should be very small.
                foreach (var existingAlias in builder)
                {
                    if (existingAlias.Alias.Name == aliasSyntax.Identifier.ValueText)
                    {
                        diagnostics.Add(ErrorCode.ERR_DuplicateAlias, existingAlias.Alias.Locations[0], existingAlias.Alias.Name);
                        break;
                    }
                }

                if (aliasSyntax.Identifier.ContextualKind() == SyntaxKind.GlobalKeyword)
                {
                    diagnostics.Add(ErrorCode.ERR_GlobalExternAlias, aliasSyntax.Identifier.GetLocation());
                }

                builder.Add(new AliasAndExternAliasDirective(new AliasSymbol(binder, aliasSyntax), aliasSyntax));
            }

            return builder.ToImmutableAndFree();
        }

        private void MarkImportDirective(CSharpSyntaxNode directive, bool callerIsSemanticModel)
        {
            if (directive != null && _compilation != null && !callerIsSemanticModel)
            {
                _compilation.MarkImportDirectiveAsUsed(directive);
            }
        }

        internal void Complete(CancellationToken cancellationToken)
        {
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var incompletePart = _state.NextIncompletePart;
                switch (incompletePart)
                {
                    case CompletionPart.StartValidatingImports:
                        {
                            if (_state.NotePartComplete(CompletionPart.StartValidatingImports))
                            {
                                Validate();
                                _state.NotePartComplete(CompletionPart.FinishValidatingImports);
                            }
                        }
                        break;

                    case CompletionPart.FinishValidatingImports:
                        // some other thread has started validating imports (otherwise we would be in the case above) so
                        // we just wait for it to both finish and report the diagnostics.
                        Debug.Assert(_state.HasComplete(CompletionPart.StartValidatingImports));
                        _state.SpinWaitComplete(CompletionPart.FinishValidatingImports, cancellationToken);
                        break;

                    case CompletionPart.None:
                        return;

                    default:
                        // any other values are completion parts intended for other kinds of symbols
                        _state.NotePartComplete(CompletionPart.All & ~CompletionPart.ImportsAll);
                        break;
                }

                _state.SpinWaitComplete(incompletePart, cancellationToken);
            }
        }

        private void Validate()
        {
            DiagnosticBag semanticDiagnostics = _compilation.DeclarationDiagnostics;

            if (UsingAliases != null)
            {
                // Check constraints within named aliases.

                // Force resolution of named aliases.
                foreach (var alias in UsingAliases.Values)
                {
                    alias.Alias.GetAliasTarget(basesBeingResolved: null);
                    semanticDiagnostics.AddRange(alias.Alias.AliasTargetDiagnostics);
                }

                foreach (var alias in UsingAliases.Values)
                {
                    alias.Alias.CheckConstraints(semanticDiagnostics);
                }
            }

            // Force resolution of extern aliases.
            foreach (var alias in ExternAliases)
            {
                alias.Alias.GetAliasTarget(null);
                semanticDiagnostics.AddRange(alias.Alias.AliasTargetDiagnostics);
            }

            if (_diagnostics != null && !_diagnostics.IsEmptyWithoutResolution)
            {
                semanticDiagnostics.AddRange(_diagnostics.AsEnumerable());
            }
        }

        internal bool IsUsingAlias(string name, bool callerIsSemanticModel)
        {
            AliasAndUsingDirective node;
            if (this.UsingAliases != null && this.UsingAliases.TryGetValue(name, out node))
            {
                // This method is called by InContainerBinder.LookupSymbolsInSingleBinder to see if
                // there's a conflict between an alias and a member.  As a conflict may cause a
                // speculative lambda binding to fail this is semantically relevant and we need to
                // mark this using alias as referenced (and thus not something that can be removed).
                MarkImportDirective(node.UsingDirective, callerIsSemanticModel);
                return true;
            }

            return false;
        }

        internal void LookupSymbol(
            Binder originalBinder,
            LookupResult result,
            string name,
            int arity,
            ConsList<Symbol> basesBeingResolved,
            LookupOptions options,
            bool diagnose,
            ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            LookupSymbolInAliases(originalBinder, result, name, arity, basesBeingResolved, options, diagnose, ref useSiteDiagnostics);

            if (!result.IsMultiViable && (options & LookupOptions.NamespaceAliasesOnly) == 0)
            {
                LookupSymbolInUsings(this.Usings, originalBinder, result, name, arity, basesBeingResolved, options, diagnose, ref useSiteDiagnostics);
            }
        }

        internal void LookupSymbolInAliases(
            Binder originalBinder,
            LookupResult result,
            string name,
            int arity,
            ConsList<Symbol> basesBeingResolved,
            LookupOptions options,
            bool diagnose,
            ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            bool callerIsSemanticModel = originalBinder.IsSemanticModelBinder;

            AliasAndUsingDirective alias;
            if (this.UsingAliases != null && this.UsingAliases.TryGetValue(name, out alias))
            {
                // Found a match in our list of normal aliases.  Mark the alias as being seen so that
                // it won't be reported to the user as something that can be removed.
                var res = originalBinder.CheckViability(alias.Alias, arity, options, null, diagnose, ref useSiteDiagnostics, basesBeingResolved);
                if (res.Kind == LookupResultKind.Viable)
                {
                    MarkImportDirective(alias.UsingDirective, callerIsSemanticModel);
                }

                result.MergeEqual(res);
            }

            foreach (var a in this.ExternAliases)
            {
                if (a.Alias.Name == name)
                {
                    // Found a match in our list of extern aliases.  Mark the extern alias as being
                    // seen so that it won't be reported to the user as something that can be
                    // removed.
                    var res = originalBinder.CheckViability(a.Alias, arity, options, null, diagnose, ref useSiteDiagnostics, basesBeingResolved);
                    if (res.Kind == LookupResultKind.Viable)
                    {
                        MarkImportDirective(a.ExternAliasDirective, callerIsSemanticModel);
                    }

                    result.MergeEqual(res);
                }
            }
        }

        internal void LookupSymbolInUsings(
            ImmutableArray<NamespaceOrTypeAndUsingDirective> usings,
            Binder originalBinder,
            LookupResult result,
            string name,
            int arity,
            ConsList<Symbol> basesBeingResolved,
            LookupOptions options,
            bool diagnose,
            ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            bool callerIsSemanticModel = originalBinder.IsSemanticModelBinder;

            foreach (var typeOrNamespace in usings)
            {
                ImmutableArray<Symbol> candidates = Binder.GetCandidateMembers(typeOrNamespace.NamespaceOrType, name, options, originalBinder: originalBinder);
                foreach (Symbol symbol in candidates)
                {
                    switch (symbol.Kind)
                    {
                        // lookup via "using namespace" ignores namespaces inside
                        case SymbolKind.Namespace:
                            continue;

                        // lookup via "using static" ignores extension methods and non-static methods
                        case SymbolKind.Method:
                            if (!symbol.IsStatic || ((MethodSymbol)symbol).IsExtensionMethod)
                            {
                                continue;
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
                                continue;
                            }

                            break;
                    }

                    // Found a match in our list of normal using directives.  Mark the directive
                    // as being seen so that it won't be reported to the user as something that
                    // can be removed.
                    var res = originalBinder.CheckViability(symbol, arity, options, null, diagnose, ref useSiteDiagnostics, basesBeingResolved);
                    if (res.Kind == LookupResultKind.Viable)
                    {
                        MarkImportDirective(typeOrNamespace.UsingDirective, callerIsSemanticModel);
                    }

                    result.MergeEqual(res);
                }
            }
        }

        internal void LookupExtensionMethodsInUsings(
            ArrayBuilder<MethodSymbol> methods,
            string name,
            int arity,
            LookupOptions options,
            bool callerIsSemanticModel)
        {
            Debug.Assert(methods.Count == 0);

            // We need to avoid collecting multiple candidates for an extension method imported both through a namespace and a static class
            // We will look for duplicates only if both of the following flags are set to true
            bool seenNamespaceWithExtensionMethods = false;
            bool seenStaticClassWithExtensionMethods = false;

            foreach (var nsOrType in this.Usings)
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
                                MarkImportDirective(nsOrType.UsingDirective, callerIsSemanticModel);
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
                                MarkImportDirective(nsOrType.UsingDirective, callerIsSemanticModel);
                                seenStaticClassWithExtensionMethods = true;
                            }

                            break;
                        }
                }
            }

            if (seenNamespaceWithExtensionMethods && seenStaticClassWithExtensionMethods)
            {
                var methodsNoDuplicates = ArrayBuilder<MethodSymbol>.GetInstance();
                methodsNoDuplicates.AddRange(methods.Distinct());
                if (methodsNoDuplicates.Count < methods.Count)
                {
                    methods.Clear();
                    methods.AddRange(methodsNoDuplicates);
                }

                methodsNoDuplicates.Free();
            }
        }

        // Note: we do not mark nodes when looking up arities or names.  This is because these two
        // types of lookup are only around to make the public
        // SemanticModel.LookupNames/LookupSymbols work and do not count as usages of the directives
        // when the actual code is bound.

        internal void AddLookupSymbolsInfoInAliases(Binder binder, LookupSymbolsInfo result, LookupOptions options)
        {
            if (this.UsingAliases != null)
            {
                foreach (var usingAlias in this.UsingAliases.Values)
                {
                    var usingAliasSymbol = usingAlias.Alias;
                    var usingAliasTargetSymbol = usingAliasSymbol.GetAliasTarget(basesBeingResolved: null);
                    if (binder.CanAddLookupSymbolInfo(usingAliasTargetSymbol, options, null))
                    {
                        result.AddSymbol(usingAliasSymbol, usingAliasSymbol.Name, 0);
                    }
                }
            }

            if (this.ExternAliases != null)
            {
                foreach (var externAlias in this.ExternAliases)
                {
                    var externAliasSymbol = externAlias.Alias;
                    var externAliasTargetSymbol = externAliasSymbol.GetAliasTarget(basesBeingResolved: null);
                    if (binder.CanAddLookupSymbolInfo(externAliasTargetSymbol, options, null))
                    {
                        result.AddSymbol(externAliasSymbol, externAliasSymbol.Name, 0);
                    }
                }
            }
        }

        internal static void AddLookupSymbolsInfoInUsings(
            ImmutableArray<NamespaceOrTypeAndUsingDirective> usings, Binder binder, LookupSymbolsInfo result, LookupOptions options)
        {
            Debug.Assert(!options.CanConsiderNamespaces());

            // look in all using namespaces
            foreach (var namespaceSymbol in usings)
            {
                foreach (var member in namespaceSymbol.NamespaceOrType.GetMembersUnordered())
                {
                    if (binder.CanAddLookupSymbolInfo(member, options, null))
                    {
                        result.AddSymbol(member, member.Name, member.GetArity());
                    }
                }
            }
        }
    }
}
