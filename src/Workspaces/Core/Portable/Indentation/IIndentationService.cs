// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Indentation
{
    /// <summary>
    /// An indentation result represents where the indent should be placed.  It conveys this through
    /// a pair of values.  A position in the existing document where the indent should be relative,
    /// and the number of columns after that the indent should be placed at.  
    /// 
    /// This pairing provides flexibility to the implementor to compute the indentation results in
    /// a variety of ways.  For example, one implementation may wish to express indentation of a 
    /// newline as being four columns past the start of the first token on a previous line.  Another
    /// may wish to simply express the indentation as an absolute amount from the start of the 
    /// current line.  With this tuple, both forms can be expressed, and the implementor does not
    /// have to convert from one to the other.
    /// </summary>
    internal struct IndentationResult
    {
        /// <summary>
        /// The base position in the document that the indent should be relative to.  This position
        /// can occur on any line (including the current line, or a previous line).
        /// </summary>
        public int BasePosition { get; }

        /// <summary>
        /// The number of columns the indent should be at relative to the BasePosition's column.
        /// </summary>
        public int Offset { get; }

        public IndentationResult(int basePosition, int offset) : this()
        {
            this.BasePosition = basePosition;
            this.Offset = offset;
        }
    }

    internal interface IIndentationService : ILanguageService
    {
        /// <summary>
        /// Determines the desired indentation of a given line.
        /// </summary>
        IndentationResult GetIndentation(
            Document document, int lineNumber,
            FormattingOptions.IndentStyle indentStyle, CancellationToken cancellationToken);
    }

    internal static class IIndentationServiceExtensions
    {
        public static IndentationResult GetIndentation(
            this IIndentationService service, Document document,
            int lineNumber, CancellationToken cancellationToken)
        {
            var options = document.GetOptionsAsync(cancellationToken).WaitAndGetResult_CanCallOnBackground(cancellationToken);
            var style = options.GetOption(FormattingOptions.SmartIndent, document.Project.Language);

            return service.GetIndentation(document, lineNumber, style, cancellationToken);
        }

        /// <summary>
        /// Get's the preferred indentation for <paramref name="token"/> if that token were on its own line.  This
        /// effectively simulates where the token would be if the user hit enter at the start of the token.
        /// </summary>
        public static string GetPreferredIndentation(this SyntaxToken token, Document document, CancellationToken cancellationToken)
        {
            var sourceText = document.GetTextSynchronously(cancellationToken);
            var tokenLine = sourceText.Lines.GetLineFromPosition(token.SpanStart);
            var firstNonWhitespacePos = tokenLine.GetFirstNonWhitespacePosition();
            Contract.ThrowIfNull(firstNonWhitespacePos);
            if (firstNonWhitespacePos.Value == token.SpanStart)
            {
                // token was on it's own line.  Start the end delimiter at the same location as it.
                return tokenLine.Text!.ToString(TextSpan.FromBounds(tokenLine.Start, token.SpanStart));
            }

            // Token was on a line with something else.  Determine where we would indent the token if it was on the next
            // line and use that to determine the indentation of the final line.

            var options = document.Project.Solution.Options;
            var languageName = document.Project.Language;
            var newLine = options.GetOption(FormattingOptions.NewLine, languageName);

            var annotation = new SyntaxAnnotation();
            var newToken = token.WithAdditionalAnnotations(annotation);

            var syntaxGenerator = document.GetRequiredLanguageService<SyntaxGeneratorInternal>();
            newToken = newToken.WithLeadingTrivia(newToken.LeadingTrivia.Add(syntaxGenerator.EndOfLine(newLine)));

            var root = document.GetRequiredSyntaxRootSynchronously(cancellationToken);
            var newRoot = root.ReplaceToken(token, newToken);
            var newDocument = document.WithSyntaxRoot(newRoot);
            var newText = newDocument.GetTextSynchronously(cancellationToken);

            var newTokenLine = newText.Lines.GetLineFromPosition(newRoot.GetAnnotatedTokens(annotation).Single().SpanStart);

            var indentStyle = document.Project.Solution.Options.GetOption(FormattingOptions.SmartIndent, languageName);
            var indenter = document.GetRequiredLanguageService<IIndentationService>();

            var indentation = indenter.GetIndentation(newDocument, newTokenLine.LineNumber, indentStyle, cancellationToken);

            return indentation.GetIndentationString(
                newText,
                options.GetOption(FormattingOptions.UseTabs, languageName),
                options.GetOption(FormattingOptions.TabSize, languageName));
        }
    }

    internal static class IndentationResultExtensions
    {
        public static string GetIndentationString(this IndentationResult indentationResult, SourceText sourceText, bool useTabs, int tabSize)
        {
            var baseLine = sourceText.Lines.GetLineFromPosition(indentationResult.BasePosition);
            var baseOffsetInLine = indentationResult.BasePosition - baseLine.Start;

            var indent = baseOffsetInLine + indentationResult.Offset;

            var indentString = indent.CreateIndentationString(useTabs, tabSize);
            return indentString;
        }
    }
}
