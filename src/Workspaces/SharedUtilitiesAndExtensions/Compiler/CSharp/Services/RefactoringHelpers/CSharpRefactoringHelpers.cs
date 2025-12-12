// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.LanguageService;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.CodeRefactorings;

internal sealed class CSharpRefactoringHelpers : AbstractRefactoringHelpers<ExpressionSyntax, ArgumentSyntax, ExpressionStatementSyntax>
{
    public static readonly CSharpRefactoringHelpers Instance = new();

    private CSharpRefactoringHelpers()
    {
    }

    protected override IHeaderFacts HeaderFacts => CSharpHeaderFacts.Instance;
    protected override ISyntaxFacts SyntaxFacts => CSharpSyntaxFacts.Instance;

    public override bool IsBetweenTypeMembers(SourceText sourceText, SyntaxNode root, int position, [NotNullWhen(true)] out SyntaxNode? typeDeclaration)
    {
        var token = root.FindToken(position);
        var typeDecl = token.GetAncestor<TypeDeclarationSyntax>();
        typeDeclaration = typeDecl;

        if (typeDecl == null)
            return false;

        RoslynDebug.AssertNotNull(typeDeclaration);
        if (position < typeDecl.OpenBraceToken.Span.End ||
            position > typeDecl.CloseBraceToken.Span.Start)
        {
            return false;
        }

        var line = sourceText.Lines.GetLineFromPosition(position);
        if (!line.IsEmptyOrWhitespace())
            return false;

        var member = typeDecl.Members.FirstOrDefault(d => d.FullSpan.Contains(position));
        if (member == null)
        {
            // There are no members, or we're after the last member.
            return true;
        }
        else
        {
            // We're within a member.  Make sure we're in the leading whitespace of
            // the member.
            if (position < member.SpanStart)
            {
                foreach (var trivia in member.GetLeadingTrivia())
                {
                    if (!trivia.IsWhitespaceOrEndOfLine())
                        return false;

                    if (trivia.FullSpan.Contains(position))
                        return true;
                }
            }
        }

        return false;
    }

    protected override IEnumerable<SyntaxNode> ExtractNodesSimple(SyntaxNode? node, ISyntaxFacts syntaxFacts)
    {
        if (node == null)
        {
            yield break;
        }

        foreach (var extractedNode in base.ExtractNodesSimple(node, syntaxFacts))
        {
            yield return extractedNode;
        }

        // `var a = b;`
        // -> `var a = b`;
        if (node is LocalDeclarationStatementSyntax localDeclaration)
        {
            yield return localDeclaration.Declaration;
        }

        // var `a = b`;
        if (node is VariableDeclaratorSyntax declarator)
        {
            var declaration = declarator.Parent;
            if (declaration?.Parent is LocalDeclarationStatementSyntax localDeclarationStatement)
            {
                var variables = syntaxFacts.GetVariablesOfLocalDeclarationStatement(localDeclarationStatement);
                if (variables.Count == 1)
                {
                    // -> `var a = b`;
                    yield return declaration;

                    // -> `var a = b;`
                    yield return localDeclarationStatement;
                }
            }
        }
    }
}
