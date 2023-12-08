// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.AddImport;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeCleanup;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Completion.Providers
{
    internal abstract partial class AbstractMemberInsertingCompletionProvider : LSPCompletionProvider
    {
        private readonly SyntaxAnnotation _annotation = new();
        private readonly SyntaxAnnotation _otherAnnotation = new();

        protected abstract SyntaxToken GetToken(CompletionItem completionItem, SyntaxTree tree, CancellationToken cancellationToken);

        protected abstract Task<ISymbol> GenerateMemberAsync(ISymbol member, INamedTypeSymbol containingType, Document document, CompletionItem item, CancellationToken cancellationToken);
        protected abstract int GetTargetCaretPosition(SyntaxNode caretTarget);
        protected abstract SyntaxNode GetSyntax(SyntaxToken commonSyntaxToken);

        public AbstractMemberInsertingCompletionProvider()
        {
        }

        public override async Task<CompletionChange> GetChangeAsync(Document document, CompletionItem item, char? commitKey = null, CancellationToken cancellationToken = default)
        {
            // TODO: pass fallback options: https://github.com/dotnet/roslyn/issues/60786
            var globalOptions = document.Project.Solution.Services.GetService<ILegacyGlobalCleanCodeGenerationOptionsWorkspaceService>();
            var fallbackOptions = globalOptions?.Provider ?? CodeActionOptions.DefaultProvider;

            var newDocument = await DetermineNewDocumentAsync(document, item, fallbackOptions, cancellationToken).ConfigureAwait(false);
            var newText = await newDocument.GetValueTextAsync(cancellationToken).ConfigureAwait(false);
            var newRoot = await newDocument.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            int? newPosition = null;

            // Attempt to find the inserted node and move the caret appropriately
            if (newRoot != null)
            {
                var caretTarget = newRoot.GetAnnotatedNodes(_annotation).FirstOrDefault();
                if (caretTarget != null)
                {
                    var targetPosition = GetTargetCaretPosition(caretTarget);

                    // Something weird happened and we failed to get a valid position.
                    // Bail on moving the caret.
                    if (targetPosition > 0 && targetPosition <= newText.Length)
                    {
                        newPosition = targetPosition;
                    }
                }
            }

            var changes = await newDocument.GetTextChangesAsync(document, cancellationToken).ConfigureAwait(false);
            var changesArray = changes.ToImmutableArray();
            var change = Utilities.Collapse(newText, changesArray);

            return CompletionChange.Create(change, changesArray, newPosition, includesCommitCharacter: true);
        }

        private async Task<Document> DetermineNewDocumentAsync(
            Document document,
            CompletionItem completionItem,
            CleanCodeGenerationOptionsProvider fallbackOptions,
            CancellationToken cancellationToken)
        {
            var text = await document.GetValueTextAsync(cancellationToken).ConfigureAwait(false);

            // The span we're going to replace
            var line = text.Lines[MemberInsertionCompletionItem.GetLine(completionItem)];

            // Annotate the line we care about so we can find it after adding usings
            var tree = await document.GetRequiredSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
            var token = GetToken(completionItem, tree, cancellationToken);
            var annotatedRoot = tree.GetRoot(cancellationToken).ReplaceToken(token, token.WithAdditionalAnnotations(_otherAnnotation));
            // Make sure the new document is frozen before we try to get the semantic model. This is to 
            // avoid trigger source generator, which is expensive and not needed for calculating the change.
            document = document.WithSyntaxRoot(annotatedRoot).WithFrozenPartialSemantics(cancellationToken);

            var memberContainingDocument = await GenerateMemberAndUsingsAsync(document, completionItem, line, fallbackOptions, cancellationToken).ConfigureAwait(false);
            if (memberContainingDocument == null)
            {
                // Generating the new document failed because we somehow couldn't resolve
                // the underlying symbol's SymbolKey. At this point, we won't be able to 
                // make any changes, so just return the document we started with.
                return document;
            }

            var memberContainingDocumentCleanupOptions = await document.GetCodeCleanupOptionsAsync(fallbackOptions, cancellationToken).ConfigureAwait(false);
            var insertionRoot = await GetTreeWithAddedSyntaxNodeRemovedAsync(memberContainingDocument, memberContainingDocumentCleanupOptions, cancellationToken).ConfigureAwait(false);
            var insertionText = await GenerateInsertionTextAsync(memberContainingDocument, memberContainingDocumentCleanupOptions, cancellationToken).ConfigureAwait(false);

            var destinationSpan = ComputeDestinationSpan(insertionRoot);

            var finalText = insertionRoot.GetText(text.Encoding)
                .Replace(destinationSpan, insertionText.Trim());

            document = document.WithText(finalText);
            var newRoot = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var declaration = GetSyntax(newRoot.FindToken(destinationSpan.End));

            document = document.WithSyntaxRoot(newRoot.ReplaceNode(declaration, declaration.WithAdditionalAnnotations(_annotation)));
            var formattingOptions = await document.GetSyntaxFormattingOptionsAsync(fallbackOptions, cancellationToken).ConfigureAwait(false);
            return await Formatter.FormatAsync(document, _annotation, formattingOptions, cancellationToken).ConfigureAwait(false);
        }

        private async Task<Document?> GenerateMemberAndUsingsAsync(
            Document document,
            CompletionItem completionItem,
            TextLine line,
            CodeAndImportGenerationOptionsProvider fallbackOptions,
            CancellationToken cancellationToken)
        {
            var codeGenService = document.GetRequiredLanguageService<ICodeGenerationService>();

            // Resolve member and type in our new, forked, solution
            var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            var containingType = semanticModel.GetEnclosingSymbol<INamedTypeSymbol>(line.Start, cancellationToken);
            Contract.ThrowIfNull(containingType);

            var symbols = await SymbolCompletionItem.GetSymbolsAsync(completionItem, document, cancellationToken).ConfigureAwait(false);
            var overriddenMember = symbols.FirstOrDefault();

            if (overriddenMember == null)
            {
                // Unfortunately, SymbolKey resolution failed. Bail.
                return null;
            }

            // CodeGenerationOptions containing before and after
            var context = new CodeGenerationSolutionContext(
                document.Project.Solution,
                new CodeGenerationContext(
                    contextLocation: semanticModel.SyntaxTree.GetLocation(TextSpan.FromBounds(line.Start, line.Start))),
                fallbackOptions);

            var generatedMember = await GenerateMemberAsync(overriddenMember, containingType, document, completionItem, cancellationToken).ConfigureAwait(false);
            generatedMember = _annotation.AddAnnotationToSymbol(generatedMember);

            Document? memberContainingDocument = null;
            if (generatedMember.Kind == SymbolKind.Method)
            {
                memberContainingDocument = await codeGenService.AddMethodAsync(context, containingType, (IMethodSymbol)generatedMember, cancellationToken).ConfigureAwait(false);
            }
            else if (generatedMember.Kind == SymbolKind.Property)
            {
                memberContainingDocument = await codeGenService.AddPropertyAsync(context, containingType, (IPropertySymbol)generatedMember, cancellationToken).ConfigureAwait(false);
            }
            else if (generatedMember.Kind == SymbolKind.Event)
            {
                memberContainingDocument = await codeGenService.AddEventAsync(context, containingType, (IEventSymbol)generatedMember, cancellationToken).ConfigureAwait(false);
            }

            return memberContainingDocument;
        }

        private TextSpan ComputeDestinationSpan(SyntaxNode insertionRoot)
        {
            var targetToken = insertionRoot.GetAnnotatedTokens(_otherAnnotation).FirstOrNull();
            Contract.ThrowIfNull(targetToken);

            var text = insertionRoot.GetText();
            var line = text.Lines.GetLineFromPosition(targetToken.Value.Span.End);

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
            var position = line.GetFirstNonWhitespacePosition();
            Contract.ThrowIfNull(position);

            var firstToken = insertionRoot.FindToken(position.Value);
            return TextSpan.FromBounds(firstToken.SpanStart, line.End);
        }

        private async Task<string> GenerateInsertionTextAsync(
            Document memberContainingDocument, CodeCleanupOptions cleanupOptions, CancellationToken cancellationToken)
        {
            memberContainingDocument = await Simplifier.ReduceAsync(memberContainingDocument, Simplifier.Annotation, cleanupOptions.SimplifierOptions, cancellationToken).ConfigureAwait(false);
            memberContainingDocument = await Formatter.FormatAsync(memberContainingDocument, Formatter.Annotation, cleanupOptions.FormattingOptions, cancellationToken).ConfigureAwait(false);

            var root = await memberContainingDocument.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            return root.GetAnnotatedNodes(_annotation).Single().ToString().Trim();
        }

        private async Task<SyntaxNode> GetTreeWithAddedSyntaxNodeRemovedAsync(
            Document document, CodeCleanupOptions cleanupOptions, CancellationToken cancellationToken)
        {
            // Added imports are annotated for simplification too. Therefore, we simplify the document
            // before removing added member node to preserve those imports in the document.
            document = await Simplifier.ReduceAsync(document, Simplifier.Annotation, cleanupOptions.SimplifierOptions, cancellationToken).ConfigureAwait(false);

            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var members = root.GetAnnotatedNodes(_annotation).AsImmutable();

            root = root.RemoveNodes(members, SyntaxRemoveOptions.KeepUnbalancedDirectives);
            Contract.ThrowIfNull(root);

            var dismemberedDocument = document.WithSyntaxRoot(root);

            dismemberedDocument = await Formatter.FormatAsync(dismemberedDocument, Formatter.Annotation, cleanupOptions.FormattingOptions, cancellationToken).ConfigureAwait(false);
            return await dismemberedDocument.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        }

        private static readonly ImmutableArray<CharacterSetModificationRule> s_commitRules = ImmutableArray.Create(
            CharacterSetModificationRule.Create(CharacterSetModificationKind.Replace, '('));

        private static readonly ImmutableArray<CharacterSetModificationRule> s_filterRules = ImmutableArray.Create(
            CharacterSetModificationRule.Create(CharacterSetModificationKind.Remove, '('));

        private static readonly CompletionItemRules s_defaultRules =
            CompletionItemRules.Create(
                commitCharacterRules: s_commitRules,
                filterCharacterRules: s_filterRules,
                enterKeyRule: EnterKeyRule.Never);

        protected static CompletionItemRules GetRules()
            => s_defaultRules;

        internal override Task<CompletionDescription> GetDescriptionWorkerAsync(Document document, CompletionItem item, CompletionOptions options, SymbolDescriptionOptions displayOptions, CancellationToken cancellationToken)
            => MemberInsertionCompletionItem.GetDescriptionAsync(item, document, displayOptions, cancellationToken);
    }
}
