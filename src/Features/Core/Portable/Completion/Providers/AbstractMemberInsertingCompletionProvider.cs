// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeCleanup;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Completion.Providers;

internal abstract partial class AbstractMemberInsertingCompletionProvider : LSPCompletionProvider
{
    private static readonly ImmutableArray<CharacterSetModificationRule> s_commitRules = [CharacterSetModificationRule.Create(CharacterSetModificationKind.Replace, '(')];

    private static readonly ImmutableArray<CharacterSetModificationRule> s_filterRules = [CharacterSetModificationRule.Create(CharacterSetModificationKind.Remove, '(')];

    private static readonly CompletionItemRules s_defaultRules =
        CompletionItemRules.Create(
            commitCharacterRules: s_commitRules,
            filterCharacterRules: s_filterRules,
            enterKeyRule: EnterKeyRule.Never);

    private readonly SyntaxAnnotation _annotation = new();
    private readonly SyntaxAnnotation _replaceStartAnnotation = new();
    private readonly SyntaxAnnotation _replaceEndAnnotation = new();

    protected abstract SyntaxToken GetToken(CompletionItem completionItem, SyntaxTree tree, CancellationToken cancellationToken);

    protected abstract Task<ISymbol> GenerateMemberAsync(
        Document document, CompletionItem item, Compilation compilation, ISymbol member, INamedTypeSymbol containingType, CancellationToken cancellationToken);
    protected abstract TextSpan GetTargetSelectionSpan(SyntaxNode caretTarget);

    protected abstract SyntaxNode GetSyntax(SyntaxToken commonSyntaxToken);

    protected static CompletionItemRules GetRules()
        => s_defaultRules;

    public override async Task<CompletionChange> GetChangeAsync(Document document, CompletionItem item, char? commitKey = null, CancellationToken cancellationToken = default)
    {
        var (newDocument, newSpan) = await DetermineNewDocumentAsync(document, item, cancellationToken).ConfigureAwait(false);
        var newText = await newDocument.GetValueTextAsync(cancellationToken).ConfigureAwait(false);

        var changes = await newDocument.GetTextChangesAsync(document, cancellationToken).ConfigureAwait(false);
        var changesArray = changes.ToImmutableArray();
        var change = Utilities.Collapse(newText, changesArray);

        return CompletionChange.Create(change, changesArray, properties: ImmutableDictionary<string, string>.Empty, newSpan, includesCommitCharacter: true);
    }

    private async Task<(Document, TextSpan? caretPosition)> DetermineNewDocumentAsync(
        Document document,
        CompletionItem completionItem,
        CancellationToken cancellationToken)
    {
        var text = await document.GetValueTextAsync(cancellationToken).ConfigureAwait(false);

        // The span we're going to replace
        var line = text.Lines[MemberInsertionCompletionItem.GetLine(completionItem)];

        // Annotate the line we care about so we can find it after adding usings
        // We annotate the line in order to handle adding the generated code before our annotated token in the same line
        var tree = await document.GetRequiredSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
        var treeRoot = tree.GetRoot(cancellationToken);

        // DevDiv 958235: 
        //
        // void goo()
        // {
        // }
        // override $$
        //
        // If our text edit includes the trailing trivia of the close brace of goo(),
        // that token will be reconstructed. The ensuing tree diff will then count
        // the { } as replaced even though we didn't want it to. If the user
        // has collapsed the outline for goo, that means we'll edit the outlined 
        // region and weird stuff will happen. Therefore, we'll start with the first
        // token on the line in order to leave the token and its trivia alone.
        var lineStart = line.GetFirstNonWhitespacePosition();
        Contract.ThrowIfNull(lineStart);
        var endToken = GetToken(completionItem, tree, cancellationToken);
        var annotatedRoot = treeRoot.ReplaceToken(
            endToken, endToken.WithAdditionalAnnotations(_replaceEndAnnotation));

        var startToken = annotatedRoot.FindTokenOnRightOfPosition(lineStart.Value);
        annotatedRoot = annotatedRoot.ReplaceToken(
            startToken, startToken.WithAdditionalAnnotations(_replaceStartAnnotation));

        // Make sure the new document is frozen before we try to get the semantic model. This is to avoid trigger source
        // generator, which is expensive and not needed for calculating the change.  Pass in 'forceFreeze: true' to
        // ensure all further transformations we make do not run generators either.
        document = document.WithSyntaxRoot(annotatedRoot).WithFrozenPartialSemantics(forceFreeze: true, cancellationToken);

        var memberContainingDocument = await GenerateMemberAndUsingsAsync(document, completionItem, line, cancellationToken).ConfigureAwait(false);
        if (memberContainingDocument == null)
        {
            // Generating the new document failed because we somehow couldn't resolve
            // the underlying symbol's SymbolKey. At this point, we won't be able to 
            // make any changes, so just return the document we started with.
            return (document, null);
        }

        var result = await RemoveDestinationNodeAsync(memberContainingDocument, cancellationToken).ConfigureAwait(false);
        return result;
    }

