// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.AddImport;
using Microsoft.CodeAnalysis.CSharp.LanguageService;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Extensions;

internal static partial class CompilationUnitSyntaxExtensions
{
    public static bool CanAddUsingDirectives(this SyntaxNode contextNode, bool allowInHiddenRegions, CancellationToken cancellationToken)
    {
        var usingDirectiveAncestor = contextNode.GetAncestor<UsingDirectiveSyntax>();
        if (usingDirectiveAncestor?.Parent is CompilationUnitSyntax)
        {
            // We are inside a top level using directive (i.e. one that's directly in the compilation unit).
            return false;
        }

        if (!allowInHiddenRegions && contextNode.SyntaxTree.HasHiddenRegions())
        {
            var namespaceDeclaration = contextNode.GetInnermostNamespaceDeclarationWithUsings();
            var root = (CompilationUnitSyntax)contextNode.SyntaxTree.GetRoot(cancellationToken);
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

    private static TextSpan GetUsingsSpan(CompilationUnitSyntax root, BaseNamespaceDeclarationSyntax? namespaceDeclaration)
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
        return root.AddUsingDirectives([usingDirective], contextNode, placeSystemNamespaceFirst, annotations);
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
        if (usingDirectives.Count == 0)
        {
            return root;
        }

        var usings = AddUsingDirectives(root, usingDirectives);

        // Keep usings sorted if they were originally sorted.
        usings.SortUsingDirectives(root.Usings, placeSystemNamespaceFirst);

        var addBlankLine = false;
        if (root.Externs.Count == 0)
        {
            (root, addBlankLine) = AddImportHelpers.MoveTrivia(
                CSharpSyntaxFacts.Instance, root, root.Usings, usings);
        }

        var rootWithNewUsings = root.WithUsings(
            SyntaxFactory.List(usings.Select(u => u.WithAdditionalAnnotations(annotations))));
        if (addBlankLine)
        {
            var lastUsing = rootWithNewUsings.Usings.Last();
            var nextToken = lastUsing.GetLastToken(includeZeroWidth: true, includeSkipped: true).GetNextTokenOrEndOfFile(includeZeroWidth: true, includeSkipped: true);
            var endOfLine = lastUsing.GetTrailingTrivia().LastOrDefault(CSharpSyntaxFacts.Instance.IsEndOfLineTrivia);
            Debug.Assert(!endOfLine.IsKind(SyntaxKind.None));
            if (!endOfLine.IsKind(SyntaxKind.None))
            {
                rootWithNewUsings = rootWithNewUsings.ReplaceToken(nextToken, nextToken.WithPrependedLeadingTrivia(endOfLine));
            }
        }

        return rootWithNewUsings;
    }

    private static List<UsingDirectiveSyntax> AddUsingDirectives(
        CompilationUnitSyntax root, IList<UsingDirectiveSyntax> usingDirectives)
    {
        // We need to try and not place the using inside of a directive if possible.
        var usings = new List<UsingDirectiveSyntax>();
        var endOfList = root.Usings.Count - 1;
        var startOfLastDirective = -1;
        var endOfLastDirective = -1;
        for (var i = 0; i < root.Usings.Count; i++)
        {
            if (root.Usings[i].GetLeadingTrivia().Any(trivia => trivia.IsKind(SyntaxKind.IfDirectiveTrivia)))
            {
                startOfLastDirective = i;
            }

            if (root.Usings[i].GetLeadingTrivia().Any(trivia => trivia.IsKind(SyntaxKind.EndIfDirectiveTrivia)))
            {
                endOfLastDirective = i;
            }
        }

        // if the entire using is in a directive or there is a using list at the end outside of the directive add the using at the end, 
        // else place it before the last directive.
        usings.AddRange(root.Usings);
        if ((startOfLastDirective == 0 && (endOfLastDirective == endOfList || endOfLastDirective == -1)) ||
            (startOfLastDirective == -1 && endOfLastDirective == -1) ||
            (endOfLastDirective != endOfList && endOfLastDirective != -1))
        {
            usings.AddRange(usingDirectives);
        }
        else
        {
            usings.InsertRange(startOfLastDirective, usingDirectives);
        }

        return usings;
    }
}
