// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using Microsoft.CodeAnalysis.AddImports;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Utilities;
using Microsoft.CodeAnalysis.Host.Mef;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.AddImports
{
    [ExportLanguageService(typeof(IAddImportsService), LanguageNames.CSharp), Shared]
    internal class CSharpAddImportsService : AbstractAddImportsService<
        CompilationUnitSyntax, NamespaceDeclarationSyntax, UsingDirectiveSyntax, ExternAliasDirectiveSyntax>
    {
        [ImportingConstructor]
        public CSharpAddImportsService()
        {
        }

        // C# doesn't have global imports.
        protected override ImmutableArray<SyntaxNode> GetGlobalImports(Compilation compilation)
            => ImmutableArray<SyntaxNode>.Empty;

        protected override SyntaxNode? GetAlias(UsingDirectiveSyntax usingOrAlias)
            => usingOrAlias.Alias;

        protected override bool IsStaticUsing(UsingDirectiveSyntax usingOrAlias)
            => usingOrAlias.StaticKeyword != default;

        protected override SyntaxNode Rewrite(
            ExternAliasDirectiveSyntax[] externAliases,
            UsingDirectiveSyntax[] usingDirectives,
            UsingDirectiveSyntax[] staticUsingDirectives,
            UsingDirectiveSyntax[] aliasDirectives,
            SyntaxNode externContainer,
            SyntaxNode usingContainer,
            SyntaxNode staticUsingContainer,
            SyntaxNode aliasContainer,
            bool placeSystemNamespaceFirst,
            SyntaxNode root,
            CancellationToken cancellationToken)
        {
            var rewriter = new Rewriter(
                externAliases, usingDirectives, staticUsingDirectives,
                aliasDirectives, externContainer, usingContainer,
                staticUsingContainer, aliasContainer, placeSystemNamespaceFirst, cancellationToken);

            var newRoot = rewriter.Visit(root);
            return newRoot;
        }

        protected override SyntaxList<UsingDirectiveSyntax> GetUsingsAndAliases(SyntaxNode node)
            => node switch
            {
                CompilationUnitSyntax c => c.Usings,
                NamespaceDeclarationSyntax n => n.Usings,
                _ => default,
            };

        protected override SyntaxList<ExternAliasDirectiveSyntax> GetExterns(SyntaxNode node)
            => node switch
            {
                CompilationUnitSyntax c => c.Externs,
                NamespaceDeclarationSyntax n => n.Externs,
                _ => default,
            };

        protected override bool IsEquivalentImport(SyntaxNode a, SyntaxNode b)
        {
            return a.IsEquivalentTo(b, topLevel: false);
        }

        private class Rewriter : CSharpSyntaxRewriter
        {
            private readonly bool _placeSystemNamespaceFirst;
            private readonly CancellationToken _cancellationToken;
            private readonly SyntaxNode _externContainer;
            private readonly SyntaxNode _usingContainer;
            private readonly SyntaxNode _aliasContainer;
            private readonly SyntaxNode _staticUsingContainer;

            private readonly UsingDirectiveSyntax[] _aliasDirectives;
            private readonly ExternAliasDirectiveSyntax[] _externAliases;
            private readonly UsingDirectiveSyntax[] _usingDirectives;
            private readonly UsingDirectiveSyntax[] _staticUsingDirectives;

            public Rewriter(
                ExternAliasDirectiveSyntax[] externAliases,
                UsingDirectiveSyntax[] usingDirectives,
                UsingDirectiveSyntax[] staticUsingDirectives,
                UsingDirectiveSyntax[] aliasDirectives,
                SyntaxNode externContainer,
                SyntaxNode usingContainer,
                SyntaxNode aliasContainer,
                SyntaxNode staticUsingContainer,
                bool placeSystemNamespaceFirst,
                CancellationToken cancellationToken)
            {
                _externAliases = externAliases;
                _usingDirectives = usingDirectives;
                _staticUsingDirectives = staticUsingDirectives;
                _aliasDirectives = aliasDirectives;
                _externContainer = externContainer;
                _usingContainer = usingContainer;
                _aliasContainer = aliasContainer;
                _staticUsingContainer = staticUsingContainer;
                _placeSystemNamespaceFirst = placeSystemNamespaceFirst;
                _cancellationToken = cancellationToken;
            }

            public override SyntaxNode VisitNamespaceDeclaration(NamespaceDeclarationSyntax node)
            {
                // recurse downwards so we visit inner namespaces first.
                var rewritten = (NamespaceDeclarationSyntax)(base.VisitNamespaceDeclaration(node) ?? throw ExceptionUtilities.Unreachable);

                if (!node.CanAddUsingDirectives(_cancellationToken))
                {
                    return rewritten;
                }

                if (node == _aliasContainer)
                {
                    rewritten = rewritten.AddUsingDirectives(_aliasDirectives, _placeSystemNamespaceFirst);
                }

                if (node == _usingContainer)
                {
                    rewritten = rewritten.AddUsingDirectives(_usingDirectives, _placeSystemNamespaceFirst);
                }

                if (node == _staticUsingContainer)
                {
                    rewritten = rewritten.AddUsingDirectives(_staticUsingDirectives, _placeSystemNamespaceFirst);
                }

                if (node == _externContainer)
                {
                    rewritten = rewritten.AddExterns(_externAliases);
                }

                return rewritten;
            }

            public override SyntaxNode VisitCompilationUnit(CompilationUnitSyntax node)
            {
                // recurse downwards so we visit inner namespaces first.
                var rewritten = (CompilationUnitSyntax)(base.VisitCompilationUnit(node) ?? throw ExceptionUtilities.Unreachable);

                if (!node.CanAddUsingDirectives(_cancellationToken))
                {
                    return rewritten;
                }

                if (node == _aliasContainer)
                {
                    rewritten = rewritten.AddUsingDirectives(_aliasDirectives, _placeSystemNamespaceFirst);
                }

                if (node == _usingContainer)
                {
                    rewritten = rewritten.AddUsingDirectives(_usingDirectives, _placeSystemNamespaceFirst);
                }

                if (node == _staticUsingContainer)
                {
                    rewritten = rewritten.AddUsingDirectives(_staticUsingDirectives, _placeSystemNamespaceFirst);
                }

                if (node == _externContainer)
                {
                    rewritten = rewritten.AddExterns(_externAliases);
                }

                return rewritten;
            }
        }
    }
}
