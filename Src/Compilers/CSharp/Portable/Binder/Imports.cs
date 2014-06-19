// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// Represents symbols imported to the binding scope via using namespace, using alias, and extern alias.
    /// </summary>
    internal sealed class Imports
    {
        internal static readonly Imports Empty = new Imports(null, null,
            ImmutableArray<NamespaceOrTypeAndUsingDirective>.Empty, ImmutableArray<AliasAndExternAliasDirective>.Empty, default(ImmutableArray<Diagnostic>));

        private readonly CSharpCompilation compilation;
        private readonly ImmutableArray<Diagnostic> diagnostics;

        // completion state that tracks whether validation was done/not done/currently in process. 
        private SymbolCompletionState state;

        public readonly Dictionary<string, AliasAndUsingDirective> UsingAliases;
        public readonly ImmutableArray<NamespaceOrTypeAndUsingDirective> Usings;
        public readonly ImmutableArray<AliasAndExternAliasDirective> ExternAliases;

        private Imports(
            CSharpCompilation compilation,
            Dictionary<string, AliasAndUsingDirective> usingAliases,
            ImmutableArray<NamespaceOrTypeAndUsingDirective> usings,
            ImmutableArray<AliasAndExternAliasDirective> externs,
            ImmutableArray<Diagnostic> diagnostics)
        {
            Debug.Assert(!usings.IsDefault && !externs.IsDefault);

            this.compilation = compilation;
            this.UsingAliases = usingAliases;
            this.Usings = usings;
            this.diagnostics = diagnostics;
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
            if (declarationSyntax.Kind == SyntaxKind.CompilationUnit)
            {
                var compilation = (CompilationUnitSyntax)declarationSyntax;
                // using directives are not in scope within using directives
                usingDirectives = inUsing ? default(SyntaxList<UsingDirectiveSyntax>) : compilation.Usings;
                externAliasDirectives = compilation.Externs;
            }
            else if (declarationSyntax.Kind == SyntaxKind.NamespaceDeclaration)
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

            // EDMAURER define all of the extern aliases first. They may used by the target of a using

            // using Bar=Foo::Bar;
            // using Foo::Baz;
            // extern alias Foo;

            var diagnostics = DiagnosticBag.GetInstance();

            var externAliases = BuildExternAliases(externAliasDirectives, binder, diagnostics);
            var usings = ArrayBuilder<NamespaceOrTypeAndUsingDirective>.GetInstance();
            Dictionary<string, AliasAndUsingDirective> usingAliases = null;

            if (usingDirectives.Count > 0)
            {
                // A binder that contains the extern aliases but not the usings. The resolution of the target of a using directive or alias 
                // should not make use of other peer usings.
                InContainerBinder usingsBinder;
                if (binder.Container.IsSubmissionClass)
                {
                    // Top-level usings in interactive code are resolved in the context of global namespace, w/o extern aliases:
                    usingsBinder = new InContainerBinder(binder.Compilation.GlobalNamespace, new BuckStopsHereBinder(binder.Compilation));
                }
                else
                {
                    usingsBinder = new InContainerBinder(binder.Container, binder.Next,
                        new Imports(binder.Compilation, null, ImmutableArray<NamespaceOrTypeAndUsingDirective>.Empty, externAliases, default(ImmutableArray<Diagnostic>)));
                }

                var uniqueUsings = new HashSet<NamespaceOrTypeSymbol>();

                foreach (var u in usingDirectives)
                {
                    binder.Compilation.RecordImport(u);

                    if (u.Alias != null)
                    {
                        string identifierValueText = u.Alias.Name.Identifier.ValueText;
                        if (usingAliases != null && usingAliases.ContainsKey(identifierValueText))
                        {
                            // Suppress diagnostics if we're already broken.
                            if (!u.Name.IsMissing)
                            {
                                // The using alias '{0}' appeared previously in this namespace
                                diagnostics.Add(ErrorCode.ERR_DuplicateAlias, u.Alias.Name.Location, identifierValueText);
                            }
                        }
                        else
                        {
                            //EDMAURER an O(m*n) algorithm here but n (number of extern aliases) will likely be very small.
                            foreach (var e in externAliases)
                            {
                                if (e.Alias.Name == identifierValueText)
                                {
                                    // The using alias '{0}' appeared previously in this namespace
                                    diagnostics.Add(ErrorCode.ERR_DuplicateAlias, u.Location, identifierValueText);
                                    break;
                                }
                            }

                            if (usingAliases == null)
                            {
                                usingAliases = new Dictionary<string, AliasAndUsingDirective>();
                            }

                            // EDMAURER construct the alias sym with the binder for which we are building imports. That
                            // way the alias target can make use of extern alias definitions.
                            usingAliases.Add(identifierValueText, new AliasAndUsingDirective(new AliasSymbol(usingsBinder, u), u));
                        }
                    }
                    else
                    {
                        if (u.Name.IsMissing)
                        {
                            //don't try to lookup namespaces inserted by parser error recovery
                            continue;
                        }

                        var imported = usingsBinder.BindNamespaceOrTypeSymbol(u.Name, diagnostics, basesBeingResolved);
                        if (imported.Kind == SymbolKind.Namespace)
                        {
                            if (uniqueUsings.Contains(imported))
                            {
                                diagnostics.Add(ErrorCode.WRN_DuplicateUsing, u.Name.Location, imported);
                            }
                            else
                            {
                                uniqueUsings.Add(imported);
                                usings.Add(new NamespaceOrTypeAndUsingDirective(imported, u));
                            }
                        }
                        else if (imported.Kind == SymbolKind.NamedType)
                        {
                            var importedType = (NamedTypeSymbol)imported;
                            if (!binder.AllowStaticClassUsings)
                            {
                                // error: A using directive can only be applied to namespace; '{0}' is a type not a namespace
                                diagnostics.Add(ErrorCode.ERR_BadUsingNamespace, u.Name.Location, importedType);
                            }
                            else if (importedType.IsStatic && importedType.TypeKind == TypeKind.Class)
                            {
                                if (uniqueUsings.Contains(importedType))
                                {
                                    diagnostics.Add(ErrorCode.WRN_DuplicateUsing, u.Name.Location, importedType);
                                }
                                else
                                {
                                    uniqueUsings.Add(importedType);
                                    usings.Add(new NamespaceOrTypeAndUsingDirective(importedType, u));
                                }
                            }
                            else
                            {
                                // error: A using directive can only be applied to classes that are static; '{0}' is not a static class
                                diagnostics.Add(ErrorCode.ERR_BadUsingType, u.Name.Location, importedType);
                            }
                        }
                        else if (imported.Kind != SymbolKind.ErrorType)
                        {
                            // Do not report additional error if the symbol itself is erroneous.

                            // error: '<symbol>' is a '<symbol kind>' but is used as 'type or namespace'
                            diagnostics.Add(ErrorCode.ERR_BadSKknown, u.Name.Location,
                                u.Name,
                                imported.GetKindText(),
                                MessageID.IDS_SK_TYPE_OR_NAMESPACE.Localize());
                        }
                    }
                }
            }

            return new Imports(binder.Compilation, usingAliases, usings.ToImmutableAndFree(), externAliases, diagnostics.ToReadOnlyAndFree());
        }

        public static Imports FromGlobalUsings(CSharpCompilation compilation)
        {
            var usings = compilation.Options.Usings;
            var diagnostics = DiagnosticBag.GetInstance();
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

            return new Imports(compilation, null, boundUsings.ToImmutableAndFree(), ImmutableArray<AliasAndExternAliasDirective>.Empty, diagnostics.ToReadOnlyAndFree());
        }

        public static Imports FromCustomDebugInfo(
            CSharpCompilation compilation,
            Dictionary<string, AliasAndUsingDirective> usingAliases,
            ImmutableArray<NamespaceOrTypeAndUsingDirective> usings,
            ImmutableArray<AliasAndExternAliasDirective> externs)
        {
            return new Imports(compilation, usingAliases, usings, externs, ImmutableArray<Diagnostic>.Empty);
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

                //EDMAURER some n^2 action, but n should be very small.
                foreach (var existingAlias in builder)
                {
                    if (existingAlias.Alias.Name == aliasSyntax.Identifier.ValueText)
                    {
                        diagnostics.Add(ErrorCode.ERR_DuplicateAlias, existingAlias.Alias.Locations[0], existingAlias.Alias.Name);
                        break;
                    }
                }

                if (aliasSyntax.Identifier.CSharpContextualKind() == SyntaxKind.GlobalKeyword)
                {
                    diagnostics.Add(ErrorCode.ERR_GlobalExternAlias, aliasSyntax.Identifier.GetLocation());
                }

                builder.Add(new AliasAndExternAliasDirective(new AliasSymbol(binder, aliasSyntax), aliasSyntax));
            }

            return builder.ToImmutableAndFree();
        }

        private void MarkImportDirective(CSharpSyntaxNode directive, bool callerIsSemanticModel)
        {
            if (directive != null && this.compilation != null && !callerIsSemanticModel)
            {
                compilation.MarkImportDirectiveAsUsed(directive);
            }
        }

        internal void Complete(CancellationToken cancellationToken)
        {
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var incompletePart = state.NextIncompletePart;
                switch (incompletePart)
                {
                    case CompletionPart.StartValidatingImports:
                        {
                            if (state.NotePartComplete(CompletionPart.StartValidatingImports))
                            {
                                Validate();
                                state.NotePartComplete(CompletionPart.FinishValidatingImports);
                            }
                        }
                        break;

                    case CompletionPart.FinishValidatingImports:
                        // some other thread has started validating imports (otherwise we would be in the case above) so
                        // we just wait for it to both finish and report the diagnostics.
                        Debug.Assert(state.HasComplete(CompletionPart.StartValidatingImports));
                        state.SpinWaitComplete(CompletionPart.FinishValidatingImports, cancellationToken);
                        break;

                    case CompletionPart.None:
                        return;

                    default:
                        // any other values are completion parts intended for other kinds of symbols
                        state.NotePartComplete(CompletionPart.All & ~CompletionPart.ImportsAll);
                        break;
                }

                state.SpinWaitComplete(incompletePart, cancellationToken);
            }
        }

        private void Validate()
        {
            DiagnosticBag semanticDiagnostics = this.compilation.SemanticDiagnostics;

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

            if (!this.diagnostics.IsEmpty)
            {
                semanticDiagnostics.AddRange(diagnostics);
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
                    // lookup via "using namespace" ignores namespaces inside
                    if (symbol.Kind != SymbolKind.Namespace)
                    {
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
        }

        internal void LookupExtensionMethodsInUsings(
            ArrayBuilder<MethodSymbol> methods,
            string name,
            int arity,
            LookupOptions options,
            bool callerIsSemanticModel)
        {
            foreach (var nsOrType in this.Usings)
            {
                if (nsOrType.NamespaceOrType.Kind == SymbolKind.Namespace)
                {
                    var count = methods.Count;
                    ((NamespaceSymbol)nsOrType.NamespaceOrType).GetExtensionMethods(methods, name, arity, options);

                    // If we found any extension methods, then consider this using as used.
                    if (methods.Count != count)
                    {
                        MarkImportDirective(nsOrType.UsingDirective, callerIsSemanticModel);
                    }
                }
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