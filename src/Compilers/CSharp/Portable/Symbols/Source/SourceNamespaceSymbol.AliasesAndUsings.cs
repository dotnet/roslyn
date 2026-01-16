// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Collections;
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
                        var result = GetGlobalUsingImports(basesBeingResolved);
#if DEBUG
                        var calculated = GetAliasesAndUsingsForAsserts(declarationSyntax).GetImports(this, declarationSyntax, basesBeingResolved);
                        if (result == Imports.Empty || calculated == Imports.Empty)
                        {
                            Debug.Assert((object)result == calculated);
                        }
                        else
                        {
                            Debug.Assert(result.ExternAliases.SequenceEqual(calculated.ExternAliases));
                            Debug.Assert(result.UsingAliases.SetEquals(calculated.UsingAliases));
                            Debug.Assert(result.Usings.SequenceEqual(calculated.Usings));
                        }
#endif

                        return result;
                    }
                    break;

                case BaseNamespaceDeclarationSyntax namespaceDecl:
                    if (!namespaceDecl.Externs.Any() && !namespaceDecl.Usings.Any())
                    {
#if DEBUG
                        Debug.Assert(GetAliasesAndUsingsForAsserts(declarationSyntax).GetImports(this, declarationSyntax, basesBeingResolved) == Imports.Empty);
#endif
                        return Imports.Empty;
                    }
                    break;

                default:
                    throw ExceptionUtilities.UnexpectedValue(declarationSyntax);
            }

            return GetAliasesAndUsings(declarationSyntax).GetImports(this, declarationSyntax, basesBeingResolved);
        }

        private AliasesAndUsings GetAliasesAndUsings(CSharpSyntaxNode declarationSyntax)
        {
            return GetAliasesAndUsings(GetMatchingNamespaceDeclaration(declarationSyntax));
        }

        private SingleNamespaceDeclaration GetMatchingNamespaceDeclaration(CSharpSyntaxNode declarationSyntax)
        {
            foreach (var declaration in _mergedDeclaration.Declarations)
            {
                var declarationSyntaxRef = declaration.SyntaxReference;
                if (declarationSyntaxRef.SyntaxTree != declarationSyntax.SyntaxTree)
                {
                    continue;
                }

                if (declarationSyntaxRef.GetSyntax() == declarationSyntax)
                {
                    return declaration;
                }
            }

            throw ExceptionUtilities.Unreachable();
        }

        private static AliasesAndUsings GetOrCreateAliasAndUsings(
            ref ImmutableDictionary<SingleNamespaceDeclaration, AliasesAndUsings> dictionary,
            SingleNamespaceDeclaration declaration)
        {
            return ImmutableInterlocked.GetOrAdd(
                ref dictionary,
                declaration,
                static _ => new AliasesAndUsings());
        }

        private AliasesAndUsings GetAliasesAndUsings(SingleNamespaceDeclaration declaration)
            => GetOrCreateAliasAndUsings(ref _aliasesAndUsings_doNotAccessDirectly, declaration);

#if DEBUG
        private AliasesAndUsings GetAliasesAndUsingsForAsserts(CSharpSyntaxNode declarationSyntax)
        {
            var singleDeclaration = GetMatchingNamespaceDeclaration(declarationSyntax);

            return singleDeclaration.HasExternAliases || singleDeclaration.HasGlobalUsings || singleDeclaration.HasUsings
                ? GetAliasesAndUsings(singleDeclaration)
                : GetOrCreateAliasAndUsings(ref _aliasesAndUsingsForAsserts_doNotAccessDirectly, singleDeclaration);
        }
