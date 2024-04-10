// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Diagnostics;
using System.Linq;
using System.Threading;
using Roslyn.Utilities;
using System.Text;

namespace Microsoft.CodeAnalysis.LanguageService;

internal abstract class AbstractDocumentationCommentService<
    TDocumentationCommentTriviaSyntax,
    TXmlNodeSyntax,
    TXmlAttributeSyntax,
    TCrefSyntax,
    TXmlElementSyntax,
    TXmlTextSyntax,
    TXmlEmptyElementSyntax,
    TXmlCrefAttributeSyntax,
    TXmlNameAttributeSyntax,
    TXmlTextAttributeSyntax> : IDocumentationCommentService
    where TDocumentationCommentTriviaSyntax : SyntaxNode
    where TXmlNodeSyntax : SyntaxNode
    where TXmlAttributeSyntax : SyntaxNode
    where TCrefSyntax : SyntaxNode
    where TXmlElementSyntax : TXmlNodeSyntax
    where TXmlTextSyntax : TXmlNodeSyntax
    where TXmlEmptyElementSyntax : TXmlNodeSyntax
    where TXmlCrefAttributeSyntax : TXmlAttributeSyntax
    where TXmlNameAttributeSyntax : TXmlAttributeSyntax
    where TXmlTextAttributeSyntax : TXmlAttributeSyntax
{
    public const string Ellipsis = "...";

    private readonly ISyntaxFacts _syntaxFacts;

    protected AbstractDocumentationCommentService(ISyntaxFacts syntaxFacts)
        => _syntaxFacts = syntaxFacts;

    private static void AddSpaceIfNotAlreadyThere(StringBuilder sb)
    {
        if (sb is [.., not ' '])
            sb.Append(' ');
    }

    private string GetDocumentationCommentPrefix(TDocumentationCommentTriviaSyntax documentationComment)
    {
        Contract.ThrowIfNull(documentationComment);

        var leadingTrivia = documentationComment.GetLeadingTrivia();
        var exteriorTrivia = leadingTrivia.Where(_syntaxFacts.IsDocumentationCommentExteriorTrivia)
                                          .FirstOrNull();

        return exteriorTrivia != null ? exteriorTrivia.Value.ToString() : string.Empty;
    }

    public string GetBannerText(
        TDocumentationCommentTriviaSyntax documentationComment, int maxBannerLength, CancellationToken cancellationToken)
    {
        // TODO: Consider unifying code to extract text from an Xml Documentation Comment (https://github.com/dotnet/roslyn/issues/2290)
        var summaryElement =
            documentationComment.ChildNodes().OfType<TXmlElementSyntax>()
                                .FirstOrDefault(e => GetName(e).ToString() == "summary");

        var prefix = GetDocumentationCommentPrefix(documentationComment);

        string text;
        if (summaryElement != null)
        {
            var sb = new StringBuilder(summaryElement.Span.Length);
            sb.Append(prefix);
            sb.Append(" <summary>");
            HandleElement(summaryElement, sb);

            text = sb.ToString().Trim();
        }
        else
        {
            // If a summary element isn't found, use the first line of the XML doc comment.
            var syntaxTree = documentationComment.SyntaxTree;
            var spanStart = documentationComment.SpanStart;
            var line = syntaxTree.GetText(cancellationToken).Lines.GetLineFromPosition(spanStart);
            text = prefix + " " + line.ToString()[(spanStart - line.Start)..].Trim() + " " + Ellipsis;
        }

        if (text.Length > maxBannerLength)
        {
            text = text[..maxBannerLength] + " " + Ellipsis;
        }

        return text;
    }

    private void HandleElement(TXmlElementSyntax summaryElement, StringBuilder sb)
    {
        foreach (var node in summaryElement.ChildNodes())
        {
            HandleNode(node, sb);
        }
    }

    private void HandleNode(SyntaxNode node, StringBuilder sb)
    {
        if (node.HasLeadingTrivia)
        {
            // Collapse all trailing trivia to a single space.
            AddSpaceIfNotAlreadyThere(sb);
        }

        if (node is TXmlTextSyntax xmlText)
        {
            var textTokens = GetTextTokens(xmlText);
            AppendTextTokens(sb, textTokens);
        }
        else if (node is TXmlElementSyntax xmlElement)
        {
            HandleElement(xmlElement, sb);
        }
        else if (node is TXmlEmptyElementSyntax xmlEmpty)
        {
            foreach (var attribute in GetAttributes(xmlEmpty))
            {
                switch (attribute)
                {
                    case TXmlCrefAttributeSyntax xmlCref:
                        AddSpaceIfNotAlreadyThere(sb);
                        sb.Append(GetCref(xmlCref).ToString());
                        break;
                    case TXmlNameAttributeSyntax xmlName:
                        AddSpaceIfNotAlreadyThere(sb);
                        sb.Append(GetIdentifier(xmlName).Text);
                        break;
                    case TXmlTextAttributeSyntax xmlTextAttribute:
                        AddSpaceIfNotAlreadyThere(sb);
                        AppendTextTokens(sb, GetTextTokens(xmlTextAttribute));
                        break;
                    default:
                        Debug.Assert(false, $"Unexpected XML syntax kind {attribute.RawKind}");
                        break;
                }
            }
        }

        if (node.HasTrailingTrivia)
        {
            // Collapse all trailing trivia to a single space.
            AddSpaceIfNotAlreadyThere(sb);
        }
    }

    protected abstract SyntaxToken GetIdentifier(TXmlNameAttributeSyntax xmlName);
    protected abstract TCrefSyntax GetCref(TXmlCrefAttributeSyntax xmlCref);
    protected abstract SyntaxList<TXmlAttributeSyntax> GetAttributes(TXmlEmptyElementSyntax xmlEmpty);
    protected abstract SyntaxTokenList GetTextTokens(TXmlTextSyntax xmlText);
    protected abstract SyntaxTokenList GetTextTokens(TXmlTextAttributeSyntax xmlTextAttribute);
    protected abstract SyntaxNode GetName(TXmlElementSyntax xmlElement);

    private static void AppendTextTokens(StringBuilder sb, SyntaxTokenList textTokens)
    {
        foreach (var token in textTokens)
        {
            var tokenText = token.ToString();

            // Collapse all preceding trivia or whitespace for this token to a single space.
            if (token.LeadingTrivia.Count > 0 || HasLeadingWhitespace(tokenText))
            {
                AddSpaceIfNotAlreadyThere(sb);
            }

            sb.Append(tokenText.Trim());

            // Collapse all trailing trivia or whitespace for this token to a single space.
            if (token.TrailingTrivia.Count > 0 || HasTrailingWhitespace(tokenText))
            {
                AddSpaceIfNotAlreadyThere(sb);
            }
        }
    }

    private static bool HasLeadingWhitespace(string tokenText)
        => tokenText.Length > 0 && char.IsWhiteSpace(tokenText[0]);

    private static bool HasTrailingWhitespace(string tokenText)
        => tokenText.Length > 0 && char.IsWhiteSpace(tokenText[^1]);

    public string GetBannerText(SyntaxNode documentationCommentTriviaSyntax, int maxBannerLength, CancellationToken cancellationToken)
        => GetBannerText((TDocumentationCommentTriviaSyntax)documentationCommentTriviaSyntax, maxBannerLength, cancellationToken);
}
