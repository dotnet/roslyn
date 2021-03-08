// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    partial class SourceNamespaceSymbol
    {
        public Imports GetImports(CSharpSyntaxNode declarationSyntax, ConsList<TypeSymbol>? basesBeingResolved)
        {
            switch (declarationSyntax)
            {
                case CompilationUnitSyntax compilationUnit:
                    if (!compilationUnit.Externs.Any() && !compilationUnit.Usings.Any())
                    {
                        return Imports.Empty;
                    }
                    break;

                case NamespaceDeclarationSyntax namespaceDecl:
                    if (!namespaceDecl.Externs.Any() && !namespaceDecl.Usings.Any())
                    {
                        return Imports.Empty;
                    }
                    break;

                default:
                    throw ExceptionUtilities.UnexpectedValue(declarationSyntax);
            }

            foreach (var declaration in _mergedDeclaration.Declarations)
            {
                var declarationSyntaxRef = declaration.SyntaxReference;
                if (declarationSyntaxRef.SyntaxTree != declarationSyntax.SyntaxTree)
                {
                    continue;
                }

                if (declarationSyntaxRef.GetSyntax() == declarationSyntax)
                {
                    return _aliasesAndUsings[declaration].GetImports(this, declarationSyntax, basesBeingResolved);
                }
            }

            Debug.Assert(false);
            return Imports.Empty;
        }

        public ImmutableArray<AliasAndExternAliasDirective> GetExternAliases(CSharpSyntaxNode declarationSyntax)
        {
            switch (declarationSyntax)
            {
                case CompilationUnitSyntax compilationUnit:
                    if (!compilationUnit.Externs.Any())
                    {
                        return ImmutableArray<AliasAndExternAliasDirective>.Empty;
                    }
                    break;

                case NamespaceDeclarationSyntax namespaceDecl:
                    if (!namespaceDecl.Externs.Any())
                    {
                        return ImmutableArray<AliasAndExternAliasDirective>.Empty;
                    }
                    break;

                default:
                    throw ExceptionUtilities.UnexpectedValue(declarationSyntax);
            }

            foreach (var declaration in _mergedDeclaration.Declarations)
            {
                var declarationSyntaxRef = declaration.SyntaxReference;
                if (declarationSyntaxRef.SyntaxTree != declarationSyntax.SyntaxTree)
                {
                    continue;
                }

                if (declarationSyntaxRef.GetSyntax() == declarationSyntax)
                {
                    return _aliasesAndUsings[declaration].GetExternAliases(this, declarationSyntax);
                }
            }

            Debug.Assert(false);
            return ImmutableArray<AliasAndExternAliasDirective>.Empty;
        }

        public ImmutableArray<AliasAndUsingDirective> GetUsingAliases(CSharpSyntaxNode declarationSyntax, ConsList<TypeSymbol>? basesBeingResolved)
        {
            switch (declarationSyntax)
            {
                case CompilationUnitSyntax compilationUnit:
                    if (!compilationUnit.Usings.Any())
                    {
                        return ImmutableArray<AliasAndUsingDirective>.Empty;
                    }
                    break;

                case NamespaceDeclarationSyntax namespaceDecl:
                    if (!namespaceDecl.Usings.Any())
                    {
                        return ImmutableArray<AliasAndUsingDirective>.Empty;
                    }
                    break;

                default:
                    throw ExceptionUtilities.UnexpectedValue(declarationSyntax);
            }

            foreach (var declaration in _mergedDeclaration.Declarations)
            {
                var declarationSyntaxRef = declaration.SyntaxReference;
                if (declarationSyntaxRef.SyntaxTree != declarationSyntax.SyntaxTree)
                {
                    continue;
                }

                if (declarationSyntaxRef.GetSyntax() == declarationSyntax)
                {
                    return _aliasesAndUsings[declaration].GetUsingAliases(this, declarationSyntax, basesBeingResolved);
                }
            }

            Debug.Assert(false);
            return ImmutableArray<AliasAndUsingDirective>.Empty;
        }

        public ImmutableDictionary<string, AliasAndUsingDirective> GetUsingAliasesMap(CSharpSyntaxNode declarationSyntax, ConsList<TypeSymbol>? basesBeingResolved)
        {
            switch (declarationSyntax)
            {
                case CompilationUnitSyntax compilationUnit:
                    if (!compilationUnit.Usings.Any())
                    {
                        return ImmutableDictionary<string, AliasAndUsingDirective>.Empty;
                    }
                    break;

                case NamespaceDeclarationSyntax namespaceDecl:
                    if (!namespaceDecl.Usings.Any())
                    {
                        return ImmutableDictionary<string, AliasAndUsingDirective>.Empty;
                    }
                    break;

                default:
                    throw ExceptionUtilities.UnexpectedValue(declarationSyntax);
            }

            foreach (var declaration in _mergedDeclaration.Declarations)
            {
                var declarationSyntaxRef = declaration.SyntaxReference;
                if (declarationSyntaxRef.SyntaxTree != declarationSyntax.SyntaxTree)
                {
                    continue;
                }

                if (declarationSyntaxRef.GetSyntax() == declarationSyntax)
                {
                    return _aliasesAndUsings[declaration].GetUsingAliasesMap(this, declarationSyntax, basesBeingResolved);
                }
            }

            Debug.Assert(false);
            return ImmutableDictionary<string, AliasAndUsingDirective>.Empty;
        }

        public ImmutableArray<NamespaceOrTypeAndUsingDirective> GetUsingNamespacesOrTypes(CSharpSyntaxNode declarationSyntax, ConsList<TypeSymbol>? basesBeingResolved)
        {
            switch (declarationSyntax)
            {
                case CompilationUnitSyntax compilationUnit:
                    if (!compilationUnit.Usings.Any())
                    {
                        return ImmutableArray<NamespaceOrTypeAndUsingDirective>.Empty;
                    }
                    break;

                case NamespaceDeclarationSyntax namespaceDecl:
                    if (!namespaceDecl.Usings.Any())
                    {
                        return ImmutableArray<NamespaceOrTypeAndUsingDirective>.Empty;
                    }
                    break;

                default:
                    throw ExceptionUtilities.UnexpectedValue(declarationSyntax);
            }

            foreach (var declaration in _mergedDeclaration.Declarations)
            {
                var declarationSyntaxRef = declaration.SyntaxReference;
                if (declarationSyntaxRef.SyntaxTree != declarationSyntax.SyntaxTree)
                {
                    continue;
                }

                if (declarationSyntaxRef.GetSyntax() == declarationSyntax)
                {
                    return _aliasesAndUsings[declaration].GetUsingNamespacesOrTypes(this, declarationSyntax, basesBeingResolved);
                }
            }

            Debug.Assert(false);
            return ImmutableArray<NamespaceOrTypeAndUsingDirective>.Empty;
        }

        private class AliasesAndUsings
        {
            private ExternAliasesAndDiagnostics? _lazyExternAliases;
            private UsingsAndDiagnostics? _lazyUsings;
            private Imports? _lazyImports;

            /// <summary>
            /// Completion state that tracks whether validation was done/not done/currently in process. 
            /// </summary>
            private SymbolCompletionState _state;

            internal ImmutableArray<AliasAndExternAliasDirective> GetExternAliases(SourceNamespaceSymbol declaringSymbol, CSharpSyntaxNode declarationSyntax)
            {
                return GetExternAliasesAndDiagnostics(declaringSymbol, declarationSyntax).ExternAliases;
            }

            private ExternAliasesAndDiagnostics GetExternAliasesAndDiagnostics(SourceNamespaceSymbol declaringSymbol, CSharpSyntaxNode declarationSyntax)
            {
                if (_lazyExternAliases is null)
                {
                    SyntaxList<ExternAliasDirectiveSyntax> externAliasDirectives;
                    switch (declarationSyntax)
                    {
                        case CompilationUnitSyntax compilationUnit:
                            externAliasDirectives = compilationUnit.Externs;
                            break;

                        case NamespaceDeclarationSyntax namespaceDecl:
                            externAliasDirectives = namespaceDecl.Externs;
                            break;

                        default:
                            throw ExceptionUtilities.UnexpectedValue(declarationSyntax);
                    }

                    if (!externAliasDirectives.Any())
                    {
                        _lazyExternAliases = ExternAliasesAndDiagnostics.Empty;
                    }
                    else
                    {
                        var diagnostics = DiagnosticBag.GetInstance();
                        Interlocked.CompareExchange(
                            ref _lazyExternAliases,
                            new ExternAliasesAndDiagnostics() { ExternAliases = buildExternAliases(externAliasDirectives, declaringSymbol, diagnostics), Diagnostics = diagnostics.ToReadOnlyAndFree() },
                            null);
                    }
                }

                return _lazyExternAliases;

                static ImmutableArray<AliasAndExternAliasDirective> buildExternAliases(
                    SyntaxList<ExternAliasDirectiveSyntax> syntaxList,
                    SourceNamespaceSymbol declaringSymbol,
                    DiagnosticBag diagnostics)
                {
                    CSharpCompilation compilation = declaringSymbol.DeclaringCompilation;

                    var builder = ArrayBuilder<AliasAndExternAliasDirective>.GetInstance();

                    foreach (ExternAliasDirectiveSyntax aliasSyntax in syntaxList)
                    {
                        compilation.RecordImport(aliasSyntax);
                        bool skipInLookup = false;

                        // Extern aliases not allowed in interactive submissions:
                        if (compilation.IsSubmission)
                        {
                            diagnostics.Add(ErrorCode.ERR_ExternAliasNotAllowed, aliasSyntax.Location);
                            skipInLookup = true;
                        }
                        else
                        {
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
                        }

                        builder.Add(new AliasAndExternAliasDirective(new AliasSymbolFromSyntax(declaringSymbol, aliasSyntax), aliasSyntax, skipInLookup));
                    }

                    return builder.ToImmutableAndFree();
                }
            }

            internal ImmutableArray<AliasAndUsingDirective> GetUsingAliases(SourceNamespaceSymbol declaringSymbol, CSharpSyntaxNode declarationSyntax, ConsList<TypeSymbol>? basesBeingResolved)
            {
                return GetUsingsAndDiagnostics(declaringSymbol, declarationSyntax, basesBeingResolved).UsingAliases;
            }

            internal ImmutableDictionary<string, AliasAndUsingDirective> GetUsingAliasesMap(SourceNamespaceSymbol declaringSymbol, CSharpSyntaxNode declarationSyntax, ConsList<TypeSymbol>? basesBeingResolved)
            {
                return GetUsingsAndDiagnostics(declaringSymbol, declarationSyntax, basesBeingResolved).UsingAliasesMap ?? ImmutableDictionary<string, AliasAndUsingDirective>.Empty;
            }

            internal ImmutableArray<NamespaceOrTypeAndUsingDirective> GetUsingNamespacesOrTypes(SourceNamespaceSymbol declaringSymbol, CSharpSyntaxNode declarationSyntax, ConsList<TypeSymbol>? basesBeingResolved)
            {
                return GetUsingsAndDiagnostics(declaringSymbol, declarationSyntax, basesBeingResolved).UsingNamespacesOrTypes;
            }

            private UsingsAndDiagnostics GetUsingsAndDiagnostics(SourceNamespaceSymbol declaringSymbol, CSharpSyntaxNode declarationSyntax, ConsList<TypeSymbol>? basesBeingResolved)
            {
                if (_lazyUsings is null)
                {
                    SyntaxList<UsingDirectiveSyntax> usingDirectives;
                    switch (declarationSyntax)
                    {
                        case CompilationUnitSyntax compilationUnit:
                            usingDirectives = compilationUnit.Usings;
                            break;

                        case NamespaceDeclarationSyntax namespaceDecl:
                            usingDirectives = namespaceDecl.Usings;
                            break;

                        default:
                            throw ExceptionUtilities.UnexpectedValue(declarationSyntax);
                    }

                    if (!usingDirectives.Any())
                    {
                        _lazyUsings = UsingsAndDiagnostics.Empty;
                    }
                    else
                    {
                        Interlocked.CompareExchange(
                            ref _lazyUsings,
                            buildUsings(usingDirectives, declaringSymbol, declarationSyntax, basesBeingResolved),
                            null);
                    }
                }

                return _lazyUsings;

                UsingsAndDiagnostics buildUsings(
                    SyntaxList<UsingDirectiveSyntax> usingDirectives,
                    SourceNamespaceSymbol declaringSymbol,
                    CSharpSyntaxNode declarationSyntax,
                    ConsList<TypeSymbol>? basesBeingResolved)
                {
                    // define all of the extern aliases first. They may used by the target of a using
                    var externAliases = GetExternAliases(declaringSymbol, declarationSyntax);
                    var diagnostics = new DiagnosticBag();

                    var compilation = declaringSymbol.DeclaringCompilation;

                    var usings = ArrayBuilder<NamespaceOrTypeAndUsingDirective>.GetInstance();
                    ImmutableDictionary<string, AliasAndUsingDirective>.Builder? usingAliasesMap = null;
                    var usingAliases = ArrayBuilder<AliasAndUsingDirective>.GetInstance();

                    // A binder that contains the extern aliases but not the usings. The resolution of the target of a using directive or alias 
                    // should not make use of other peer usings.
                    Binder? declarationBinder = null;

                    var uniqueUsings = SpecializedSymbolCollections.GetPooledSymbolHashSetInstance<NamespaceOrTypeSymbol>();

                    foreach (var usingDirective in usingDirectives)
                    {
                        compilation.RecordImport(usingDirective);

                        if (usingDirective.Alias != null)
                        {
                            SyntaxToken identifier = usingDirective.Alias.Name.Identifier;
                            Location location = usingDirective.Alias.Name.Location;

                            if (identifier.ContextualKind() == SyntaxKind.GlobalKeyword)
                            {
                                diagnostics.Add(ErrorCode.WRN_GlobalAliasDefn, location);
                            }

                            if (usingDirective.StaticKeyword != default(SyntaxToken))
                            {
                                diagnostics.Add(ErrorCode.ERR_NoAliasHere, location);
                            }

                            SourceMemberContainerTypeSymbol.ReportTypeNamedRecord(identifier.Text, compilation, diagnostics, location);

                            string identifierValueText = identifier.ValueText;
                            bool skipInLookup = false;

                            if (usingAliasesMap != null && usingAliasesMap.ContainsKey(identifierValueText))
                            {
                                skipInLookup = true;

                                // Suppress diagnostics if we're already broken.
                                if (!usingDirective.Name.IsMissing)
                                {
                                    // The using alias '{0}' appeared previously in this namespace
                                    diagnostics.Add(ErrorCode.ERR_DuplicateAlias, location, identifierValueText);
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
                            }

                            // construct the alias sym with the binder for which we are building imports. That
                            // way the alias target can make use of extern alias definitions.
                            var aliasAndDirective = new AliasAndUsingDirective(new AliasSymbolFromSyntax(declaringSymbol, usingDirective), usingDirective);
                            usingAliases.Add(aliasAndDirective);

                            if (usingAliasesMap == null)
                            {
                                Debug.Assert(!skipInLookup);
                                usingAliasesMap = ImmutableDictionary.CreateBuilder<string, AliasAndUsingDirective>();
                            }

                            if (!skipInLookup)
                            {
                                usingAliasesMap.Add(identifierValueText, aliasAndDirective);
                            }
                        }
                        else
                        {
                            if (usingDirective.Name.IsMissing)
                            {
                                //don't try to lookup namespaces inserted by parser error recovery
                                continue;
                            }

                            var directiveDiagnostics = BindingDiagnosticBag.GetInstance();
                            Debug.Assert(directiveDiagnostics.DiagnosticBag is object);
                            Debug.Assert(directiveDiagnostics.DependenciesBag is object);

                            declarationBinder ??= compilation.GetBinderFactory(declarationSyntax.SyntaxTree).GetBinder(usingDirective.Name).WithAdditionalFlags(BinderFlags.SuppressConstraintChecks);
                            var imported = declarationBinder.BindNamespaceOrTypeSymbol(usingDirective.Name, directiveDiagnostics, basesBeingResolved).NamespaceOrTypeSymbol;

                            if (imported.Kind == SymbolKind.Namespace)
                            {
                                Debug.Assert(directiveDiagnostics.DependenciesBag.IsEmpty());

                                if (usingDirective.StaticKeyword != default(SyntaxToken))
                                {
                                    diagnostics.Add(ErrorCode.ERR_BadUsingType, usingDirective.Name.Location, imported);
                                }
                                else if (!uniqueUsings.Add(imported))
                                {
                                    diagnostics.Add(ErrorCode.WRN_DuplicateUsing, usingDirective.Name.Location, imported);
                                }
                                else
                                {
                                    usings.Add(new NamespaceOrTypeAndUsingDirective(imported, usingDirective, dependencies: default));
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
                                        declarationBinder.ReportDiagnosticsIfObsolete(diagnostics, importedType, usingDirective.Name, hasBaseReceiver: false);

                                        uniqueUsings.Add(importedType);
                                        usings.Add(new NamespaceOrTypeAndUsingDirective(importedType, usingDirective, directiveDiagnostics.DependenciesBag.ToImmutableArray()));
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

                            diagnostics.AddRange(directiveDiagnostics.DiagnosticBag);
                            directiveDiagnostics.Free();
                        }
                    }

                    uniqueUsings.Free();

                    if (diagnostics.IsEmptyWithoutResolution)
                    {
                        diagnostics = null;
                    }

                    return new UsingsAndDiagnostics() { UsingAliases = usingAliases.ToImmutableAndFree(), UsingAliasesMap = usingAliasesMap?.ToImmutable(), UsingNamespacesOrTypes = usings.ToImmutableAndFree(), Diagnostics = diagnostics };
                }
            }

            internal Imports GetImports(SourceNamespaceSymbol declaringSymbol, CSharpSyntaxNode declarationSyntax, ConsList<TypeSymbol>? basesBeingResolved)
            {
                if (_lazyImports is null)
                {
                    Interlocked.CompareExchange(ref _lazyImports,
                                                Imports.Create(GetUsingAliasesMap(declaringSymbol, declarationSyntax, basesBeingResolved),
                                                               GetUsingNamespacesOrTypes(declaringSymbol, declarationSyntax, basesBeingResolved),
                                                               GetExternAliases(declaringSymbol, declarationSyntax)),
                                                null);
                }

                return _lazyImports;
            }

            internal void Complete(SourceNamespaceSymbol declaringSymbol, SyntaxReference declarationSyntax, CancellationToken cancellationToken)
            {
                var externAliasesAndDiagnostics = _lazyExternAliases ?? GetExternAliasesAndDiagnostics(declaringSymbol, (CSharpSyntaxNode)declarationSyntax.GetSyntax());
                cancellationToken.ThrowIfCancellationRequested();

                var usingsAndDiagnostics = _lazyUsings ?? GetUsingsAndDiagnostics(declaringSymbol, (CSharpSyntaxNode)declarationSyntax.GetSyntax(), basesBeingResolved: null);
                cancellationToken.ThrowIfCancellationRequested();

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
                                    Validate(declaringSymbol, externAliasesAndDiagnostics, usingsAndDiagnostics);
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

            private static void Validate(SourceNamespaceSymbol declaringSymbol, ExternAliasesAndDiagnostics externAliasesAndDiagnostics, UsingsAndDiagnostics usingsAndDiagnostics)
            {
                var compilation = declaringSymbol.DeclaringCompilation;
                DiagnosticBag semanticDiagnostics = compilation.DeclarationDiagnostics;

                // Check constraints within named aliases.
                var diagnostics = BindingDiagnosticBag.GetInstance();
                Debug.Assert(diagnostics.DiagnosticBag is object);
                Debug.Assert(diagnostics.DependenciesBag is object);

                if (usingsAndDiagnostics.UsingAliasesMap is object)
                {
                    // Force resolution of named aliases.
                    foreach (var (_, alias) in usingsAndDiagnostics.UsingAliasesMap)
                    {
                        NamespaceOrTypeSymbol target = alias.Alias.GetAliasTarget(basesBeingResolved: null);

                        diagnostics.Clear();
                        if (alias.Alias is AliasSymbolFromSyntax aliasFromSyntax)
                        {
                            diagnostics.AddRange(aliasFromSyntax.AliasTargetDiagnostics);
                        }

                        alias.Alias.CheckConstraints(diagnostics);

                        semanticDiagnostics.AddRange(diagnostics.DiagnosticBag);
                        recordImportDependencies(alias.UsingDirective!, target);
                    }
                }

                var corLibrary = compilation.SourceAssembly.CorLibrary;
                var conversions = new TypeConversions(corLibrary);
                foreach (var @using in usingsAndDiagnostics.UsingNamespacesOrTypes)
                {
                    diagnostics.Clear();
                    diagnostics.AddDependencies(@using.Dependencies);

                    NamespaceOrTypeSymbol target = @using.NamespaceOrType;

                    // Check if `using static` directives meet constraints.
                    UsingDirectiveSyntax usingDirective = @using.UsingDirective!;
                    if (target.IsType)
                    {
                        var typeSymbol = (TypeSymbol)target;
                        var location = usingDirective.Name.Location;
                        typeSymbol.CheckAllConstraints(compilation, conversions, location, diagnostics);
                    }

                    semanticDiagnostics.AddRange(diagnostics.DiagnosticBag);
                    recordImportDependencies(usingDirective, target);
                }

                // Force resolution of extern aliases.
                foreach (var alias in externAliasesAndDiagnostics.ExternAliases)
                {
                    if (alias.SkipInLookup)
                    {
                        continue;
                    }

                    var target = (NamespaceSymbol)alias.Alias.GetAliasTarget(null);
                    Debug.Assert(target.IsGlobalNamespace);

                    if (alias.Alias is AliasSymbolFromSyntax aliasFromSyntax)
                    {
                        semanticDiagnostics.AddRange(aliasFromSyntax.AliasTargetDiagnostics.DiagnosticBag!);
                    }

                    if (!Compilation.ReportUnusedImportsInTree(alias.ExternAliasDirective!.SyntaxTree))
                    {
                        diagnostics.Clear();
                        diagnostics.AddAssembliesUsedByNamespaceReference(target);
                        compilation.AddUsedAssemblies(diagnostics.DependenciesBag);
                    }
                }

                semanticDiagnostics.AddRange(externAliasesAndDiagnostics.Diagnostics);

                if (usingsAndDiagnostics.Diagnostics?.IsEmptyWithoutResolution == false)
                {
                    semanticDiagnostics.AddRange(usingsAndDiagnostics.Diagnostics.AsEnumerable());
                }

                diagnostics.Free();

                void recordImportDependencies(UsingDirectiveSyntax usingDirective, NamespaceOrTypeSymbol target)
                {
                    if (Compilation.ReportUnusedImportsInTree(usingDirective.SyntaxTree))
                    {
                        compilation.RecordImportDependencies(usingDirective, diagnostics.DependenciesBag.ToImmutableArray());
                    }
                    else
                    {
                        if (target.IsNamespace)
                        {
                            diagnostics.AddAssembliesUsedByNamespaceReference((NamespaceSymbol)target);
                        }

                        compilation.AddUsedAssemblies(diagnostics.DependenciesBag);
                    }
                }
            }

            private class ExternAliasesAndDiagnostics
            {
                public static readonly ExternAliasesAndDiagnostics Empty = new ExternAliasesAndDiagnostics() { ExternAliases = ImmutableArray<AliasAndExternAliasDirective>.Empty, Diagnostics = ImmutableArray<Diagnostic>.Empty };

                public ImmutableArray<AliasAndExternAliasDirective> ExternAliases { get; init; }
                public ImmutableArray<Diagnostic> Diagnostics { get; init; }
            }

            private class UsingsAndDiagnostics
            {
                public static readonly UsingsAndDiagnostics Empty =
                    new UsingsAndDiagnostics()
                    {
                        UsingAliases = ImmutableArray<AliasAndUsingDirective>.Empty,
                        UsingAliasesMap = null,
                        UsingNamespacesOrTypes = ImmutableArray<NamespaceOrTypeAndUsingDirective>.Empty,
                        Diagnostics = null
                    };

                public ImmutableArray<AliasAndUsingDirective> UsingAliases { get; init; }
                public ImmutableDictionary<string, AliasAndUsingDirective>? UsingAliasesMap { get; init; }
                public ImmutableArray<NamespaceOrTypeAndUsingDirective> UsingNamespacesOrTypes { get; init; }
                public DiagnosticBag? Diagnostics { get; init; }
            }
        }
    }
}
