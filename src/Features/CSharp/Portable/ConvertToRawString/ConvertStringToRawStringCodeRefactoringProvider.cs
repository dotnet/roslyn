// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.ConvertToRawString;

[ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = PredefinedCodeRefactoringProviderNames.ConvertToRawString), Shared]
internal partial class ConvertStringToRawStringCodeRefactoringProvider : SyntaxEditorBasedCodeRefactoringProvider
{
    private static readonly BidirectionalMap<ConvertToRawKind, string> s_kindToEquivalenceKeyMap =
        new(
        [
            KeyValuePairUtil.Create(ConvertToRawKind.SingleLine, nameof(ConvertToRawKind.SingleLine)),
            KeyValuePairUtil.Create(ConvertToRawKind.MultiLineIndented, nameof(ConvertToRawKind.MultiLineIndented)),
            KeyValuePairUtil.Create(ConvertToRawKind.MultiLineWithoutLeadingWhitespace, nameof(ConvertToRawKind.MultiLineWithoutLeadingWhitespace)),
        ]);

    private static readonly ImmutableArray<IConvertStringProvider> s_convertStringProviders = [ConvertRegularStringToRawStringProvider.Instance, ConvertInterpolatedStringToRawStringProvider.Instance];

    [ImportingConstructor]
    [SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
    public ConvertStringToRawStringCodeRefactoringProvider()
    {
    }

    protected override ImmutableArray<FixAllScope> SupportedFixAllScopes => AllFixAllScopes;

    private static bool CanConvert(
        ParsedDocument parsedDocument,
        ExpressionSyntax expression,
        SyntaxFormattingOptions formattingOptions,
        out CanConvertParams canConvertParams,
        [NotNullWhen(true)] out IConvertStringProvider? provider,
        CancellationToken cancellationToken)
    {
        foreach (var convertProvider in s_convertStringProviders)
        {
            if (convertProvider.CanConvert(parsedDocument, expression, formattingOptions, out canConvertParams, cancellationToken))
            {
                provider = convertProvider;
                return true;
            }
        }

        canConvertParams = default;
        provider = null;
        return false;
    }

    public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
    {
        var (document, span, cancellationToken) = context;

        var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var token = root.FindToken(span.Start);
        if (!context.Span.IntersectsWith(token.Span))
            return;

        if (token.Parent is not ExpressionSyntax parentExpression)
            return;

        var formattingOptions = await document.GetSyntaxFormattingOptionsAsync(cancellationToken).ConfigureAwait(false);
        var parsedDocument = await ParsedDocument.CreateAsync(document, cancellationToken).ConfigureAwait(false);

        if (!CanConvert(parsedDocument, parentExpression, formattingOptions, out var convertParams, out var provider, cancellationToken))
            return;

        var priority = convertParams.Priority;

        if (convertParams.CanBeSingleLine)
        {
            context.RegisterRefactoring(
                CodeAction.Create(
                    CSharpFeaturesResources.Convert_to_raw_string,
                    cancellationToken => UpdateDocumentAsync(document, parentExpression, ConvertToRawKind.SingleLine, provider, cancellationToken),
                    s_kindToEquivalenceKeyMap[ConvertToRawKind.SingleLine],
                    priority),
                token.Span);
        }
        else
        {
            context.RegisterRefactoring(
                CodeAction.Create(
                    CSharpFeaturesResources.Convert_to_raw_string,
                    cancellationToken => UpdateDocumentAsync(document, parentExpression, ConvertToRawKind.MultiLineIndented, provider, cancellationToken),
                    s_kindToEquivalenceKeyMap[ConvertToRawKind.MultiLineIndented],
                    priority),
                token.Span);

            if (convertParams.CanBeMultiLineWithoutLeadingWhiteSpaces)
            {
                context.RegisterRefactoring(
                    CodeAction.Create(
                        CSharpFeaturesResources.without_leading_whitespace_may_change_semantics,
                        cancellationToken => UpdateDocumentAsync(document, parentExpression, ConvertToRawKind.MultiLineWithoutLeadingWhitespace, provider, cancellationToken),
                        s_kindToEquivalenceKeyMap[ConvertToRawKind.MultiLineWithoutLeadingWhitespace],
                        priority),
                    token.Span);
            }
        }
    }

    private static async Task<Document> UpdateDocumentAsync(
        Document document,
        ExpressionSyntax expression,
        ConvertToRawKind kind,
        IConvertStringProvider provider,
        CancellationToken cancellationToken)
    {
        var options = await document.GetSyntaxFormattingOptionsAsync(cancellationToken).ConfigureAwait(false);
        var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

        var parsedDocument = await ParsedDocument.CreateAsync(document, cancellationToken).ConfigureAwait(false);
        var replacement = provider.Convert(parsedDocument, expression, kind, options, cancellationToken);
        return document.WithSyntaxRoot(root.ReplaceNode(expression, replacement));
    }

    protected override async Task FixAllAsync(
        Document document,
        ImmutableArray<TextSpan> fixAllSpans,
        SyntaxEditor editor,
        string? equivalenceKey,
        CancellationToken cancellationToken)
    {
        // Get the kind to be fixed from the equivalenceKey for the FixAll operation
        Debug.Assert(equivalenceKey != null);
        var kind = s_kindToEquivalenceKeyMap[equivalenceKey];

        var formattingOptions = await document.GetSyntaxFormattingOptionsAsync(cancellationToken).ConfigureAwait(false);
        var parsedDocument = await ParsedDocument.CreateAsync(document, cancellationToken).ConfigureAwait(false);

        foreach (var fixSpan in fixAllSpans)
        {
            var node = editor.OriginalRoot.FindNode(fixSpan);
            foreach (var expression in node.DepthFirstTraversalNodes().OfType<ExpressionSyntax>())
            {
                // Ensure we can convert the string literal
                if (!CanConvert(parsedDocument, expression, formattingOptions, out var canConvertParams, out var provider, cancellationToken))
                    continue;

                // Ensure we have a matching kind to fix for this literal
                var hasMatchingKind = kind switch
                {
                    ConvertToRawKind.SingleLine => canConvertParams.CanBeSingleLine,
                    ConvertToRawKind.MultiLineIndented => !canConvertParams.CanBeSingleLine,
                    // If we started with a multi-line string that we're changing semantics for.  Then any
                    // multi-line matches are something we can proceed with.  After all, we're updating all other
                    // ones that might change semantics, so we can def update the ones that won't change semantics.
                    ConvertToRawKind.MultiLineWithoutLeadingWhitespace =>
                        !canConvertParams.CanBeSingleLine || canConvertParams.CanBeMultiLineWithoutLeadingWhiteSpaces,
                    _ => throw ExceptionUtilities.UnexpectedValue(kind),
                };

                if (!hasMatchingKind)
                    continue;

                editor.ReplaceNode(
                    expression,
                    (current, _) =>
                    {
                        if (current is not ExpressionSyntax currentExpression)
                            return current;

                        var currentParsedDocument = parsedDocument.WithChangedRoot(
                            currentExpression.SyntaxTree.GetRoot(cancellationToken), cancellationToken);
                        var replacement = provider.Convert(currentParsedDocument, currentExpression, kind, formattingOptions, cancellationToken);
                        return replacement;
                    });
            }
        }
    }
}
