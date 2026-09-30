// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Collections;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.RemoveUnnecessaryUnsafeModifier;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.RemoveUnnecessaryUnsafeModifier), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class CSharpRemoveUnnecessaryUnsafeModifierCodeFixProvider() : CodeFixProvider
{
    private const string AddSafetyCommentEquivalenceKey = nameof(AddSafetyCommentEquivalenceKey);

    public override ImmutableArray<string> FixableDiagnosticIds => [IDEDiagnosticIds.RemoveUnnecessaryUnsafeModifier];

    public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        context.RegisterCodeFix(CodeAction.Create(
            AnalyzersResources.Remove_unnecessary_unsafe_modifier,
            cancellationToken => FixAllAsync(context.Document, context.Diagnostics, cancellationToken),
            nameof(AnalyzersResources.Remove_unnecessary_unsafe_modifier)),
            context.Diagnostics);

        var compilation = await context.Document.Project.GetRequiredCompilationAsync(context.CancellationToken).ConfigureAwait(false);
        if (compilation.SourceModule.MemorySafetyRulesVersion is MemorySafetyRulesVersion.Version2)
        {
            context.RegisterCodeFix(CodeAction.Create(
                CSharpCodeFixesResources.Add_safety_documentation,
                cancellationToken => AddSafetyCommentsAsync(context.Document, context.Diagnostics, cancellationToken),
                AddSafetyCommentEquivalenceKey),
                context.Diagnostics);
        }
    }

    private static async Task<Document> FixAllAsync(Document document, ImmutableArray<Diagnostic> diagnostics, CancellationToken cancellationToken)
    {
        var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

        var editor = new SyntaxEditor(root, document.Project.Solution.Services);

        FixAll(editor, diagnostics.Select(static d => d.AdditionalLocations[0].SourceSpan));

        return document.WithSyntaxRoot(editor.GetChangedRoot());
    }

    private static async Task<Document> AddSafetyCommentsAsync(
        Document document, ImmutableArray<Diagnostic> diagnostics, CancellationToken cancellationToken)
    {
        var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var options = await document.GetLineFormattingOptionsAsync(cancellationToken).ConfigureAwait(false);
        var sourceText = await document.GetValueTextAsync(cancellationToken).ConfigureAwait(false);

        var editor = new SyntaxEditor(root, document.Project.Solution.Services);
        foreach (var diagnostic in diagnostics)
        {
            var node = root.FindNode(diagnostic.AdditionalLocations[0].SourceSpan, getInnermostNodeForTie: true);
            editor.ReplaceNode(node, AddSafetyComment(node, sourceText, options.NewLine));
        }

        return document.WithSyntaxRoot(editor.GetChangedRoot());
    }

    private static SyntaxNode AddSafetyComment(SyntaxNode node, SourceText sourceText, string newLine)
    {
        var leadingTrivia = node.GetLeadingTrivia();
        for (var i = leadingTrivia.Count - 1; i >= 0; i--)
        {
            var trivia = leadingTrivia[i];
            if (trivia.GetStructure() is DocumentationCommentTriviaSyntax documentationComment &&
                documentationComment.IsKind(SyntaxKind.MultiLineDocumentationCommentTrivia))
            {
                return node.WithLeadingTrivia(leadingTrivia.Replace(trivia, AddSafetyElement(trivia, newLine)));
            }
        }

        var indentation = sourceText.GetLeadingWhitespaceOfLineAtPosition(node.SpanStart);
        var safetyComment = SyntaxFactory.ParseLeadingTrivia($"/// <safety></safety>{newLine}").Single();
        var newLeadingTrivia = SyntaxFactory.TriviaList(
            SyntaxFactory.Whitespace(indentation),
            safetyComment);

        var finalLeadingTrivia = leadingTrivia.ToList();
        var insertionIndex = finalLeadingTrivia.Count;

        if (finalLeadingTrivia.Count > 0 && finalLeadingTrivia[^1].IsKind(SyntaxKind.WhitespaceTrivia))
            insertionIndex--;

        finalLeadingTrivia.InsertRange(insertionIndex, newLeadingTrivia);
        return node.WithLeadingTrivia(finalLeadingTrivia);
    }

    private static SyntaxTrivia AddSafetyElement(SyntaxTrivia documentationComment, string newLine)
    {
        var text = documentationComment.ToFullString();
        var closingDelimiterIndex = text.LastIndexOf("*/", StringComparison.Ordinal);
        if (closingDelimiterIndex < 0)
            return documentationComment;

        var closingLineStart = text.LastIndexOf('\n', closingDelimiterIndex) + 1;
        var previousLineEnd = closingLineStart - 1;
        if (previousLineEnd > 0 && text[previousLineEnd - 1] == '\r')
            previousLineEnd--;

        var previousLineStart = previousLineEnd > 0
            ? text.LastIndexOf('\n', previousLineEnd - 1) + 1
            : 0;
        var previousLine = text[previousLineStart..previousLineEnd];
        var contentStart = 0;
        while (contentStart < previousLine.Length && char.IsWhiteSpace(previousLine[contentStart]))
            contentStart++;

        var prefixLength = contentStart;
        if (contentStart < previousLine.Length && previousLine[contentStart] == '*')
        {
            prefixLength++;
            if (prefixLength < previousLine.Length && previousLine[prefixLength] == ' ')
                prefixLength++;
        }

        var prefix = previousLine[..prefixLength];
        var updatedText = text.Insert(closingLineStart, $"{prefix}<safety></safety>{newLine}");
        return SyntaxFactory.ParseLeadingTrivia(updatedText)
            .Single(static trivia => trivia.IsKind(SyntaxKind.MultiLineDocumentationCommentTrivia));
    }

    private static void FixAll(SyntaxEditor editor, IEnumerable<TextSpan> spans)
    {
        var root = editor.OriginalRoot;

        // Process from inside out.  Don't remove unsafe modifiers on containing nodes if we removed it from an inner
        // node. The inner removal may make the outer one necessary.

        var intervalTree = new TextSpanMutableIntervalTree();

        foreach (var span in spans.OrderByDescending(d => d.Start))
        {
            if (intervalTree.HasIntervalThatIntersectsWith(span))
                continue;

            intervalTree.AddIntervalInPlace(span);

            var node = root.FindNode(span, getInnermostNodeForTie: true);
            editor.ReplaceNode(
                node,
                static (current, generator) => generator.WithModifiers(current, generator.GetModifiers(current).WithIsUnsafe(false)));
        }
    }

    public override FixAllProvider? GetFixAllProvider()
        => new RemoveUnnecessaryUnsafeModifierFixAllProvider();

    private sealed class RemoveUnnecessaryUnsafeModifierFixAllProvider : FixAllProvider
    {
        private readonly RemoveUnnecessaryUnsafeModifierSuppressionsFixAllProvider _removeUnsafeFixAllProvider = new();

        public override Task<CodeAction?> GetFixAsync(FixAllContext fixAllContext)
            => fixAllContext.CodeActionEquivalenceKey == AddSafetyCommentEquivalenceKey
                ? WellKnownFixAllProviders.BatchFixer.GetFixAsync(fixAllContext)
                : _removeUnsafeFixAllProvider.GetFixAsync(fixAllContext);
    }

    /// <summary>
    /// Fix-all for removing unnecessary `unsafe` modifiers works in a fairly specialized fashion.  The core problem is
    /// that it's normal to have situations where a `unsafe` operator is unnecessary in one linked document in one
    /// project, but necessary in another.  Consider cases where some projects have access to modern C# with 'ref', while
    /// others may fall back to pointers.  Removing for the 'ref' case would break the pointer case.
    ///
    /// To deal with this, we consider all linked documents together.  If an `unsafe` modifier is unnecessary in *all*
    /// linked documents, then we can remove it.  Otherwise, we must keep it.
    /// </summary>
    private sealed class RemoveUnnecessaryUnsafeModifierSuppressionsFixAllProvider : MultiProjectSafeFixAllProvider
    {
#if !CODE_STYLE
        internal override CodeActionCleanup Cleanup => CodeActionCleanup.SyntaxOnly;
#endif

        protected override void FixAll(SyntaxEditor editor, IEnumerable<TextSpan> commonSpans)
            => CSharpRemoveUnnecessaryUnsafeModifierCodeFixProvider.FixAll(editor, commonSpans);
    }
}
