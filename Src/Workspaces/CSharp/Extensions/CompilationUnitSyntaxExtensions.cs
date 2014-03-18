// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Utilities;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Extensions
{
    internal static class CompilationUnitSyntaxExtensions
    {
        public static bool CanAddUsingDirectives(this SyntaxNode contextNode, CancellationToken cancellationToken)
        {
            if (contextNode.SyntaxTree.HasHiddenRegions())
            {
                var namespaceDeclaration = contextNode.GetInnermostNamespaceDeclarationWithUsings();
                var root = contextNode.GetAncestorOrThis<CompilationUnitSyntax>();
                var span = GetUsingsSpan(root, namespaceDeclaration);

                if (contextNode.SyntaxTree.OverlapsHiddenPosition(span, cancellationToken))
                {
                    return false;
                }
            }

            if (cancellationToken.IsCancellationRequested)
            {
                return false;
            }

            return true;
        }

        private static TextSpan GetUsingsSpan(CompilationUnitSyntax root, NamespaceDeclarationSyntax namespaceDeclaration)
        {
            if (namespaceDeclaration != null)
            {
                var usings = namespaceDeclaration.Usings;
                var start = usings.First().SpanStart;
                var end = usings.Last().Span.End;
                return TextSpan.FromBounds(start, end);
            }
            else
            {
                var rootUsings = root.Usings;
                if (rootUsings.Any())
                {
                    var start = rootUsings.First().SpanStart;
                    var end = rootUsings.Last().Span.End;
                    return TextSpan.FromBounds(start, end);
                }
                else
                {
                    var start = 0;
                    var end = root.Members.Any()
                        ? root.Members.First().GetFirstToken().Span.End
                        : root.Span.End;
                    return TextSpan.FromBounds(start, end);
                }
            }
        }

        public static CompilationUnitSyntax AddUsingDirective(
            this CompilationUnitSyntax root,
            UsingDirectiveSyntax usingDirective,
            SyntaxNode contextNode,
            bool placeSystemNamespaceFirst,
            params SyntaxAnnotation[] annotations)
        {
            return root.AddUsingDirectives(new[] { usingDirective }, contextNode, placeSystemNamespaceFirst, annotations);
        }

        public static CompilationUnitSyntax AddUsingDirectives(
            this CompilationUnitSyntax root,
            IList<UsingDirectiveSyntax> usingDirectives,
            SyntaxNode contextNode,
            bool placeSystemNamespaceFirst,
            params SyntaxAnnotation[] annotations)
        {
            if (!usingDirectives.Any())
            {
                return root;
            }

            var firstOuterNamespaceWithUsings = contextNode.GetInnermostNamespaceDeclarationWithUsings();

            if (firstOuterNamespaceWithUsings == null)
            {
                return root.AddUsingDirectives(usingDirectives, placeSystemNamespaceFirst, annotations);
            }
            else
            {
                var newNamespace = firstOuterNamespaceWithUsings.AddUsingDirectives(usingDirectives, placeSystemNamespaceFirst, annotations);
                return root.ReplaceNode(firstOuterNamespaceWithUsings, newNamespace);
            }
        }

        public static CompilationUnitSyntax AddUsingDirectives(
            this CompilationUnitSyntax root,
            IList<UsingDirectiveSyntax> usingDirectives,
            bool placeSystemNamespaceFirst,
            params SyntaxAnnotation[] annotations)
        {
            if (!usingDirectives.Any())
            {
                return root;
            }

            var specialCaseSystem = placeSystemNamespaceFirst;
            var comparer = specialCaseSystem
                ? UsingsAndExternAliasesDirectiveComparer.SystemFirstInstance
                : UsingsAndExternAliasesDirectiveComparer.NormalInstance;

            var usings = new List<UsingDirectiveSyntax>();
            usings.AddRange(root.Usings);
            usings.AddRange(usingDirectives);

            // If the user likes to have their Usings statements unsorted, allow them to
            if (root.Usings.IsSorted(comparer))
            {
                usings.Sort(comparer);
            }

            // If any using we added was moved to the first location, then take the trivia from
            // the start of the first token and add it to the using we added.  This way things
            // like #define's and #r's will stay above the using.
            var firstUsingChanged = root.Usings.Count == 0 || usings[0] != root.Usings[0];
            if (firstUsingChanged && root.Externs.Count == 0)
            {
                var firstToken = root.GetFirstToken();

                // Move the leading directives from the first directive to the new using.
                var firstUsing = usings[0].WithLeadingTrivia(firstToken.LeadingTrivia.Where(t => !t.IsDocComment() && !t.IsElastic()));

                // Remove the leading directives from the first token.
                var newFirstToken = firstToken.WithLeadingTrivia(firstToken.LeadingTrivia.Where(t => t.IsDocComment() || t.IsElastic()));

                // Remove the leading trivia from the first token from the tree.
                root = root.ReplaceToken(firstToken, newFirstToken);

                // Create the new list of usings.  
                var finalUsings = new List<UsingDirectiveSyntax>();
                finalUsings.Add(firstUsing);
                finalUsings.AddRange(root.Usings);
                finalUsings.AddRange(usingDirectives.Except(new[] { usings[0] }));
                finalUsings.Sort(comparer);
                usings = finalUsings;
            }

            usings = usings.Select(u => u.WithAdditionalAnnotations(annotations)).ToList();
            return root.WithUsings(usings.ToSyntaxList());
        }
    }
}