    private async Task<Document?> GenerateMemberAndUsingsAsync(
        Document document,
        CompletionItem completionItem,
        TextLine line,
        CancellationToken cancellationToken)
    {
        var codeGenService = document.GetRequiredLanguageService<ICodeGenerationService>();

        // Resolve member and type in our new, forked, solution
        var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);

        var containingType = semanticModel.GetEnclosingSymbol<INamedTypeSymbol>(line.Start, cancellationToken);
        Contract.ThrowIfNull(containingType);

        var symbols = await SymbolCompletionItem.GetSymbolsAsync(completionItem, document, cancellationToken).ConfigureAwait(false);
        var member = symbols.FirstOrDefault();

        // If SymbolKey resolution failed, then bail.
        if (member == null)
            return null;

        // CodeGenerationOptions containing before and after
        var context = new CodeGenerationSolutionContext(
            document.Project.Solution,
            new CodeGenerationContext(
                autoInsertionLocation: false,
                beforeThisLocation: semanticModel.SyntaxTree.GetLocation(TextSpan.FromBounds(line.Start, line.Start))));

        var generatedMember = await GenerateMemberAsync(
            document, completionItem, semanticModel.Compilation, member, containingType, cancellationToken).ConfigureAwait(false);
        generatedMember = _annotation.AddAnnotationToSymbol(generatedMember);

