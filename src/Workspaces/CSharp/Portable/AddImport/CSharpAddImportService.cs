// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using Microsoft.CodeAnalysis.AddImport;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Host.Mef;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.AddImport
{
    [ExportLanguageService(typeof(IAddImportService), LanguageNames.CSharp), Shared]
    internal class CSharpAddImportService : IAddImportService
    {
        public SyntaxNode AddImports(
            SyntaxNode root,
            SyntaxNode contextLocation,
            IEnumerable<SyntaxNode> newImports,
            bool placeSystemNamespaceFirst)
        {
            contextLocation = contextLocation ?? root;
            var contextSpine = contextLocation.AncestorsAndSelf().ToSet();

            // var compilationUnit = (CompilationUnitSyntax)root;

            var externAliases = newImports.OfType<ExternAliasDirectiveSyntax>().ToArray();
            var usingDirectives = newImports.OfType<UsingDirectiveSyntax>().Where(u => u.Alias == null).ToArray();
            var aliasDirectives = newImports.OfType<UsingDirectiveSyntax>().Where(u => u.Alias != null).ToArray();

            var rewriter = new Rewriter(contextSpine, externAliases, usingDirectives, aliasDirectives, placeSystemNamespaceFirst);
            var newRoot = rewriter.Visit(root);

            return newRoot;
        }

        private class Rewriter : CSharpSyntaxRewriter
        {
            private UsingDirectiveSyntax[] _aliasDirectives;
            private ExternAliasDirectiveSyntax[] _externAliases;
            private UsingDirectiveSyntax[] _usingDirectives;
            private readonly bool _placeSystemNamespaceFirst;
            private readonly ISet<SyntaxNode> _contextSpine;

            public Rewriter(
                ISet<SyntaxNode> contextSpine,
                ExternAliasDirectiveSyntax[] externAliases, 
                UsingDirectiveSyntax[] usingDirectives, 
                UsingDirectiveSyntax[] aliasDirectives,
                bool placeSystemNamespaceFirst)
            {
                _contextSpine = contextSpine;
                _externAliases = externAliases;
                _usingDirectives = usingDirectives;
                _aliasDirectives = aliasDirectives;
                _placeSystemNamespaceFirst = placeSystemNamespaceFirst;
            }

            private static Func<UsingDirectiveSyntax, bool> s_isUsing = u => u.Alias == null;
            private static Func<UsingDirectiveSyntax, bool> s_isAlias = u => u.Alias != null;

            public override SyntaxNode VisitNamespaceDeclaration(NamespaceDeclarationSyntax node)
            {
                // recurse downwards so we visit inner namespaces first.
                var rewritten = (NamespaceDeclarationSyntax)base.VisitNamespaceDeclaration(node);

                if (_contextSpine.Contains(node))
                {
                    if (_aliasDirectives != null &&
                        rewritten.Usings.Any(s_isAlias))
                    {
                        rewritten = rewritten.AddUsingDirectives(_aliasDirectives, _placeSystemNamespaceFirst);
                        _aliasDirectives = null;
                    }

                    if (_usingDirectives != null &&
                        rewritten.Usings.Any(s_isUsing))
                    {
                        rewritten = rewritten.AddUsingDirectives(_usingDirectives, _placeSystemNamespaceFirst);
                        _usingDirectives = null;
                    }

                    if (_externAliases != null &&
                        rewritten.Externs.Any())
                    {
                        rewritten = rewritten.AddExterns(_externAliases);
                        _externAliases = null;
                    }
                }

                return rewritten;
            }

            public override SyntaxNode VisitCompilationUnit(CompilationUnitSyntax node)
            {
                // recurse downwards so we visit inner namespaces first.
                var rewritten = (CompilationUnitSyntax)base.VisitCompilationUnit(node);

                if (_aliasDirectives != null)
                {
                    rewritten = rewritten.AddUsingDirectives(_aliasDirectives, _placeSystemNamespaceFirst);
                }

                if (_usingDirectives != null)
                {
                    rewritten = rewritten.AddUsingDirectives(_usingDirectives, _placeSystemNamespaceFirst);
                }

                if (_externAliases != null)
                {
                    rewritten = rewritten.AddExterns(_externAliases);
                }

                return rewritten;
            }
        }
    }
}