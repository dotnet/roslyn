// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
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
    [DebuggerDisplay("{GetDebuggerDisplay(),nq}")]
    internal sealed class Imports
    {
        internal static readonly Imports Empty = new Imports(
            null,
            ImmutableDictionary<string, AliasAndUsingDirective>.Empty,
            ImmutableArray<NamespaceOrTypeAndUsingDirective>.Empty,
            ImmutableArray<AliasAndExternAliasDirective>.Empty,
            null);

        private readonly CSharpCompilation _compilation;
        private readonly DiagnosticBag _diagnostics;

        // completion state that tracks whether validation was done/not done/currently in process. 
        private SymbolCompletionState _state;

        public readonly ImmutableDictionary<string, AliasAndUsingDirective> UsingAliases;
        public readonly ImmutableArray<NamespaceOrTypeAndUsingDirective> Usings;
        public readonly ImmutableArray<AliasAndExternAliasDirective> ExternAliases;

        private Imports(
            CSharpCompilation compilation,
            ImmutableDictionary<string, AliasAndUsingDirective> usingAliases,
            ImmutableArray<NamespaceOrTypeAndUsingDirective> usings,
            ImmutableArray<AliasAndExternAliasDirective> externs,
            DiagnosticBag diagnostics)
        {
            Debug.Assert(usingAliases != null);
            Debug.Assert(!usings.IsDefault);
            Debug.Assert(!externs.IsDefault);

            _compilation = compilation;
            this.UsingAliases = usingAliases;
            this.Usings = usings;
            _diagnostics = diagnostics;
            this.ExternAliases = externs;
        }

        internal string GetDebuggerDisplay()
        {
            return string.Join("; ", 
                UsingAliases.OrderBy(x => x.Value.UsingDirective.Location.SourceSpan.Start).Select(ua => $"{ua.Key} = {ua.Value.Alias.Target}").Concat(
                Usings.Select(u => u.NamespaceOrType.ToString())).Concat(
                ExternAliases.Select(ea => $"extern alias {ea.Alias.Name}")));

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
                var compilationUnit = (CompilationUnitSyntax)declarationSyntax;
                // using directives are not in scope within using directives
                usingDirectives = inUsing ? default(SyntaxList<UsingDirectiveSyntax>) : compilationUnit.Usings;
                externAliasDirectives = compilationUnit.Externs;
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

            var compilation = binder.Compilation;

            var externAliases = BuildExternAliases(externAliasDirectives, binder, diagnostics);
            var usings = ArrayBuilder<NamespaceOrTypeAndUsingDirective>.GetInstance();
            ImmutableDictionary<string, AliasAndUsingDirective>.Builder usingAliases = null;
            if (usingDirectives.Count > 0)
            {
                // A binder that contains the extern aliases but not the usings. The resolution of the target of a using directive or alias 
                // should not make use of other peer usings.
                Binder usingsBinder;
                if (declarationSyntax.SyntaxTree.Options.Kind != SourceCodeKind.Regular)
                {
                    usingsBinder = compilation.GetBinderFactory(declarationSyntax.SyntaxTree).GetImportsBinder(declarationSyntax, inUsing: true);
                }
                else
                {
                    var imports = externAliases.Length == 0
                        ? Empty
                        : new Imports(
                            compilation,
                            ImmutableDictionary<string, AliasAndUsingDirective>.Empty,
                            ImmutableArray<NamespaceOrTypeAndUsingDirective>.Empty,
                            externAliases,
                            diagnostics: null);
                    usingsBinder = new InContainerBinder(binder.Container, binder.Next, imports);
                }

                var uniqueUsings = PooledHashSet<NamespaceOrTypeSymbol>.GetInstance();

                foreach (var usingDirective in usingDirectives)
                {
                    compilation.RecordImport(usingDirective);

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
                                usingAliases = ImmutableDictionary.CreateBuilder<string, AliasAndUsingDirective>();
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

                        var declarationBinder = usingsBinder.WithAdditionalFlags(BinderFlags.SuppressConstraintChecks);
                        var imported = declarationBinder.BindNamespaceOrTypeSymbol(usingDirective.Name, diagnostics, basesBeingResolved).NamespaceOrTypeSymbol;
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

            return new Imports(compilation, usingAliases.ToImmutableDictionaryOrEmpty(), usings.ToImmutableAndFree(), externAliases, diagnostics);
        }

        public static Imports FromGlobalUsings(CSharpCompilation compilation)
        {
            var usings = compilation.Options.Usings;

            if (usings.Length == 0 && compilation.PreviousSubmission == null)
            {
                return Empty;
            }

            var diagnostics = new DiagnosticBag();
            var usingsBinder = new InContainerBinder(compilation.GlobalNamespace, new BuckStopsHereBinder(compilation));
            var boundUsings = ArrayBuilder<NamespaceOrTypeAndUsingDirective>.GetInstance();
            var uniqueUsings = PooledHashSet<NamespaceOrTypeSymbol>.GetInstance();

            foreach (string @using in usings)
            {
                if (!@using.IsValidClrNamespaceName())
                {
                    continue;
                }

                string[] identifiers = @using.Split('.');
                NameSyntax qualifiedName = SyntaxFactory.IdentifierName(identifiers[0]);

                for (int j = 1; j < identifiers.Length; j++)
                {
                    qualifiedName = SyntaxFactory.QualifiedName(left: qualifiedName, right: SyntaxFactory.IdentifierName(identifiers[j]));
                }

                var imported = usingsBinder.BindNamespaceOrTypeSymbol(qualifiedName, diagnostics).NamespaceOrTypeSymbol;
                if (uniqueUsings.Add(imported))
                {
                    boundUsings.Add(new NamespaceOrTypeAndUsingDirective(imported, null));
                }
            }

            if (diagnostics.IsEmptyWithoutResolution)
            {
                diagnostics = null;
            }

            var previousSubmissionImports = compilation.PreviousSubmission?.GlobalImports;
            if (previousSubmissionImports != null)
            {
                // Currently, only usings are supported.
                Debug.Assert(previousSubmissionImports.UsingAliases.IsEmpty);
                Debug.Assert(previousSubmissionImports.ExternAliases.IsEmpty);

                var expandedImports = ExpandPreviousSubmissionImports(previousSubmissionImports, compilation);

                foreach (var previousUsing in expandedImports.Usings)
                {
                    if (uniqueUsings.Add(previousUsing.NamespaceOrType))
                    {
                        boundUsings.Add(previousUsing);
                    }
                }
            }

            uniqueUsings.Free();

            if (boundUsings.Count == 0)
            {
                boundUsings.Free();
                return Empty;
            }

            return new Imports(compilation, ImmutableDictionary<string, AliasAndUsingDirective>.Empty, boundUsings.ToImmutableAndFree(), ImmutableArray<AliasAndExternAliasDirective>.Empty, diagnostics);
        }

        // TODO (https://github.com/dotnet/roslyn/issues/5517): skip namespace expansion if references haven't changed.
        internal static Imports ExpandPreviousSubmissionImports(Imports previousSubmissionImports, CSharpCompilation newSubmission)
        {
            if (previousSubmissionImports == Empty)
            {
                return Empty;
            }

            Debug.Assert(previousSubmissionImports != null);
            Debug.Assert(previousSubmissionImports._compilation.IsSubmission);
            Debug.Assert(newSubmission.IsSubmission);

            var expandedGlobalNamespace = newSubmission.GlobalNamespace;

            var expandedAliases = ImmutableDictionary<string, AliasAndUsingDirective>.Empty;
            if (!previousSubmissionImports.UsingAliases.IsEmpty)
            {
                var expandedAliasesBuilder = ImmutableDictionary.CreateBuilder<string, AliasAndUsingDirective>();
                foreach (var pair in previousSubmissionImports.UsingAliases)
                {
                    var name = pair.Key;
                    var directive = pair.Value;
                    expandedAliasesBuilder.Add(name, new AliasAndUsingDirective(directive.Alias.ToNewSubmission(newSubmission), directive.UsingDirective));
                }
                expandedAliases = expandedAliasesBuilder.ToImmutable();
            }

            var expandedUsings = ImmutableArray<NamespaceOrTypeAndUsingDirective>.Empty;
            if (!previousSubmissionImports.Usings.IsEmpty)
            {
                var expandedUsingsBuilder = ArrayBuilder<NamespaceOrTypeAndUsingDirective>.GetInstance(previousSubmissionImports.Usings.Length);
                foreach (var previousUsing in previousSubmissionImports.Usings)
                {
                    var previousTarget = previousUsing.NamespaceOrType;
                    if (previousTarget.IsType)
                    {
                        expandedUsingsBuilder.Add(previousUsing);
                    }
                    else
                    {
                        var expandedNamespace = ExpandPreviousSubmissionNamespace((NamespaceSymbol)previousTarget, expandedGlobalNamespace);
                        expandedUsingsBuilder.Add(new NamespaceOrTypeAndUsingDirective(expandedNamespace, previousUsing.UsingDirective));
                    }
                }
                expandedUsings = expandedUsingsBuilder.ToImmutableAndFree();
            }

            return new Imports(
                newSubmission,
                expandedAliases,
                expandedUsings,
                previousSubmissionImports.ExternAliases,
                diagnostics: null);
        }

        internal static NamespaceSymbol ExpandPreviousSubmissionNamespace(NamespaceSymbol originalNamespace, NamespaceSymbol expandedGlobalNamespace)
        {
            // Soft assert: we'll still do the right thing if it fails.
            Debug.Assert(!originalNamespace.IsGlobalNamespace, "Global using to global namespace");

            // Hard assert: we depend on this.
            Debug.Assert(expandedGlobalNamespace.IsGlobalNamespace, "Global namespace required");

            var nameParts = ArrayBuilder<string>.GetInstance();
            var curr = originalNamespace;
            while (!curr.IsGlobalNamespace)
            {
                nameParts.Add(curr.Name);
                curr = curr.ContainingNamespace;
            }

            var expandedNamespace = expandedGlobalNamespace;
            for (int i = nameParts.Count - 1; i >= 0; i--)
            {
                // Note, the name may have become ambiguous (e.g. if a type with the same name
                // is now in scope), but we're not rebinding - we're just expanding to the
                // current contents of the same namespace.
                expandedNamespace = expandedNamespace.GetMembers(nameParts[i]).OfType<NamespaceSymbol>().Single();
            }
            nameParts.Free();

            return expandedNamespace;
        }

        public static Imports FromCustomDebugInfo(
            CSharpCompilation compilation,
            ImmutableDictionary<string, AliasAndUsingDirective> usingAliases,
            ImmutableArray<NamespaceOrTypeAndUsingDirective> usings,
            ImmutableArray<AliasAndExternAliasDirective> externs)
        {
            return new Imports(compilation, usingAliases, usings, externs, diagnostics: null);
        }

        /// <remarks>
        /// Does not preserve diagnostics.
        /// </remarks>
        internal Imports Concat(Imports otherImports)
        {
            Debug.Assert(otherImports != null);

            if (this == Empty)
            {
                return otherImports;
            }

            if (otherImports == Empty)
            {
                return this;
            }

            Debug.Assert(_compilation == otherImports._compilation);

            var usingAliases = this.UsingAliases.SetItems(otherImports.UsingAliases); // NB: SetItems, rather than AddRange
            var usings = this.Usings.AddRange(otherImports.Usings).Distinct(UsingTargetComparer.Instance);
            var externAliases = ConcatExternAliases(this.ExternAliases, otherImports.ExternAliases);

            return new Imports(_compilation, usingAliases, usings, externAliases, diagnostics: null);
        }

        private static ImmutableArray<AliasAndExternAliasDirective> ConcatExternAliases(ImmutableArray<AliasAndExternAliasDirective> externs1, ImmutableArray<AliasAndExternAliasDirective> externs2)
        {
            if (externs1.Length == 0)
            {
                return externs2;
            }

            if (externs2.Length == 0)
            {
                return externs1;
            }

            var replacedExternAliases = PooledHashSet<string>.GetInstance();
            replacedExternAliases.AddAll(externs2.Select(e => e.Alias.Name));
            return externs1.WhereAsArray(e => !replacedExternAliases.Contains(e.Alias.Name)).AddRange(externs2);
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
            MarkImportDirective(_compilation, directive, callerIsSemanticModel);
        }

        private static void MarkImportDirective(CSharpCompilation compilation, CSharpSyntaxNode directive, bool callerIsSemanticModel)
        {
            Debug.Assert(compilation != null); // If any directives are used, then there must be a compilation.
            if (directive != null && !callerIsSemanticModel)
            {
                compilation.MarkImportDirectiveAsUsed(directive);
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
            if (this == Empty)
            {
                return;
            }

            DiagnosticBag semanticDiagnostics = _compilation.DeclarationDiagnostics;

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
            if (this.UsingAliases.TryGetValue(name, out node))
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
            if (this.UsingAliases.TryGetValue(name, out alias))
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

        internal static void LookupSymbolInUsings(
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
            if (originalBinder.Flags.Includes(BinderFlags.InScriptUsing))
            {
                return;
            }

            bool callerIsSemanticModel = originalBinder.IsSemanticModelBinder;

            foreach (var typeOrNamespace in usings)
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
                    var res = originalBinder.CheckViability(symbol, arity, options, null, diagnose, ref useSiteDiagnostics, basesBeingResolved);
                    if (res.Kind == LookupResultKind.Viable)
                    {
                        MarkImportDirective(originalBinder.Compilation, typeOrNamespace.UsingDirective, callerIsSemanticModel);
                    }

                    result.MergeEqual(res);
                }
            }
        }

        private static bool IsValidLookupCandidateInUsings(Symbol symbol)
        {
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

        internal void LookupExtensionMethodsInUsings(
            ArrayBuilder<MethodSymbol> methods,
            string name,
            int arity,
            LookupOptions options,
            Binder originalBinder)
        {
            var binderFlags = originalBinder.Flags;
            if (binderFlags.Includes(BinderFlags.InScriptUsing))
            {
                return;
            }

            Debug.Assert(methods.Count == 0);

            bool callerIsSemanticModel = binderFlags.Includes(BinderFlags.SemanticModel);

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
                methods.RemoveDuplicates();
            }
        }

        // Note: we do not mark nodes when looking up arities or names.  This is because these two
        // types of lookup are only around to make the public
        // SemanticModel.LookupNames/LookupSymbols work and do not count as usages of the directives
        // when the actual code is bound.

        internal void AddLookupSymbolsInfo(LookupSymbolsInfo result, LookupOptions options, Binder originalBinder)
        {
            AddLookupSymbolsInfoInAliases(result, options, originalBinder);

            // Add types within namespaces imported through usings, but don't add nested namespaces.
            LookupOptions usingOptions = (options & ~(LookupOptions.NamespaceAliasesOnly | LookupOptions.NamespacesOrTypesOnly)) | LookupOptions.MustNotBeNamespace;
            AddLookupSymbolsInfoInUsings(this.Usings, result, usingOptions, originalBinder);
        }

        internal void AddLookupSymbolsInfoInAliases(LookupSymbolsInfo result, LookupOptions options, Binder originalBinder)
        {
            foreach (var usingAlias in this.UsingAliases.Values)
            {
                AddAliasSymbolToResult(result, usingAlias.Alias, options, originalBinder);
            }

            foreach (var externAlias in this.ExternAliases)
            {
                AddAliasSymbolToResult(result, externAlias.Alias, options, originalBinder);
            }
        }

        private static void AddAliasSymbolToResult(LookupSymbolsInfo result, AliasSymbol aliasSymbol, LookupOptions options, Binder originalBinder)
        {
            var targetSymbol = aliasSymbol.GetAliasTarget(basesBeingResolved: null);
            if (originalBinder.CanAddLookupSymbolInfo(targetSymbol, options, null))
            {
                result.AddSymbol(aliasSymbol, aliasSymbol.Name, 0);
            }
        }

        private static void AddLookupSymbolsInfoInUsings(
            ImmutableArray<NamespaceOrTypeAndUsingDirective> usings, LookupSymbolsInfo result, LookupOptions options, Binder originalBinder)
        {
            if (originalBinder.Flags.Includes(BinderFlags.InScriptUsing))
            {
                return;
            }

            Debug.Assert(!options.CanConsiderNamespaces());

            // look in all using namespaces
            foreach (var namespaceSymbol in usings)
            {
                foreach (var member in namespaceSymbol.NamespaceOrType.GetMembersUnordered())
                {
                    if (IsValidLookupCandidateInUsings(member) && originalBinder.CanAddLookupSymbolInfo(member, options, null))
                    {
                        result.AddSymbol(member, member.Name, member.GetArity());
                    }
                }
            }
        }

        private class UsingTargetComparer : IEqualityComparer<NamespaceOrTypeAndUsingDirective>
        {
            public static readonly IEqualityComparer<NamespaceOrTypeAndUsingDirective> Instance = new UsingTargetComparer();

            private UsingTargetComparer() { }

            bool IEqualityComparer<NamespaceOrTypeAndUsingDirective>.Equals(NamespaceOrTypeAndUsingDirective x, NamespaceOrTypeAndUsingDirective y)
            {
                return x.NamespaceOrType.Equals(y.NamespaceOrType);
            }

            int IEqualityComparer<NamespaceOrTypeAndUsingDirective>.GetHashCode(NamespaceOrTypeAndUsingDirective obj)
            {
                return obj.NamespaceOrType.GetHashCode();
            }
        }
    }
}
