﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using System.Collections.Immutable;
using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.Completion.Providers
{
    internal abstract partial class AbstractMemberInsertingCompletionProvider : CommonCompletionProvider
    {
        private readonly SyntaxAnnotation _annotation = new SyntaxAnnotation();
        private readonly SyntaxAnnotation _otherAnnotation = new SyntaxAnnotation();

        protected abstract SyntaxToken GetToken(CompletionItem completionItem, SyntaxTree tree, CancellationToken cancellationToken);

        protected abstract Task<ISymbol> GenerateMemberAsync(ISymbol member, INamedTypeSymbol containingType, Document document, CompletionItem item, CancellationToken cancellationToken);
        protected abstract int GetTargetCaretPosition(SyntaxNode caretTarget);
        protected abstract SyntaxNode GetSyntax(SyntaxToken commonSyntaxToken);

        public AbstractMemberInsertingCompletionProvider()
        {
        }

        public override async Task<CompletionChange> GetChangeAsync(Document document, CompletionItem item, char? commitKey = default, CancellationToken cancellationToken = default)
        {
            var newDocument = await DetermineNewDocumentAsync(document, item, cancellationToken).ConfigureAwait(false);
            var newText = await newDocument.GetTextAsync(cancellationToken).ConfigureAwait(false);
            var newRoot = await newDocument.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            int? newPosition = null;

            // Attempt to find the inserted node and move the caret appropriately
            if (newRoot != null)
            {
                var caretTarget = newRoot.GetAnnotatedNodesAndTokens(_annotation).FirstOrNullable();
                if (caretTarget != null)
                {
                    var targetPosition = GetTargetCaretPosition(caretTarget.Value.AsNode());

                    // Something weird happened and we failed to get a valid position.
                    // Bail on moving the caret.
                    if (targetPosition > 0 && targetPosition <= newText.Length)
                    {
                        newPosition = targetPosition;
                    }
                }
            }

            var changes = await newDocument.GetTextChangesAsync(document, cancellationToken).ConfigureAwait(false);
            var change = Collapse(newText, changes.ToList());

            return CompletionChange.Create(change, newPosition, includesCommitCharacter: true);
        }

        private TextChange Collapse(SourceText newText, List<TextChange> changes)
        {
            if (changes.Count == 0)
            {
                return new TextChange(new TextSpan(0, 0), "");
            }
            else if (changes.Count == 1)
            {
                return changes[0];
            }

            // The span we want to replace goes from the start of the first span to the end of
            // the  last span.
            var totalOldSpan = TextSpan.FromBounds(changes.First().Span.Start, changes.Last().Span.End);

            // We figure out the text we're replacing with by actually just figuring out the
            // new span in the newText and grabbing the text out of that.  The newSpan will
            // start from the same position as the oldSpan, but it's length will be the old
            // span's length + all the deltas we accumulate through each text change.  i.e.
            // if the first change adds 2 characters and the second change adds 4, then 
            // the newSpan will be 2+4=6 characters longer than the old span.
            var sumOfDeltas = changes.Sum(c => c.NewText.Length - c.Span.Length);
            var totalNewSpan = new TextSpan(totalOldSpan.Start, totalOldSpan.Length + sumOfDeltas);

            return new TextChange(totalOldSpan, newText.ToString(totalNewSpan));
        }

        private async Task<Document> DetermineNewDocumentAsync(Document document, CompletionItem completionItem, CancellationToken cancellationToken)
        {
            var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);

            // The span we're going to replace
            var line = text.Lines[MemberInsertionCompletionItem.GetLine(completionItem)];

            // Annotate the line we care about so we can find it after adding usings
            var tree = await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
            var token = GetToken(completionItem, tree, cancellationToken);
            var annotatedRoot = tree.GetRoot(cancellationToken).ReplaceToken(token, token.WithAdditionalAnnotations(_otherAnnotation));
            document = document.WithSyntaxRoot(annotatedRoot);

            var memberContainingDocument = await GenerateMemberAndUsingsAsync(document, completionItem, line, cancellationToken).ConfigureAwait(false);

            var insertionRoot = await PrepareTreeForMemberInsertionAsync(memberContainingDocument, cancellationToken).ConfigureAwait(false);
            var insertionText = await GenerateInsertionTextAsync(memberContainingDocument, cancellationToken).ConfigureAwait(false);

            var destinationSpan = ComputeDestinationSpan(insertionRoot, insertionText);

            var finalText = insertionRoot.GetText(text.Encoding)
                .Replace(destinationSpan, insertionText.Trim());

            document = document.WithText(finalText);
            var newRoot = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var declaration = GetSyntax(newRoot.FindToken(destinationSpan.End));

            document = document.WithSyntaxRoot(newRoot.ReplaceNode(declaration, declaration.WithAdditionalAnnotations(_annotation)));
            return Formatter.FormatAsync(document, _annotation, cancellationToken: cancellationToken).WaitAndGetResult(cancellationToken);
        }

        private async Task<Document> GenerateMemberAndUsingsAsync(
            Document document,
            CompletionItem completionItem,
            TextLine line,
            CancellationToken cancellationToken)
        {
            var syntaxFactory = document.GetLanguageService<SyntaxGenerator>();
            var codeGenService = document.GetLanguageService<ICodeGenerationService>();

            // Resolve member and type in our new, forked, solution
            var semanticModel = document.GetSemanticModelAsync(cancellationToken).WaitAndGetResult(cancellationToken);
            var containingType = semanticModel.GetEnclosingSymbol<INamedTypeSymbol>(line.Start, cancellationToken);
            var symbols = await SymbolCompletionItem.GetSymbolsAsync(completionItem, document, cancellationToken).ConfigureAwait(false);
            var overriddenMember = symbols.First();

            // CodeGenerationOptions containing before and after
            var options = new CodeGenerationOptions(contextLocation: semanticModel.SyntaxTree.GetLocation(TextSpan.FromBounds(line.Start, line.Start)));

            var generatedMember = await GenerateMemberAsync(overriddenMember, containingType, document, completionItem, cancellationToken).ConfigureAwait(false);
            generatedMember = _annotation.AddAnnotationToSymbol(generatedMember);

            Document memberContainingDocument = null;
            if (generatedMember.Kind == SymbolKind.Method)
            {
                memberContainingDocument = codeGenService.AddMethodAsync(document.Project.Solution, containingType, (IMethodSymbol)generatedMember, options, cancellationToken).WaitAndGetResult(cancellationToken);
            }
            else if (generatedMember.Kind == SymbolKind.Property)
            {
                memberContainingDocument = codeGenService.AddPropertyAsync(document.Project.Solution, containingType, (IPropertySymbol)generatedMember, options, cancellationToken).WaitAndGetResult(cancellationToken);
            }
            else if (generatedMember.Kind == SymbolKind.Event)
            {
                memberContainingDocument = codeGenService.AddEventAsync(document.Project.Solution, containingType, (IEventSymbol)generatedMember, options, cancellationToken).WaitAndGetResult(cancellationToken);
            }

            return memberContainingDocument;
        }

        private ISymbol GetResolvedSymbol(SymbolKeyResolution resolution, TextSpan span)
        {
            if (resolution.CandidateReason == CandidateReason.Ambiguous)
            {
                // In order to produce to correct undo stack, completion lets the commit
                // character enter the buffer. That means we can get ambiguity.
                // partial class C { partial void foo() }
                // partial class C { partial foo($$
                // Committing with the open paren will create a second, ambiguous foo.
                // We'll try to prefer the symbol whose declaration doesn't intersect our position
                var nonIntersectingMember = resolution.CandidateSymbols.First(s => s.DeclaringSyntaxReferences.Any(d => !d.Span.IntersectsWith(span)));
                if (nonIntersectingMember != null)
                {
                    return nonIntersectingMember;
                }

                // The user has ambiguous definitions, just take the first one.
                return resolution.CandidateSymbols.First();
            }

            return resolution.Symbol;
        }

        private TextSpan ComputeDestinationSpan(SyntaxNode insertionRoot, string insertionText)
        {
            var targetToken = insertionRoot.GetAnnotatedTokens(_otherAnnotation).FirstOrNullable();
            var text = insertionRoot.GetText();
            var line = text.Lines.GetLineFromPosition(targetToken.Value.Span.End);

            // DevDiv 958235: 
            //
            // void foo()
            // {
            // }
            // override $$
            //
            // If our text edit includes the trailing trivia of the close brace of foo(),
            // that token will be reconstructed. The ensuing tree diff will then count
            // the { } as replaced even though we didn't want it to. If the user
            // has collapsed the outline for foo, that means we'll edit the outlined 
            // region and weird stuff will happen. Therefore, we'll start with the first
            // token on the line in order to leave the token and its trivia alone.
            var firstToken = insertionRoot.FindToken(line.GetFirstNonWhitespacePosition().Value);
            return TextSpan.FromBounds(firstToken.SpanStart, line.End);
        }

        private async Task<string> GenerateInsertionTextAsync(
            Document memberContainingDocument, CancellationToken cancellationToken)
        {
            memberContainingDocument = await Simplifier.ReduceAsync(memberContainingDocument, Simplifier.Annotation, null, cancellationToken).ConfigureAwait(false);
            memberContainingDocument = await Formatter.FormatAsync(memberContainingDocument, Formatter.Annotation, cancellationToken: cancellationToken).ConfigureAwait(false);

            var root = await memberContainingDocument.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var members = root.GetAnnotatedNodesAndTokens(_annotation).AsImmutable().Select(nOrT => nOrT.AsNode().ToString().Trim());

            return string.Join("\r\n", members);
        }

        private async Task<SyntaxNode> PrepareTreeForMemberInsertionAsync(
            Document document, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            var members = root.GetAnnotatedNodesAndTokens(_annotation)
                              .AsImmutable()
                              .Select(m => m.AsNode());

            root = root.RemoveNodes(members, SyntaxRemoveOptions.KeepUnbalancedDirectives);

            var dismemberedDocument = document.WithSyntaxRoot(root);

            dismemberedDocument = await Simplifier.ReduceAsync(dismemberedDocument, Simplifier.Annotation, null, cancellationToken).ConfigureAwait(false);
            dismemberedDocument = await Formatter.FormatAsync(dismemberedDocument, Formatter.Annotation, cancellationToken: cancellationToken).ConfigureAwait(false);
            return await dismemberedDocument.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
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

        internal virtual CompletionItemRules GetRules()
        {
            return s_defaultRules;
        }

        protected override Task<CompletionDescription> GetDescriptionWorkerAsync(Document document, CompletionItem item, CancellationToken cancellationToken)
            => MemberInsertionCompletionItem.GetDescriptionAsync(item, document, cancellationToken);
    }
}
