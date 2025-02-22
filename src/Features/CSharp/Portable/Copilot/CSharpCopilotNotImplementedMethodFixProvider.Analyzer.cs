// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.Copilot;

internal static class DocumentAnalyzer
{
    public static async Task<AnalysisRecord?> AnalyzeDocumentAsync(Document document, ThrowStatementSyntax throwStatement, CancellationToken cancellationToken)
    {
        if (throwStatement == null)
        {
            return null;
        }

        // Get the semantic model and syntax root
        var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        if (semanticModel == null)
        {
            return null;
        }

        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root == null)
        {
            return null;
        }

        // Find the containing method declaration
        var methodDeclaration = throwStatement.Ancestors().OfType<MethodDeclarationSyntax>().FirstOrDefault();
        if (methodDeclaration == null)
            return null;

        // Get previous and next tokens for context
        var previousToken = methodDeclaration.Modifiers.Count > 0
            ? methodDeclaration.Modifiers[0]
            : methodDeclaration.GetFirstToken();
        var nextToken = methodDeclaration.GetLastToken();

        // Extract method information
        var parameters = methodDeclaration.ParameterList.Parameters;

        // Create analysis record
        var record = new AnalysisRecord
        {
            MethodName = methodDeclaration.Identifier.Text,
            ReturnType = methodDeclaration.ReturnType.ToString(),
            Parameters = parameters,
            ContainingType = methodDeclaration.Parent?.ToString() ?? string.Empty,
            Accessibility = GetAccessibility(methodDeclaration.Modifiers),
            Modifiers = methodDeclaration.Modifiers.Select(m => m.Text).ToImmutableArray(),
            PreviousToken = previousToken,
            NextToken = nextToken
        };

        return record;
    }

    private static Accessibility GetAccessibility(SyntaxTokenList modifiers)
    {
        foreach (var modifier in modifiers)
        {
            switch (modifier.Kind())
            {
                case SyntaxKind.PublicKeyword:
                    return Accessibility.Public;
                case SyntaxKind.PrivateKeyword:
                    return Accessibility.Private;
                case SyntaxKind.ProtectedKeyword:
                    return Accessibility.Protected;
                case SyntaxKind.InternalKeyword:
                    return Accessibility.Internal;
            }
        }

        return Accessibility.Private; // Default accessibility in C#
    }

    internal record AnalysisRecord
    {
        public required string MethodName { get; init; }
        public required string ReturnType { get; init; }
        public required SeparatedSyntaxList<ParameterSyntax> Parameters { get; init; }
        public required string ContainingType { get; init; }
        public required Accessibility Accessibility { get; init; }
        public required ImmutableArray<string> Modifiers { get; init; }
        public required SyntaxToken PreviousToken { get; init; }
        public required SyntaxToken NextToken { get; init; }
    }
}
