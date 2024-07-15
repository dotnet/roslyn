// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.ConvertPrimaryToRegularConstructor;

using static SyntaxFactory;

internal sealed partial class ConvertPrimaryToRegularConstructorCodeRefactoringProvider
{
    private static SyntaxTrivia GetDocComment(SyntaxTriviaList trivia)
        => trivia.LastOrDefault(t => t.IsSingleLineDocComment());

    private static DocumentationCommentTriviaSyntax? GetDocCommentStructure(SyntaxTrivia trivia)
        => (DocumentationCommentTriviaSyntax?)trivia.GetStructure();

    private static bool IsXmlElement(XmlNodeSyntax node, string name, [NotNullWhen(true)] out XmlElementSyntax? element)
    {
        element = node is XmlElementSyntax { StartTag.Name.LocalName.ValueText: var elementName } xmlElement && elementName == name
            ? xmlElement
            : null;
        return element != null;
    }

    private static TypeDeclarationSyntax RemoveParamXmlElements(TypeDeclarationSyntax typeDeclaration)
    {
        var triviaList = typeDeclaration.GetLeadingTrivia();
        var trivia = GetDocComment(triviaList);
        var docComment = GetDocCommentStructure(trivia);
        if (docComment == null)
            return typeDeclaration;

        using var _ = ArrayBuilder<XmlNodeSyntax>.GetInstance(out var content);

        foreach (var node in docComment.Content)
        {
            if (IsXmlElement(node, "param", out var paramElement))
            {
                // We're skipping a param node.  Fixup any preceding text node we may have before it.
                FixupLastTextNode();
            }
            else
            {
                content.Add(node);
            }
        }

        if (content.All(c => c is XmlTextSyntax xmlText && xmlText.TextTokens.All(
                t => t.Kind() == SyntaxKind.XmlTextLiteralNewLineToken || string.IsNullOrWhiteSpace(t.Text))))
        {
            // Nothing but param nodes.  Just remove all the doc comments entirely.
            var triviaIndex = triviaList.IndexOf(trivia);

            // remove the doc comment itself
            var updatedTriviaList = triviaList.RemoveAt(triviaIndex);

            // If the comment was on a line that started with whitespace, remove that whitespce too.
            if (triviaIndex > 0 && triviaList[triviaIndex - 1].IsWhitespace())
                updatedTriviaList = updatedTriviaList.RemoveAt(triviaIndex - 1);

            return typeDeclaration.WithLeadingTrivia(updatedTriviaList);
        }
        else
        {
            var updatedTrivia = Trivia(docComment.WithContent([.. content]));
            return typeDeclaration.WithLeadingTrivia(triviaList.Replace(trivia, updatedTrivia));
        }

        void FixupLastTextNode()
        {
            var node = content.LastOrDefault();
            if (node is not XmlTextSyntax xmlText)
                return;

            var tokens = xmlText.TextTokens;
            var lastIndex = tokens.Count;
            if (lastIndex - 1 >= 0 && tokens[lastIndex - 1].Kind() == SyntaxKind.XmlTextLiteralToken && string.IsNullOrWhiteSpace(tokens[lastIndex - 1].Text))
                lastIndex--;

            if (lastIndex - 1 >= 0 && tokens[lastIndex - 1].Kind() == SyntaxKind.XmlTextLiteralNewLineToken)
                lastIndex--;

            if (lastIndex == tokens.Count)
            {
                // no change necessary.
                return;
            }
            else if (lastIndex == 0)
            {
                // Removed all tokens from the text node.  So remove the text node entirely.
                content.RemoveLast();
            }
            else
            {
                // Otherwise, replace with newlines stripped.
                content[^1] = xmlText.WithTextTokens([.. tokens.Take(lastIndex)]);
            }
        }
    }

    private static ConstructorDeclarationSyntax WithTypeDeclarationParamDocComments(TypeDeclarationSyntax typeDeclaration, ConstructorDeclarationSyntax constructor)
    {
        // Now move the param tags on the type decl over to the constructor.
        var triviaList = typeDeclaration.GetLeadingTrivia();
        var trivia = GetDocComment(triviaList);
        var docComment = GetDocCommentStructure(trivia);
        if (docComment is not null)
        {
            using var _2 = ArrayBuilder<XmlNodeSyntax>.GetInstance(out var content);

            for (int i = 0, n = docComment.Content.Count; i < n; i++)
            {
                var node = docComment.Content[i];
                if (IsXmlElement(node, "param", out _))
                {
                    content.Add(node);

                    // if the param tag is followed with a newline, then preserve that when transferring over.
                    if (i + 1 < docComment.Content.Count && IsDocCommentNewLine(docComment.Content[i + 1]))
                        content.Add(docComment.Content[i + 1]);
                }
            }

            if (content.Count > 0)
            {
                if (!content[0].GetLeadingTrivia().Any(SyntaxKind.DocumentationCommentExteriorTrivia))
                    content[0] = content[0].WithLeadingTrivia(DocumentationCommentExterior("/// "));

                content[^1] = content[^1].WithTrailingTrivia(EndOfLine(""));

                var finalTrivia = DocumentationCommentTrivia(SyntaxKind.SingleLineDocumentationCommentTrivia, [.. content]);
                return constructor.WithLeadingTrivia(Trivia(finalTrivia));
            }
        }

        return constructor;
    }

    private static bool IsDocCommentNewLine(XmlNodeSyntax node)
    {
        if (node is not XmlTextSyntax xmlText)
            return false;

        foreach (var textToken in xmlText.TextTokens)
        {
            if (textToken.Kind() == SyntaxKind.XmlTextLiteralNewLineToken)
                continue;

            if (textToken.Kind() == SyntaxKind.XmlTextLiteralToken && string.IsNullOrWhiteSpace(textToken.Text))
                continue;

            return false;
        }

        return true;
    }
}
