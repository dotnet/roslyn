// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.ConvertToRawString;

[ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = PredefinedCodeRefactoringProviderNames.ConvertToRawString), Shared]
[method: ImportingConstructor]
[method: SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
internal sealed partial class ConvertStringToRawStringCodeRefactoringProvider() : SyntaxEditorBasedCodeRefactoringProvider
{
    private static readonly string[] s_kindToEquivalenceKeyMap;

    static ConvertStringToRawStringCodeRefactoringProvider()
    {
        var count = (int)ConvertToRawKind.ContainsEscapedEndOfLineCharacter * 2;
        s_kindToEquivalenceKeyMap = new string[count];
        for (var i = 0; i < count; i++)
            s_kindToEquivalenceKeyMap[i] = i.ToString(CultureInfo.InvariantCulture);
    }

    private static readonly ImmutableArray<IConvertStringProvider> s_convertStringProviders =
        [ConvertRegularStringToRawStringProvider.Instance, ConvertInterpolatedStringToRawStringProvider.Instance];

    protected override ImmutableArray<RefactorAllScope> SupportedRefactorAllScopes => AllRefactorAllScopes;

    protected override CodeActionCleanup Cleanup => CodeActionCleanup.SyntaxOnly;

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

        var kind = convertParams.ContainsEscapedEndOfLineCharacter
            ? ConvertToRawKind.ContainsEscapedEndOfLineCharacter
            : 0;

        if (convertParams.CanBeSingleLine)
        {
            context.RegisterRefactoring(
                CodeAction.Create(
                    CSharpFeaturesResources.Convert_to_raw_string,
                    cancellationToken => UpdateDocumentAsync(document, parentExpression, kind | ConvertToRawKind.SingleLine, provider, cancellationToken),
                    s_kindToEquivalenceKeyMap[(int)(kind | ConvertToRawKind.SingleLine)],
                    priority),
                token.Span);
        }
        else
        {
            context.RegisterRefactoring(
                CodeAction.Create(
                    CSharpFeaturesResources.Convert_to_raw_string,
                    cancellationToken => UpdateDocumentAsync(document, parentExpression, kind | ConvertToRawKind.MultiLineIndented, provider, cancellationToken),
                    s_kindToEquivalenceKeyMap[(int)(kind | ConvertToRawKind.MultiLineIndented)],
                    priority),
                token.Span);

            if (convertParams.CanBeMultiLineWithoutLeadingWhiteSpaces)
            {
                context.RegisterRefactoring(
                    CodeAction.Create(
                        CSharpFeaturesResources.without_leading_whitespace_may_change_semantics,
                        cancellationToken => UpdateDocumentAsync(document, parentExpression, kind | ConvertToRawKind.MultiLineWithoutLeadingWhitespace, provider, cancellationToken),
                        s_kindToEquivalenceKeyMap[(int)(kind | ConvertToRawKind.MultiLineWithoutLeadingWhitespace)],
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

    protected override async Task RefactorAllAsync(
        Document document,
        ImmutableArray<TextSpan> fixAllSpans,
        SyntaxEditor editor,
        string? equivalenceKey,
        CancellationToken cancellationToken)
    {
        // Get the kind to be fixed from the equivalenceKey for the FixAll operation
        Debug.Assert(equivalenceKey != null);
        var kind = (ConvertToRawKind)int.Parse(equivalenceKey, CultureInfo.InvariantCulture);

        var formattingOptions = await document.GetSyntaxFormattingOptionsAsync(cancellationToken).ConfigureAwait(false);
        var parsedDocument = await ParsedDocument.CreateAsync(document, cancellationToken).ConfigureAwait(false);

        using var _ = PooledDictionary<ExpressionSyntax, IConvertStringProvider>.GetInstance(out var expressionToProvider);

        foreach (var fixSpan in fixAllSpans)
        {
            var node = editor.OriginalRoot.FindNode(fixSpan);
            foreach (var expression in node.DepthFirstTraversalNodes().OfType<ExpressionSyntax>())
            {
                // Ensure we can convert the string literal
                if (!CanConvert(parsedDocument, expression, formattingOptions, out var canConvertParams, out var provider, cancellationToken))
                    continue;

                // If the user started on a string that didn't have an explicit \n in it, then don't update other string
                // literals that do.  Note: if they did start on a string with an explicit \n, then we should update all
                // other literals, regardless of whether they had an explicit \n or not.
                if ((kind & ConvertToRawKind.ContainsEscapedEndOfLineCharacter) == 0 && canConvertParams.ContainsEscapedEndOfLineCharacter)
                    continue;

                // Ensure we have a matching kind to fix for this literal.
                // Remove the end of line flag so we can trivially switch on the rest below.
                var hasMatchingKind = (kind & ~ConvertToRawKind.ContainsEscapedEndOfLineCharacter) switch
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

                expressionToProvider[expression] = provider;
            }
        }

        foreach (var (expression, provider) in expressionToProvider)
        {
            // Don't update a string if we're updating a parent string around it.  We may not be able to find
            // it if we've already updated the parent string.
            if (expression.Ancestors().OfType<ExpressionSyntax>().Any(static (e, expressionToProvider) => expressionToProvider.ContainsKey(e), expressionToProvider))
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