#endif

        public ImmutableArray<AliasAndExternAliasDirective> GetExternAliases(CSharpSyntaxNode declarationSyntax)
        {
            switch (declarationSyntax)
            {
                case CompilationUnitSyntax compilationUnit:
                    if (!compilationUnit.Externs.Any())
                    {
#if DEBUG
                        Debug.Assert(GetAliasesAndUsingsForAsserts(declarationSyntax).GetExternAliases(this, declarationSyntax).IsEmpty);
#endif
                        return ImmutableArray<AliasAndExternAliasDirective>.Empty;
                    }
                    break;

                case BaseNamespaceDeclarationSyntax namespaceDecl:
                    if (!namespaceDecl.Externs.Any())
                    {
#if DEBUG
                        Debug.Assert(GetAliasesAndUsingsForAsserts(declarationSyntax).GetExternAliases(this, declarationSyntax).IsEmpty);
#endif
                        return ImmutableArray<AliasAndExternAliasDirective>.Empty;
                    }
                    break;

                default:
                    throw ExceptionUtilities.UnexpectedValue(declarationSyntax);
            }

            return GetAliasesAndUsings(declarationSyntax).GetExternAliases(this, declarationSyntax);
        }

        public ImmutableArray<AliasAndUsingDirective> GetUsingAliases(CSharpSyntaxNode declarationSyntax, ConsList<TypeSymbol>? basesBeingResolved)
        {
            switch (declarationSyntax)
            {
                case CompilationUnitSyntax compilationUnit:
                    if (!compilationUnit.Usings.Any())
                    {
#if DEBUG
                        Debug.Assert(GetAliasesAndUsingsForAsserts(declarationSyntax).GetUsingAliases(this, declarationSyntax, basesBeingResolved).IsEmpty);
#endif
                        return ImmutableArray<AliasAndUsingDirective>.Empty;
                    }
                    break;

                case BaseNamespaceDeclarationSyntax namespaceDecl:
                    if (!namespaceDecl.Usings.Any())
                    {
#if DEBUG
                        Debug.Assert(GetAliasesAndUsingsForAsserts(declarationSyntax).GetUsingAliases(this, declarationSyntax, basesBeingResolved).IsEmpty);
#endif
                        return ImmutableArray<AliasAndUsingDirective>.Empty;
                    }
                    break;

                default:
                    throw ExceptionUtilities.UnexpectedValue(declarationSyntax);
            }

            return GetAliasesAndUsings(declarationSyntax).GetUsingAliases(this, declarationSyntax, basesBeingResolved);
        }

        public ImmutableDictionary<string, AliasAndUsingDirective> GetUsingAliasesMap(CSharpSyntaxNode declarationSyntax, ConsList<TypeSymbol>? basesBeingResolved)
        {
            switch (declarationSyntax)
            {
                case CompilationUnitSyntax compilationUnit:
                    if (!compilationUnit.Usings.Any())
                    {
                        var result = GetGlobalUsingAliasesMap(basesBeingResolved);
#if DEBUG
                        Debug.Assert(result.SetEquals(GetAliasesAndUsingsForAsserts(declarationSyntax).GetUsingAliasesMap(this, declarationSyntax, basesBeingResolved)));
#endif
                        return result;
                    }
                    break;

                case BaseNamespaceDeclarationSyntax namespaceDecl:
                    if (!namespaceDecl.Usings.Any())
                    {
#if DEBUG
                        Debug.Assert(GetAliasesAndUsingsForAsserts(declarationSyntax).GetUsingAliasesMap(this, declarationSyntax, basesBeingResolved).IsEmpty);
#endif
                        return ImmutableDictionary<string, AliasAndUsingDirective>.Empty;
                    }
                    break;

                default:
                    throw ExceptionUtilities.UnexpectedValue(declarationSyntax);
            }

            return GetAliasesAndUsings(declarationSyntax).GetUsingAliasesMap(this, declarationSyntax, basesBeingResolved);
        }

        public ImmutableArray<NamespaceOrTypeAndUsingDirective> GetUsingNamespacesOrTypes(CSharpSyntaxNode declarationSyntax, ConsList<TypeSymbol>? basesBeingResolved)
        {
            switch (declarationSyntax)
            {
                case CompilationUnitSyntax compilationUnit:
                    if (!compilationUnit.Usings.Any())
                    {
                        var result = GetGlobalUsingNamespacesOrTypes(basesBeingResolved);
#if DEBUG
                        Debug.Assert(result.SequenceEqual(GetAliasesAndUsingsForAsserts(declarationSyntax).GetUsingNamespacesOrTypes(this, declarationSyntax, basesBeingResolved)));
#endif
                        return result;
                    }
                    break;

                case BaseNamespaceDeclarationSyntax namespaceDecl:
                    if (!namespaceDecl.Usings.Any())
                    {
#if DEBUG
                        Debug.Assert(GetAliasesAndUsingsForAsserts(declarationSyntax).GetUsingNamespacesOrTypes(this, declarationSyntax, basesBeingResolved).IsEmpty);
#endif
                        return ImmutableArray<NamespaceOrTypeAndUsingDirective>.Empty;
                    }
                    break;

                default:
                    throw ExceptionUtilities.UnexpectedValue(declarationSyntax);
            }

            return GetAliasesAndUsings(declarationSyntax).GetUsingNamespacesOrTypes(this, declarationSyntax, basesBeingResolved);
        }

        private Imports GetGlobalUsingImports(ConsList<TypeSymbol>? basesBeingResolved)
        {
            return GetMergedGlobalAliasesAndUsings(basesBeingResolved).Imports;
        }

        private ImmutableDictionary<string, AliasAndUsingDirective> GetGlobalUsingAliasesMap(ConsList<TypeSymbol>? basesBeingResolved)
        {
            return GetMergedGlobalAliasesAndUsings(basesBeingResolved).UsingAliasesMap!;
        }

        private ImmutableArray<NamespaceOrTypeAndUsingDirective> GetGlobalUsingNamespacesOrTypes(ConsList<TypeSymbol>? basesBeingResolved)
        {
            return GetMergedGlobalAliasesAndUsings(basesBeingResolved).UsingNamespacesOrTypes;
        }

        private MergedGlobalAliasesAndUsings GetMergedGlobalAliasesAndUsings(ConsList<TypeSymbol>? basesBeingResolved, CancellationToken cancellationToken = default)
        {
            if (_lazyMergedGlobalAliasesAndUsings is null)
            {
                if (!this.IsGlobalNamespace)
                {
                    _lazyMergedGlobalAliasesAndUsings = MergedGlobalAliasesAndUsings.Empty;
                }
                else
                {
                    ImmutableDictionary<string, AliasAndUsingDirective>? mergedAliases = null;
                    var mergedNamespacesOrTypes = ArrayBuilder<NamespaceOrTypeAndUsingDirective>.GetInstance();
                    var uniqueUsings = SpecializedSymbolCollections.GetPooledSymbolHashSetInstance<NamespaceOrTypeSymbol>();
                    var diagnostics = DiagnosticBag.GetInstance();

                    try
                    {
                        bool haveExternAliases = false;

                        foreach (var singleDeclaration in _mergedDeclaration.Declarations)
                        {
                            if (singleDeclaration.HasExternAliases)
                            {
                                haveExternAliases = true;
                            }

                            if (singleDeclaration.HasGlobalUsings)
                            {
                                var aliases = GetAliasesAndUsings(singleDeclaration).GetGlobalUsingAliasesMap(this, singleDeclaration.SyntaxReference, basesBeingResolved);

                                cancellationToken.ThrowIfCancellationRequested();

                                if (!aliases.IsEmpty)
                                {
                                    if (mergedAliases is null)
                                    {
                                        mergedAliases = aliases;
                                    }
                                    else
                                    {
                                        var builder = mergedAliases.ToBuilder();
                                        bool added = false;

                                        foreach (var pair in aliases)
                                        {
                                            if (builder.ContainsKey(pair.Key))
                                            {
                                                // The using alias '{0}' appeared previously in this namespace
                                                diagnostics.Add(ErrorCode.ERR_DuplicateAlias, pair.Value.Alias.GetFirstLocation(), pair.Key);
                                            }
                                            else
                                            {
                                                builder.Add(pair);
                                                added = true;
                                            }
                                        }

                                        if (added)
                                        {
                                            mergedAliases = builder.ToImmutable();
                                        }

                                        cancellationToken.ThrowIfCancellationRequested();
                                    }
                                }

                                var namespacesOrTypes = GetAliasesAndUsings(singleDeclaration).GetGlobalUsingNamespacesOrTypes(this, singleDeclaration.SyntaxReference, basesBeingResolved);

                                if (!namespacesOrTypes.IsEmpty)
                                {
                                    if (mergedNamespacesOrTypes.Count == 0)
                                    {
                                        mergedNamespacesOrTypes.AddRange(namespacesOrTypes);
                                        uniqueUsings.AddAll(namespacesOrTypes.Select(n => n.NamespaceOrType));
                                    }
                                    else
                                    {
                                        foreach (var namespaceOrType in namespacesOrTypes)
                                        {
                                            if (!uniqueUsings.Add(namespaceOrType.NamespaceOrType))
                                            {
                                                diagnostics.Add(ErrorCode.HDN_DuplicateWithGlobalUsing, namespaceOrType.UsingDirective!.NamespaceOrType.Location, namespaceOrType.NamespaceOrType);
                                            }
                                            else
                                            {
                                                mergedNamespacesOrTypes.Add(namespaceOrType);
                                            }
                                        }
                                    }
                                }

                                cancellationToken.ThrowIfCancellationRequested();
                            }
                        }

                        // Report a conflict between global using aliases and extern aliases from other compilation units
                        if (haveExternAliases && mergedAliases is object)
                        {
                            foreach (var singleDeclaration in _mergedDeclaration.Declarations)
                            {
                                if (singleDeclaration.HasExternAliases)
                                {
                                    var externAliases = GetAliasesAndUsings(singleDeclaration).GetExternAliases(this, singleDeclaration.SyntaxReference);
                                    var globalAliasesMap = ImmutableDictionary<string, AliasAndUsingDirective>.Empty;

                                    if (singleDeclaration.HasGlobalUsings)
                                    {
                                        globalAliasesMap = GetAliasesAndUsings(singleDeclaration).GetGlobalUsingAliasesMap(this, singleDeclaration.SyntaxReference, basesBeingResolved);
                                    }

                                    foreach (var externAlias in externAliases)
                                    {
                                        if (!externAlias.SkipInLookup &&
                                            !globalAliasesMap.ContainsKey(externAlias.Alias.Name) && // If we have a global alias with the same name declared in the same compilation unit, we already reported the conflict on the global alias.
                                            mergedAliases.ContainsKey(externAlias.Alias.Name))
                                        {
                                            // The using alias '{0}' appeared previously in this namespace
                                            diagnostics.Add(ErrorCode.ERR_DuplicateAlias, externAlias.Alias.GetFirstLocation(), externAlias.Alias.Name);
                                        }
                                    }
                                }
                            }
                        }

                        Interlocked.CompareExchange(ref _lazyMergedGlobalAliasesAndUsings,
                            new MergedGlobalAliasesAndUsings()
                            {
                                UsingAliasesMap = mergedAliases ?? ImmutableDictionary<string, AliasAndUsingDirective>.Empty,
                                UsingNamespacesOrTypes = mergedNamespacesOrTypes.ToImmutableAndFree(),
                                Diagnostics = diagnostics.ToReadOnlyAndFree()
                            },
                            null);

                        mergedNamespacesOrTypes = null;
                        diagnostics = null;
                    }
                    finally
                    {
                        uniqueUsings.Free();
                        mergedNamespacesOrTypes?.Free();
                        diagnostics?.Free();
                    }
                }
            }

            return _lazyMergedGlobalAliasesAndUsings;
        }

        private sealed class AliasesAndUsings
        {
            private ExternAliasesAndDiagnostics? _lazyExternAliases;
            private UsingsAndDiagnostics? _lazyGlobalUsings;
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

            internal ImmutableArray<AliasAndExternAliasDirective> GetExternAliases(SourceNamespaceSymbol declaringSymbol, SyntaxReference declarationSyntax)
            {
                return (_lazyExternAliases ?? GetExternAliasesAndDiagnostics(declaringSymbol, (CSharpSyntaxNode)declarationSyntax.GetSyntax())).ExternAliases;
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

                        case BaseNamespaceDeclarationSyntax namespaceDecl:
                            externAliasDirectives = namespaceDecl.Externs;
                            break;

                        default:
                            throw ExceptionUtilities.UnexpectedValue(declarationSyntax);
                    }

                    if (!externAliasDirectives.Any())
                    {
#if DEBUG
                        var diagnostics = DiagnosticBag.GetInstance();
                        var result = buildExternAliases(externAliasDirectives, declaringSymbol, diagnostics);
                        Debug.Assert(result.IsEmpty);
                        Debug.Assert(diagnostics.IsEmptyWithoutResolution);
                        diagnostics.Free();
#endif
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
                                    diagnostics.Add(ErrorCode.ERR_DuplicateAlias, existingAlias.Alias.GetFirstLocation(), existingAlias.Alias.Name);
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

            internal ImmutableArray<AliasAndUsingDirective> GetGlobalUsingAliases(SourceNamespaceSymbol declaringSymbol, CSharpSyntaxNode declarationSyntax, ConsList<TypeSymbol>? basesBeingResolved)
            {
                return GetGlobalUsingsAndDiagnostics(declaringSymbol, declarationSyntax, basesBeingResolved).UsingAliases;
            }

            internal ImmutableDictionary<string, AliasAndUsingDirective> GetUsingAliasesMap(SourceNamespaceSymbol declaringSymbol, CSharpSyntaxNode declarationSyntax, ConsList<TypeSymbol>? basesBeingResolved)
            {
                return GetUsingsAndDiagnostics(declaringSymbol, declarationSyntax, basesBeingResolved).UsingAliasesMap ?? ImmutableDictionary<string, AliasAndUsingDirective>.Empty;
            }

            internal ImmutableDictionary<string, AliasAndUsingDirective> GetGlobalUsingAliasesMap(SourceNamespaceSymbol declaringSymbol, SyntaxReference declarationSyntax, ConsList<TypeSymbol>? basesBeingResolved)
            {
                return (_lazyGlobalUsings ?? GetGlobalUsingsAndDiagnostics(declaringSymbol, (CSharpSyntaxNode)declarationSyntax.GetSyntax(), basesBeingResolved)).UsingAliasesMap ?? ImmutableDictionary<string, AliasAndUsingDirective>.Empty;
            }

            internal ImmutableArray<NamespaceOrTypeAndUsingDirective> GetUsingNamespacesOrTypes(SourceNamespaceSymbol declaringSymbol, CSharpSyntaxNode declarationSyntax, ConsList<TypeSymbol>? basesBeingResolved)
            {
                return GetUsingsAndDiagnostics(declaringSymbol, declarationSyntax, basesBeingResolved).UsingNamespacesOrTypes;
            }

            private UsingsAndDiagnostics GetUsingsAndDiagnostics(SourceNamespaceSymbol declaringSymbol, CSharpSyntaxNode declarationSyntax, ConsList<TypeSymbol>? basesBeingResolved)
            {
                return GetUsingsAndDiagnostics(ref _lazyUsings, declaringSymbol, declarationSyntax, basesBeingResolved, onlyGlobal: false);
            }

            internal ImmutableArray<NamespaceOrTypeAndUsingDirective> GetGlobalUsingNamespacesOrTypes(SourceNamespaceSymbol declaringSymbol, SyntaxReference declarationSyntax, ConsList<TypeSymbol>? basesBeingResolved)
            {
                return (_lazyGlobalUsings ?? GetGlobalUsingsAndDiagnostics(declaringSymbol, (CSharpSyntaxNode)declarationSyntax.GetSyntax(), basesBeingResolved)).UsingNamespacesOrTypes;
            }

            private UsingsAndDiagnostics GetGlobalUsingsAndDiagnostics(SourceNamespaceSymbol declaringSymbol, CSharpSyntaxNode declarationSyntax, ConsList<TypeSymbol>? basesBeingResolved)
            {
                return GetUsingsAndDiagnostics(ref _lazyGlobalUsings, declaringSymbol, declarationSyntax, basesBeingResolved, onlyGlobal: true);
            }

            private UsingsAndDiagnostics GetUsingsAndDiagnostics(ref UsingsAndDiagnostics? usings, SourceNamespaceSymbol declaringSymbol, CSharpSyntaxNode declarationSyntax, ConsList<TypeSymbol>? basesBeingResolved, bool onlyGlobal)
            {
                if (usings is null)
                {
                    SyntaxList<UsingDirectiveSyntax> usingDirectives;
                    bool? applyIsGlobalFilter;
                    switch (declarationSyntax)
                    {
                        case CompilationUnitSyntax compilationUnit:
                            applyIsGlobalFilter = onlyGlobal;
                            usingDirectives = compilationUnit.Usings;
                            break;

                        case BaseNamespaceDeclarationSyntax namespaceDecl:
                            Debug.Assert(!onlyGlobal);
                            applyIsGlobalFilter = null; // Global Using directives are not allowed in namespaces, treat them as regular, an error is reported elsewhere.
                            usingDirectives = namespaceDecl.Usings;
                            break;

                        default:
                            throw ExceptionUtilities.UnexpectedValue(declarationSyntax);
                    }

                    UsingsAndDiagnostics result;
                    if (!usingDirectives.Any())
                    {
                        if (applyIsGlobalFilter != false)
                        {
#if DEBUG
                            var calculated = buildUsings(usingDirectives, declaringSymbol, declarationSyntax, applyIsGlobalFilter, basesBeingResolved);
                            Debug.Assert(calculated.UsingAliases.IsEmpty);
                            Debug.Assert(calculated.UsingAliasesMap?.IsEmpty ?? true);
                            Debug.Assert(calculated.UsingNamespacesOrTypes.IsEmpty);
                            Debug.Assert(calculated.Diagnostics?.IsEmptyWithoutResolution ?? true);
#endif
                            result = UsingsAndDiagnostics.Empty;
                        }
                        else
                        {
                            result = new UsingsAndDiagnostics()
                            {
                                UsingAliases = GetGlobalUsingAliases(declaringSymbol, declarationSyntax, basesBeingResolved),
                                UsingAliasesMap = declaringSymbol.GetGlobalUsingAliasesMap(basesBeingResolved),
                                UsingNamespacesOrTypes = declaringSymbol.GetGlobalUsingNamespacesOrTypes(basesBeingResolved),
                                Diagnostics = null
                            };
#if DEBUG
                            var calculated = buildUsings(usingDirectives, declaringSymbol, declarationSyntax, applyIsGlobalFilter, basesBeingResolved);
                            Debug.Assert(calculated.UsingAliases.SequenceEqual(result.UsingAliases));
                            Debug.Assert((calculated.UsingAliasesMap ?? ImmutableDictionary<string, AliasAndUsingDirective>.Empty).SetEquals(result.UsingAliasesMap ?? ImmutableDictionary<string, AliasAndUsingDirective>.Empty));
                            Debug.Assert(calculated.UsingNamespacesOrTypes.SequenceEqual(result.UsingNamespacesOrTypes));
                            Debug.Assert(calculated.Diagnostics?.IsEmptyWithoutResolution ?? true);
#endif
                        }
                    }
                    else
                    {
                        result = buildUsings(usingDirectives, declaringSymbol, declarationSyntax, applyIsGlobalFilter, basesBeingResolved);
                    }

                    Interlocked.CompareExchange(ref usings, result, null);
                }

                return usings;

                UsingsAndDiagnostics buildUsings(
                    SyntaxList<UsingDirectiveSyntax> usingDirectives,
                    SourceNamespaceSymbol declaringSymbol,
                    CSharpSyntaxNode declarationSyntax,
                    bool? applyIsGlobalFilter,
                    ConsList<TypeSymbol>? basesBeingResolved)
                {
                    // define all of the extern aliases first. They may be used by the target of a using
                    var externAliases = GetExternAliases(declaringSymbol, declarationSyntax);
                    var globalUsingAliasesMap = ImmutableDictionary<string, AliasAndUsingDirective>.Empty;
                    var globalUsingNamespacesOrTypes = ImmutableArray<NamespaceOrTypeAndUsingDirective>.Empty;
                    var globalUsingAliases = ImmutableArray<AliasAndUsingDirective>.Empty;

                    if (applyIsGlobalFilter == false)
                    {
                        // Define all of the global usings. They may cause conflicts, etc.
                        globalUsingAliasesMap = declaringSymbol.GetGlobalUsingAliasesMap(basesBeingResolved);
                        globalUsingNamespacesOrTypes = declaringSymbol.GetGlobalUsingNamespacesOrTypes(basesBeingResolved);
                        globalUsingAliases = GetGlobalUsingAliases(declaringSymbol, declarationSyntax, basesBeingResolved);
                    }

                    var diagnostics = new DiagnosticBag();

                    var compilation = declaringSymbol.DeclaringCompilation;

                    ArrayBuilder<NamespaceOrTypeAndUsingDirective>? usings = null;
                    ImmutableDictionary<string, AliasAndUsingDirective>.Builder? usingAliasesMap = null;
                    ArrayBuilder<AliasAndUsingDirective>? usingAliases = null;

                    // A binder that contains the extern aliases but not the usings. The resolution of the target of a using directive or alias 
                    // should not make use of other peer usings.
                    Binder? declarationBinder = null;

                    PooledHashSet<NamespaceOrTypeSymbol>? uniqueUsings = null;
                    PooledHashSet<NamespaceOrTypeSymbol>? uniqueGlobalUsings = null;

                    foreach (var usingDirective in usingDirectives)
                    {
                        if (applyIsGlobalFilter.HasValue && usingDirective.GlobalKeyword.IsKind(SyntaxKind.GlobalKeyword) != applyIsGlobalFilter.GetValueOrDefault())
                        {
                            continue;
                        }

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

                            SourceMemberContainerTypeSymbol.ReportReservedTypeName(identifier.Text, compilation, diagnostics, location);

                            string identifierValueText = identifier.ValueText;
                            bool skipInLookup = false;

                            if (usingAliasesMap?.ContainsKey(identifierValueText) ?? globalUsingAliasesMap.ContainsKey(identifierValueText))
                            {
                                skipInLookup = true;

                                // Suppress diagnostics if we're already broken.
                                if (!usingDirective.NamespaceOrType.IsMissing)
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

                            if (usingAliases is null)
                            {
                                usingAliases = ArrayBuilder<AliasAndUsingDirective>.GetInstance();
                                usingAliases.AddRange(globalUsingAliases);
                            }

                            usingAliases.Add(aliasAndDirective);

                            if (!skipInLookup)
                            {
                                if (usingAliasesMap == null)
                                {
                                    usingAliasesMap = globalUsingAliasesMap.ToBuilder();
                                }

                                usingAliasesMap.Add(identifierValueText, aliasAndDirective);
                            }
                        }
                        else
                        {
                            if (usingDirective.NamespaceOrType.IsMissing)
                            {
                                //don't try to lookup namespaces inserted by parser error recovery
                                continue;
                            }

                            var flags = BinderFlags.SuppressConstraintChecks;
                            if (usingDirective.UnsafeKeyword != default)
                            {
                                var unsafeKeywordLocation = usingDirective.UnsafeKeyword.GetLocation();
                                if (usingDirective.StaticKeyword == default)
                                {
                                    diagnostics.Add(ErrorCode.ERR_BadUnsafeInUsingDirective, unsafeKeywordLocation);
                                }
                                else
                                {
                                    MessageID.IDS_FeatureUsingTypeAlias.CheckFeatureAvailability(diagnostics, usingDirective, unsafeKeywordLocation);
                                    declaringSymbol.CheckUnsafeModifier(DeclarationModifiers.Unsafe, unsafeKeywordLocation, diagnostics);
                                }

                                flags |= BinderFlags.UnsafeRegion;
                            }
                            else
                            {
                                // Prior to C#12, allow the using static type to be an unsafe region.  This allows us to
                                // maintain compat with prior versions of the compiler that allowed `using static
                                // List<int*[]>;` to be written.  In 12.0 and onwards though, we require the code to
                                // explicitly contain the `unsafe` keyword.
                                if (!compilation.IsFeatureEnabled(MessageID.IDS_FeatureUsingTypeAlias))
                                    flags |= BinderFlags.UnsafeRegion;
                            }

                            var directiveDiagnostics = BindingDiagnosticBag.GetInstance();
                            Debug.Assert(directiveDiagnostics.DiagnosticBag is object);
                            Debug.Assert(directiveDiagnostics.DependenciesBag is object);

                            declarationBinder ??= compilation.GetBinderFactory(declarationSyntax.SyntaxTree).GetBinder(usingDirective.NamespaceOrType).WithAdditionalFlags(flags);
                            var imported = declarationBinder.BindNamespaceOrTypeSymbol(usingDirective.NamespaceOrType, directiveDiagnostics, basesBeingResolved).NamespaceOrTypeSymbol;
                            bool addDirectiveDiagnostics = true;

                            if (imported.Kind == SymbolKind.Namespace)
                            {
                                Debug.Assert(directiveDiagnostics.DependenciesBag.IsEmpty());

                                if (usingDirective.StaticKeyword != default(SyntaxToken))
                                {
                                    diagnostics.Add(ErrorCode.ERR_BadUsingType, usingDirective.NamespaceOrType.Location, imported);
                                }
                                else if (!getOrCreateUniqueUsings(ref uniqueUsings, globalUsingNamespacesOrTypes).Add(imported))
                                {
                                    diagnostics.Add(!globalUsingNamespacesOrTypes.IsEmpty && getOrCreateUniqueGlobalUsingsNotInTree(ref uniqueGlobalUsings, globalUsingNamespacesOrTypes, declarationSyntax.SyntaxTree).Contains(imported) ?
                                                            ErrorCode.HDN_DuplicateWithGlobalUsing :
                                                            ErrorCode.WRN_DuplicateUsing,
                                                    usingDirective.NamespaceOrType.Location, imported);
                                }
                                else
                                {
                                    getOrCreateUsingsBuilder(ref usings, globalUsingNamespacesOrTypes).Add(new NamespaceOrTypeAndUsingDirective(imported, usingDirective, dependencies: default));
                                }
                            }
                            else if (imported.Kind == SymbolKind.NamedType)
                            {
                                if (usingDirective.StaticKeyword == default(SyntaxToken))
                                {
                                    diagnostics.Add(ErrorCode.ERR_BadUsingNamespace, usingDirective.NamespaceOrType.Location, imported);
                                }
                                else
                                {
                                    var importedType = (NamedTypeSymbol)imported;
                                    if (usingDirective.GlobalKeyword != default(SyntaxToken) && importedType.HasFileLocalTypes())
                                    {
                                        diagnostics.Add(ErrorCode.ERR_GlobalUsingStaticFileType, usingDirective.NamespaceOrType.Location, imported);
                                    }

                                    if (!getOrCreateUniqueUsings(ref uniqueUsings, globalUsingNamespacesOrTypes).Add(importedType))
                                    {
                                        diagnostics.Add(!globalUsingNamespacesOrTypes.IsEmpty && getOrCreateUniqueGlobalUsingsNotInTree(ref uniqueGlobalUsings, globalUsingNamespacesOrTypes, declarationSyntax.SyntaxTree).Contains(imported) ?
                                                            ErrorCode.HDN_DuplicateWithGlobalUsing :
                                                            ErrorCode.WRN_DuplicateUsing,
                                                        usingDirective.NamespaceOrType.Location, importedType);
                                    }
                                    else
                                    {
                                        declarationBinder.ReportDiagnosticsIfObsolete(diagnostics, importedType, usingDirective.NamespaceOrType, hasBaseReceiver: false);
                                        Binder.AssertNotUnsafeMemberAccess(importedType);

                                        getOrCreateUsingsBuilder(ref usings, globalUsingNamespacesOrTypes).Add(new NamespaceOrTypeAndUsingDirective(importedType, usingDirective, directiveDiagnostics.DependenciesBag.ToImmutableArray()));
                                    }
                                }
                            }
                            else if (imported.Kind is SymbolKind.ArrayType or SymbolKind.PointerType or SymbolKind.FunctionPointerType or SymbolKind.DynamicType)
                            {
                                diagnostics.Add(ErrorCode.ERR_BadUsingStaticType, usingDirective.NamespaceOrType.Location, imported.GetKindText());

                                // Don't bother adding sub diagnostics (like that an unsafe type was referenced).  The
                                // primary thing we want to report is simply that the using-static points to something
                                // entirely invalid.
                                addDirectiveDiagnostics = false;
                            }
                            else if (imported.Kind != SymbolKind.ErrorType)
                            {
                                // Do not report additional error if the symbol itself is erroneous.

                                // error: '<symbol>' is a '<symbol kind>' but is used as 'type or namespace'
                                diagnostics.Add(ErrorCode.ERR_BadSKknown, usingDirective.NamespaceOrType.Location,
                                    usingDirective.NamespaceOrType,
                                    imported.GetKindText(),
                                    MessageID.IDS_SK_TYPE_OR_NAMESPACE.Localize());
                            }

                            if (addDirectiveDiagnostics)
                            {
                                diagnostics.AddRange(directiveDiagnostics.DiagnosticBag);
                            }

                            directiveDiagnostics.Free();
                        }
                    }

                    uniqueUsings?.Free();
                    uniqueGlobalUsings?.Free();

                    if (diagnostics.IsEmptyWithoutResolution)
                    {
                        diagnostics = null;
                    }

                    return new UsingsAndDiagnostics()
                    {
                        UsingAliases = usingAliases?.ToImmutableAndFree() ?? globalUsingAliases,
                        UsingAliasesMap = usingAliasesMap?.ToImmutable() ?? globalUsingAliasesMap,
                        UsingNamespacesOrTypes = usings?.ToImmutableAndFree() ?? globalUsingNamespacesOrTypes,
                        Diagnostics = diagnostics
                    };

                    static PooledHashSet<NamespaceOrTypeSymbol> getOrCreateUniqueUsings(ref PooledHashSet<NamespaceOrTypeSymbol>? uniqueUsings, ImmutableArray<NamespaceOrTypeAndUsingDirective> globalUsingNamespacesOrTypes)
                    {
                        if (uniqueUsings is null)
                        {
                            uniqueUsings = SpecializedSymbolCollections.GetPooledSymbolHashSetInstance<NamespaceOrTypeSymbol>();
                            uniqueUsings.AddAll(globalUsingNamespacesOrTypes.Select(n => n.NamespaceOrType));
                        }

                        return uniqueUsings;
                    }

                    static PooledHashSet<NamespaceOrTypeSymbol> getOrCreateUniqueGlobalUsingsNotInTree(ref PooledHashSet<NamespaceOrTypeSymbol>? uniqueUsings, ImmutableArray<NamespaceOrTypeAndUsingDirective> globalUsingNamespacesOrTypes, SyntaxTree tree)
                    {
                        if (uniqueUsings is null)
                        {
                            uniqueUsings = SpecializedSymbolCollections.GetPooledSymbolHashSetInstance<NamespaceOrTypeSymbol>();
                            uniqueUsings.AddAll(globalUsingNamespacesOrTypes.Where(n => n.UsingDirectiveReference?.SyntaxTree != tree).Select(n => n.NamespaceOrType));
                        }

                        return uniqueUsings;
                    }

                    static ArrayBuilder<NamespaceOrTypeAndUsingDirective> getOrCreateUsingsBuilder(ref ArrayBuilder<NamespaceOrTypeAndUsingDirective>? usings, ImmutableArray<NamespaceOrTypeAndUsingDirective> globalUsingNamespacesOrTypes)
                    {
                        if (usings is null)
                        {
                            usings = ArrayBuilder<NamespaceOrTypeAndUsingDirective>.GetInstance();
                            usings.AddRange(globalUsingNamespacesOrTypes);
                        }

                        return usings;
                    }
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
                var externAliasesAndDiagnostics = _lazyExternAliases ?? GetExternAliasesAndDiagnostics(declaringSymbol, (CSharpSyntaxNode)declarationSyntax.GetSyntax(cancellationToken));
                cancellationToken.ThrowIfCancellationRequested();

                var globalUsingsAndDiagnostics = _lazyGlobalUsings ??
                                                (declaringSymbol.IsGlobalNamespace ?
                                                     GetGlobalUsingsAndDiagnostics(declaringSymbol, (CSharpSyntaxNode)declarationSyntax.GetSyntax(cancellationToken), basesBeingResolved: null) :
                                                     UsingsAndDiagnostics.Empty);
                cancellationToken.ThrowIfCancellationRequested();

                var usingsAndDiagnostics = _lazyUsings ?? GetUsingsAndDiagnostics(declaringSymbol, (CSharpSyntaxNode)declarationSyntax.GetSyntax(cancellationToken), basesBeingResolved: null);
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
                                    Validate(declaringSymbol, declarationSyntax, externAliasesAndDiagnostics, usingsAndDiagnostics, globalUsingsAndDiagnostics.Diagnostics);
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

            private static void Validate(SourceNamespaceSymbol declaringSymbol, SyntaxReference declarationSyntax, ExternAliasesAndDiagnostics externAliasesAndDiagnostics, UsingsAndDiagnostics usingsAndDiagnostics, DiagnosticBag? globalUsingDiagnostics)
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
                        if (alias.UsingDirectiveReference!.SyntaxTree != declarationSyntax.SyntaxTree)
                        {
                            // Must be a global alias from a different compilation unit
                            Debug.Assert(declaringSymbol.IsGlobalNamespace);
                            continue;
                        }

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
                var conversions = corLibrary.TypeConversions;
                foreach (var @using in usingsAndDiagnostics.UsingNamespacesOrTypes)
                {
                    if (@using.UsingDirectiveReference!.SyntaxTree != declarationSyntax.SyntaxTree)
                    {
                        // Must be a global using directive from a different compilation unit
                        Debug.Assert(declaringSymbol.IsGlobalNamespace);
                        continue;
                    }

                    diagnostics.Clear();
                    diagnostics.AddDependencies(@using.Dependencies);

                    NamespaceOrTypeSymbol target = @using.NamespaceOrType;

                    // Check if `using static` directives meet constraints.
                    UsingDirectiveSyntax usingDirective = @using.UsingDirective!;
                    if (target.IsType)
                    {
                        var typeSymbol = (TypeSymbol)target;
                        var location = usingDirective.NamespaceOrType.Location;
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

                if (globalUsingDiagnostics?.IsEmptyWithoutResolution == false)
                {
                    semanticDiagnostics.AddRange(globalUsingDiagnostics.AsEnumerable());
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

        private class MergedGlobalAliasesAndUsings
        {
            private Imports? _lazyImports;

            /// <summary>
            /// Completion state that tracks whether validation was done/not done/currently in process. 
            /// </summary>
            private SymbolCompletionState _state;

            public static readonly MergedGlobalAliasesAndUsings Empty =
                new MergedGlobalAliasesAndUsings()
                {
                    UsingAliasesMap = ImmutableDictionary<string, AliasAndUsingDirective>.Empty,
                    UsingNamespacesOrTypes = ImmutableArray<NamespaceOrTypeAndUsingDirective>.Empty,
                    Diagnostics = ImmutableArray<Diagnostic>.Empty,
                    _lazyImports = Imports.Empty
                };

            public ImmutableDictionary<string, AliasAndUsingDirective>? UsingAliasesMap { get; init; }
            public ImmutableArray<NamespaceOrTypeAndUsingDirective> UsingNamespacesOrTypes { get; init; }
            public ImmutableArray<Diagnostic> Diagnostics { get; init; }

            public Imports Imports
            {
                get
                {
                    if (_lazyImports is null)
                    {
                        Interlocked.CompareExchange(ref _lazyImports,
                                                    Imports.Create(UsingAliasesMap ?? ImmutableDictionary<string, AliasAndUsingDirective>.Empty,
                                                                   UsingNamespacesOrTypes,
                                                                   ImmutableArray<AliasAndExternAliasDirective>.Empty),
                                                    null);
                    }

                    return _lazyImports;
                }
            }

            internal void Complete(SourceNamespaceSymbol declaringSymbol, CancellationToken cancellationToken)
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
                                    if (!Diagnostics.IsDefaultOrEmpty)
                                    {
                                        var compilation = declaringSymbol.DeclaringCompilation;
                                        DiagnosticBag semanticDiagnostics = compilation.DeclarationDiagnostics;
                                        semanticDiagnostics.AddRange(Diagnostics);
                                    }

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
        }
    }
}