        return generatedMember switch
        {
            IMethodSymbol method => await codeGenService.AddMethodAsync(context, containingType, method, cancellationToken).ConfigureAwait(false),
            IPropertySymbol property => await codeGenService.AddPropertyAsync(context, containingType, property, cancellationToken).ConfigureAwait(false),
            IEventSymbol @event => await codeGenService.AddEventAsync(context, containingType, @event, cancellationToken).ConfigureAwait(false),
            _ => document
        };
    }

    private TextSpan ComputeDestinationSpan(SyntaxNode insertionRoot, SourceText text)
    {
        var startToken = insertionRoot.GetAnnotatedTokens(_replaceStartAnnotation).FirstOrNull();
        Contract.ThrowIfNull(startToken);
        var endToken = insertionRoot.GetAnnotatedTokens(_replaceEndAnnotation).FirstOrNull();
        Contract.ThrowIfNull(endToken);

        var line = text.Lines.GetLineFromPosition(endToken.Value.Span.End);

        return TextSpan.FromBounds(startToken.Value.SpanStart, line.EndIncludingLineBreak);
    }

    private async Task<(Document Document, TextSpan? Selection)> RemoveDestinationNodeAsync(
        Document memberContainingDocument, CancellationToken cancellationToken)
    {
        // We now have a replacement node inserted into the document, but we still have the source code that triggered completion (with associated trivia).
        // We need to move the trivia to the new replacement and remove the original code.
        var root = await memberContainingDocument.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

        // Compute the destination span and node in the new tree.  Depending on the context, the destination node can end up
        // containing more code (and trivia) than what we want to remove.  For example
        // ```
        //     override Eq
        //     [Attribute] public void M() {}
        // ```
        // returns a destination node (array syntax) containing both the `override Eq` and the `[Attribute]` on the line below.
        var text = await memberContainingDocument.GetValueTextAsync(cancellationToken).ConfigureAwait(false);
        var destinationSpan = ComputeDestinationSpan(root, text);
        var destinationNode = root.FindNode(destinationSpan, true);
        var syntaxFacts = memberContainingDocument.Project.Services.GetRequiredService<ISyntaxFactsService>();

        // Given that the destination node can contain code we want to keep, we can't directly remove it using syntax tree manipulations.
        // It is difficult to create a valid syntax tree manipulation that does only a partial removal of the destination node / tokens.
        //
        // Instead it is much easier to remove the old completion source with a direct text edit (we know the exact span).
        // But in order to do that, we first must move trivia to the replacement node and format/simplify (requires tree annotations).

        // First find all tokens inside the node that intersect the span being deleted.  Move trivia from these tokens to the replacement node.
        // Tokens from the destination node, but outside the destination span should not be touched.
        var destinationTokens = destinationNode.DescendantTokens(destinationSpan);

        SyntaxTriviaList leadingTriviaToCopy = [];
        SyntaxTriviaList trailingTriviaToCopy = [];
        root = root.ReplaceTokens(destinationTokens, (original, originalAdjusted) =>
        {
            // Save the trivia for later application onto the replacement node and then delete.
            leadingTriviaToCopy = leadingTriviaToCopy.AddRange(original.LeadingTrivia);
            trailingTriviaToCopy = trailingTriviaToCopy.AddRange(original.TrailingTrivia);

            var trailingEndOfLine = originalAdjusted.TrailingTrivia.FirstOrNull(t => syntaxFacts.IsEndOfLineTrivia(t));
            var destinationWithoutTrivia = originalAdjusted.WithoutTrivia();
            // If there was an end of line attached to the destination token, keep it as otherwise lines below
            // may get moved up to the same line as the destination span line we're removing later.
            if (trailingEndOfLine is not null)
                destinationWithoutTrivia = destinationWithoutTrivia.WithTrailingTrivia(trailingEndOfLine.Value);
            return destinationWithoutTrivia;
        });

        // Add the saved trivia on to the replacement node.
        var replacingNode = root.GetAnnotatedNodes(_annotation).Single();
        root = root.ReplaceNode(replacingNode, replacingNode.WithLeadingTrivia(leadingTriviaToCopy).WithTrailingTrivia(trailingTriviaToCopy));

        // We've finished the major modifications, we can now format and simplify.
        var document = memberContainingDocument.WithSyntaxRoot(root);
        document = await Simplifier.ReduceAsync(document, Simplifier.Annotation, cancellationToken).ConfigureAwait(false);
        document = await Formatter.FormatAsync(document, Formatter.Annotation, cancellationToken).ConfigureAwait(false);

        // Formatting/simplification changed the tree, so recompute the destination span.
        root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        text = await document.GetValueTextAsync(cancellationToken).ConfigureAwait(false);
        destinationSpan = ComputeDestinationSpan(root, text);

        // We have basically the final tree.  Calculate the new caret position while we still have the annotations.
        TextSpan? newSpan = null;
        var caretTarget = root.GetAnnotatedNodes(_annotation).FirstOrDefault();
        if (caretTarget != null)
        {
            var targetSelectionSpan = GetTargetSelectionSpan(caretTarget);

            if (targetSelectionSpan.Start > 0 && targetSelectionSpan.End <= text.Length)
            {
                // The new replacement method should always be inserted before the destination span we're removing.
                // This means the end selection position in the inserted method should be safe to return as-is.
                Debug.Assert(targetSelectionSpan.End < destinationSpan.Start);
                newSpan = targetSelectionSpan;
            }
        }

        // Now we can finally delete the destination span.  It is safe to delete the whole line here (instead of just the destination span)
        // as override completion will not be shown with unrelated text preceding or following the override trigger.
        text.GetLineAndOffset(destinationSpan.Start, out var lineNumber, out _);
        var textChange = new TextChange(text.Lines[lineNumber].SpanIncludingLineBreak, string.Empty);

        text = text.WithChanges(textChange);
        return (document.WithText(text), newSpan);
    }

    internal override Task<CompletionDescription> GetDescriptionWorkerAsync(Document document, CompletionItem item, CompletionOptions options, SymbolDescriptionOptions displayOptions, CancellationToken cancellationToken)
        => MemberInsertionCompletionItem.GetDescriptionAsync(item, document, displayOptions, cancellationToken);
}
