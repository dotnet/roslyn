// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Simplification;

namespace Microsoft.CodeAnalysis.CSharp.Copilot;

internal sealed partial class CSharpCopilotNotImplementedMethodFixProvider
{
    private static class CodeGenerator
    {
        public static void GenerateCode(SyntaxEditor editor, SyntaxNode throwNode, string codeBlockSuggestion)
        {
            if (string.IsNullOrWhiteSpace(codeBlockSuggestion))
                return;

            try
            {
                // Find the containing method declaration
                var methodDeclaration = throwNode.Ancestors().OfType<MethodDeclarationSyntax>().First();

                // Replace the line with the error
                var baseIndentation = methodDeclaration.Body?.OpenBraceToken
                    .LeadingTrivia
                    .Where(t => t.IsKind(SyntaxKind.WhitespaceTrivia))
                    .LastOrDefault()
                    .ToString() ?? "    ";

                // Parse the provided code block
                var newMethodBody = SyntaxFactory.Block(
                        SyntaxFactory.Token(SyntaxKind.OpenBraceToken),
                        SyntaxFactory.List(SyntaxFactory
                            .ParseStatement($"{{\n{codeBlockSuggestion}\n}}")
                            .DescendantNodes()
                            .OfType<StatementSyntax>()
                            .Select(s => s.WithLeadingTrivia(
                                s.GetLeadingTrivia()
                                    .Select(t => t.IsKind(SyntaxKind.WhitespaceTrivia)
                                        ? SyntaxFactory.Whitespace(baseIndentation)
                                        : t))
                                .WithAdditionalAnnotations(Formatter.Annotation, Simplifier.Annotation)) ?? Enumerable.Empty<StatementSyntax>()),
                        SyntaxFactory.Token(SyntaxKind.CloseBraceToken))
                    .WithAdditionalAnnotations(Formatter.Annotation);

                // Replace the entire method body
                editor.ReplaceNode(
                    methodDeclaration.Body ?? (SyntaxNode)methodDeclaration.ExpressionBody!,
                    newMethodBody);
            }
            catch (Exception)
            {
                // Handle any exceptions that occur during the replacement
            }
        }
    }
}
