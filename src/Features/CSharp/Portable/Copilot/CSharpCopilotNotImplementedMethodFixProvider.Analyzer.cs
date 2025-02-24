// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.MethodImplementation;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Copilot;

internal static class DocumentAnalyzer
{
    private const int MaxMethodLength = 1024;
    private const int ContextLineCount = 2;

    public static async Task<MethodImplementationProposal?> AnalyzeDocumentAsync(Document document, ThrowStatementSyntax throwStatement, CancellationToken cancellationToken)
    {
        if (throwStatement == null)
        {
            return null;
        }

        // Get the semantic model and syntax root
        var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);

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

        // Get symbol information
        var methodSymbol = semanticModel.GetDeclaredSymbol(methodDeclaration, cancellationToken);
        if (methodSymbol == null)
            return null;

        var returnTypeSymbol = methodSymbol?.ReturnType;

        // Find references
        var references = await SymbolFinder.FindReferencesAsync(methodSymbol!, document.Project.Solution, cancellationToken).ConfigureAwait(false);
        var referenceCount = references.Sum(r => r.Locations.Count());

        // Get top 2 surrounding code snippets
        var topReferences = references
            .SelectMany(r => r.Locations)
            .OrderBy(l => l.Location.SourceSpan.Length)
            .Take(2)
            .Select(async l =>
            {
                var refDocument = document.Project.Solution.GetDocument(l.Document.Id);
                if (refDocument == null)
                    return null;
                var refRoot = await refDocument.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
                if (refRoot == null)
                    return null;

                var referenceNode = refRoot.FindNode(l.Location.SourceSpan);
                var containingMethod = referenceNode.Ancestors().OfType<MethodDeclarationSyntax>().FirstOrDefault();
                var refText = await refDocument.GetTextAsync(cancellationToken).ConfigureAwait(false);

                var contextSpan = GetContextSpan(refText, l.Location.SourceSpan, containingMethod);
                return new MethodImplementationReferenceContext
                {
                    FileName = refDocument.Name,
                    SurroundingCode = refText.ToString(contextSpan)
                };
            })
            .Where(x => x != null)
            .ToList();

        var referenceContexts = await Task.WhenAll(topReferences).ConfigureAwait(false);

        // Get C# language version
        var parseOptions = (CSharpParseOptions?)document.Project.ParseOptions;
        var languageVersion = parseOptions?.LanguageVersion.ToString() ?? string.Empty;

        // Create analysis record
        var record = new MethodImplementationProposal
        {
            MethodName = methodDeclaration.Identifier.Text,
            ReturnType = methodDeclaration.ReturnType.ToString(),
            Parameters = parameters.Select(p => new MethodImplementationParameterContext
            {
                Name = p.Identifier.Text,
                Type = p.Type?.ToString() ?? string.Empty,
                Modifiers = p.Modifiers.Select(m => m.Text).ToImmutableArray()
            }).ToImmutableArray(),
            ReferenceCount = referenceCount,
            TopReferences = referenceContexts?.Where(x => x != null).Select(x => x!).ToImmutableArray() ?? [],
            ContainingType = methodDeclaration.Parent?.ToString() ?? string.Empty,
            Accessibility = GetAccessibility(methodDeclaration.Modifiers).ToString().ToLower(),
            Modifiers = methodDeclaration.Modifiers.Select(m => m.Text).ToImmutableArray(),
            PreviousTokenText = previousToken.Text,
            NextTokenText = nextToken.Text,
            LanguageVersion = languageVersion,
        };

        return record;
    }

    private static TextSpan GetContextSpan(SourceText text, TextSpan referenceSpan, MethodDeclarationSyntax? containingMethod)
    {
        // If we have a reasonably-sized containing method, use its full span
        if (containingMethod != null && containingMethod.Span.Length <= MaxMethodLength)
        {
            return containingMethod.Span;
        }

        // Otherwise just get context around the reference
        var startLine = text.Lines.GetLineFromPosition(referenceSpan.Start).LineNumber;
        var endLine = text.Lines.GetLineFromPosition(referenceSpan.End).LineNumber;
        var expandedStart = text.Lines[Math.Max(0, startLine - ContextLineCount)].Start;
        var expandedEnd = text.Lines[Math.Min(text.Lines.Count - 1, endLine + ContextLineCount)].End;
        return TextSpan.FromBounds(expandedStart, expandedEnd);
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
}
