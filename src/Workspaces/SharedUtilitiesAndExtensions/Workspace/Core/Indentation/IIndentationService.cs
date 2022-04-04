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

#if CODE_STYLE
using OptionSet = Microsoft.CodeAnalysis.Diagnostics.AnalyzerConfigOptions;
#else
using Microsoft.CodeAnalysis.Options;
#endif

namespace Microsoft.CodeAnalysis.Indentation
{
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
#if CODE_STYLE
            var tree = document.GetSyntaxTreeAsync(cancellationToken).WaitAndGetResult_CanCallOnBackground(cancellationToken);
            var options = document.Project.AnalyzerOptions.AnalyzerConfigOptionsProvider.GetOptions(tree!);
#else
            var options = document.GetOptionsAsync(cancellationToken).WaitAndGetResult_CanCallOnBackground(cancellationToken);
#endif

            var style = options.GetOption(FormattingOptions2.SmartIndent, document.Project.Language);

            return service.GetIndentation(document, lineNumber, (FormattingOptions.IndentStyle)style, cancellationToken);
        }

        /// <summary>
        /// Get's the preferred indentation for <paramref name="token"/> if that token were on its own line.  This
        /// effectively simulates where the token would be if the user hit enter at the start of the token.
        /// </summary>
        public static string GetPreferredIndentation(this SyntaxToken token, Document document, CancellationToken cancellationToken)
        {
#if CODE_STYLE
            var sourceText = document.GetTextAsync(cancellationToken).WaitAndGetResult_CanCallOnBackground(cancellationToken);
#else
            var sourceText = document.GetTextSynchronously(cancellationToken);
#endif
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

#if CODE_STYLE
            var options = document.Project.AnalyzerOptions.AnalyzerConfigOptionsProvider.GetOptions(token.SyntaxTree!);
#else
            var options = document.Project.Solution.Options;
#endif

            var languageName = document.Project.Language;
            var newLine = options.GetOption(FormattingOptions2.NewLine, languageName);

            var annotation = new SyntaxAnnotation();
            var newToken = token.WithAdditionalAnnotations(annotation);

            var syntaxGenerator = document.GetRequiredLanguageService<SyntaxGeneratorInternal>();
            newToken = newToken.WithLeadingTrivia(newToken.LeadingTrivia.Add(syntaxGenerator.EndOfLine(newLine)));

#if CODE_STYLE
            var root = document.GetSyntaxRootAsync(cancellationToken).WaitAndGetResult_CanCallOnBackground(cancellationToken);
#else
            var root = document.GetRequiredSyntaxRootSynchronously(cancellationToken);
#endif
            Contract.ThrowIfNull(root);
            var newRoot = root.ReplaceToken(token, newToken);
            var newDocument = document.WithSyntaxRoot(newRoot);

#if CODE_STYLE
            var newText = newDocument.GetTextAsync(cancellationToken).WaitAndGetResult_CanCallOnBackground(cancellationToken);
#else
            var newText = newDocument.GetTextSynchronously(cancellationToken);
#endif

            var newTokenLine = newText.Lines.GetLineFromPosition(newRoot.GetAnnotatedTokens(annotation).Single().SpanStart);

            var indentStyle = options.GetOption(FormattingOptions2.SmartIndent, languageName);
            var indenter = document.GetRequiredLanguageService<IIndentationService>();

            var indentation = indenter.GetIndentation(
                newDocument, newTokenLine.LineNumber, (FormattingOptions.IndentStyle)indentStyle, cancellationToken);

            return indentation.GetIndentationString(
                newText,
                options.GetOption(FormattingOptions2.UseTabs, languageName),
                options.GetOption(FormattingOptions2.TabSize, languageName));
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
