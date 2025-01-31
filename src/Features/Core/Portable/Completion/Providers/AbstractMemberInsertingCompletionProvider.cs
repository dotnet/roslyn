// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
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
        var newDocument = await DetermineNewDocumentAsync(document, item, cancellationToken).ConfigureAwait(false);
        var newText = await newDocument.GetValueTextAsync(cancellationToken).ConfigureAwait(false);
        var newRoot = await newDocument.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

        TextSpan? newSpan = null;

        // Attempt to find the inserted node and move the caret appropriately
        if (newRoot != null)
        {
            var caretTarget = newRoot.GetAnnotatedNodes(_annotation).FirstOrDefault();
            if (caretTarget != null)
            {
                var targetSelectionSpan = GetTargetSelectionSpan(caretTarget);

                if (targetSelectionSpan.Start > 0 && targetSelectionSpan.End <= newText.Length)
                {
                    newSpan = targetSelectionSpan;
                }
            }
        }

        var changes = await newDocument.GetTextChangesAsync(document, cancellationToken).ConfigureAwait(false);
        var changesArray = changes.ToImmutableArray();
        var change = Utilities.Collapse(newText, changesArray);

        return CompletionChange.Create(change, changesArray, properties: ImmutableDictionary<string, string>.Empty, newSpan, includesCommitCharacter: true);
    }

    private async Task<Document> DetermineNewDocumentAsync(
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
            return document;
        }

        var memberContainingDocumentCleanupOptions = await document.GetCodeCleanupOptionsAsync(cancellationToken).ConfigureAwait(false);

        document = await RemoveDestinationNodeAsync(memberContainingDocument, memberContainingDocumentCleanupOptions, cancellationToken).ConfigureAwait(false);
        var formattingOptions = await document.GetSyntaxFormattingOptionsAsync(cancellationToken).ConfigureAwait(false);
        document = await Simplifier.ReduceAsync(document, _annotation, memberContainingDocumentCleanupOptions.SimplifierOptions, cancellationToken).ConfigureAwait(false);
        return await Formatter.FormatAsync(document, _annotation, formattingOptions, cancellationToken).ConfigureAwait(false);
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

    private TextSpan ComputeDestinationSpan(SyntaxNode insertionRoot)
    {
        var startToken = insertionRoot.GetAnnotatedTokens(_replaceStartAnnotation).FirstOrNull();
        Contract.ThrowIfNull(startToken);
        var endToken = insertionRoot.GetAnnotatedTokens(_replaceEndAnnotation).FirstOrNull();
        Contract.ThrowIfNull(endToken);

        var text = insertionRoot.GetText();
        var line = text.Lines.GetLineFromPosition(endToken.Value.Span.End);

        return TextSpan.FromBounds(startToken.Value.SpanStart, line.EndIncludingLineBreak);
    }

    private async Task<Document> RemoveDestinationNodeAsync(
        Document memberContainingDocument, CodeCleanupOptions cleanupOptions, CancellationToken cancellationToken)
    {
        // At this stage we have created the replacing node, but we also have the source node that triggered the completion
        // To remove the old node, we need to port over the trivia and then recalculate the node to remove
        // since we may have adjusted the position of the node by inserting trivia before the new node

        var root = await memberContainingDocument.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var destinationSpan = ComputeDestinationSpan(root);
        var destinationNode = root.FindNode(destinationSpan, true);
        var replacingNode = root.GetAnnotatedNodes(_annotation).Single();
        // WithTriviaFrom does not completely fit our purpose because we could be missing trivia from interior missing tokens,
        // with all the last tokens being missing, and thus only having part of the picture
        root = root.ReplaceNode(replacingNode, replacingNode.WithTriviaFromIncludingMissingTokens(destinationNode));

        // Now that we have replaced the node, find the destination node again
        destinationSpan = ComputeDestinationSpan(root);
        destinationNode = root.FindNode(destinationSpan);
        SyntaxNode newRoot;
        if (destinationSpan.Contains(destinationNode.Span))
        {
            newRoot = root.RemoveNode(destinationNode, SyntaxRemoveOptions.KeepNoTrivia)!;
        }
        else
        {
            var tokens = destinationNode.DescendantTokens(destinationSpan);
            newRoot = root.ReplaceTokens(tokens, static (_, _) => default);
        }

        var document = memberContainingDocument.WithSyntaxRoot(newRoot);

        document = await Simplifier.ReduceAsync(document, Simplifier.Annotation, cleanupOptions.SimplifierOptions, cancellationToken).ConfigureAwait(false);
        document = await Formatter.FormatAsync(document, Formatter.Annotation, cleanupOptions.FormattingOptions, cancellationToken).ConfigureAwait(false);

        return document;
    }

    internal override Task<CompletionDescription> GetDescriptionWorkerAsync(Document document, CompletionItem item, CompletionOptions options, SymbolDescriptionOptions displayOptions, CancellationToken cancellationToken)
        => MemberInsertionCompletionItem.GetDescriptionAsync(item, document, displayOptions, cancellationToken);
}
