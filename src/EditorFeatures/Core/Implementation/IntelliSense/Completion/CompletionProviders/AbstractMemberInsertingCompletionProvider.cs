// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.Completion
{
    internal abstract class AbstractMemberInsertingCompletionProvider : CompletionListProvider, ICustomCommitCompletionProvider
    {
        private IWaitIndicator _waitIndicator;
        private readonly SyntaxAnnotation _annotation = new SyntaxAnnotation();
        private readonly SyntaxAnnotation _otherAnnotation = new SyntaxAnnotation();

        protected abstract SyntaxToken GetToken(MemberInsertionCompletionItem completionItem, SyntaxTree tree, CancellationToken cancellationToken);

        protected abstract Task<ISymbol> GenerateMemberAsync(ISymbol member, INamedTypeSymbol containingType, Document document, MemberInsertionCompletionItem item, CancellationToken cancellationToken);
        protected abstract int GetTargetCaretPosition(SyntaxNode caretTarget);
        protected abstract SyntaxNode GetSyntax(SyntaxToken commonSyntaxToken);

        public AbstractMemberInsertingCompletionProvider(
            IWaitIndicator waitIndicator)
        {
            _waitIndicator = waitIndicator;
        }

        public void Commit(CompletionItem completionItem, ITextView textView, ITextBuffer subjectBuffer, ITextSnapshot triggerSnapshot, char? commitChar)
        {
            var currentSnapshot = subjectBuffer.CurrentSnapshot;

            var waitResult = _waitIndicator.Wait(
                "IntelliSense",
                allowCancel: true,
                action: c =>
            {
                var cancellationToken = c.CancellationToken;
                var newDocument = DetermineNewDocumentAsync((MemberInsertionCompletionItem)completionItem, currentSnapshot, cancellationToken).WaitAndGetResult(cancellationToken);

                // Apply the new document to workspace
                newDocument.Project.Solution.Workspace.ApplyDocumentChanges(newDocument, cancellationToken);

                var newRoot = newDocument.GetSyntaxRootAsync(cancellationToken).WaitAndGetResult(cancellationToken);

                // Attempt to find the inserted node and move the caret appropriately
                if (newRoot != null)
                {
                    var caretTarget = newRoot.GetAnnotatedNodesAndTokens(_annotation).FirstOrNullable();
                    if (caretTarget != null)
                    {
                        var targetPosition = GetTargetCaretPosition(caretTarget.Value.AsNode());

                        // Something weird happened and we failed to get a valid position.
                        // Bail on moving the caret.
                        if (targetPosition < 0 || targetPosition > subjectBuffer.CurrentSnapshot.Length)
                        {
                            return;
                        }

                        var target = new SnapshotPoint(subjectBuffer.CurrentSnapshot, targetPosition);

                        textView.TryMoveCaretToAndEnsureVisible(target);
                    }
                }
            });
        }

        private async Task<Document> DetermineNewDocumentAsync(MemberInsertionCompletionItem completionItem, ITextSnapshot textSnapshot, CancellationToken cancellationToken)
        {
            // The span we're going to replace
            var line = textSnapshot.GetLineFromLineNumber(completionItem.Line);

            var sourceText = textSnapshot.AsText();
            var document = sourceText.GetOpenDocumentInCurrentContextWithChanges();
            Contract.ThrowIfNull(document);

            // Annotate the line we care about so we can find it after adding usings
            var tree = document.GetSyntaxTreeAsync(cancellationToken).WaitAndGetResult(cancellationToken);
            var token = GetToken(completionItem, tree, cancellationToken);
            var annotatedRoot = tree.GetRoot(cancellationToken).ReplaceToken(token, token.WithAdditionalAnnotations(_otherAnnotation));
            document = document.WithSyntaxRoot(annotatedRoot);

            var memberContainingDocument = await GenerateMemberAndUsingsAsync(document, completionItem, line, cancellationToken).ConfigureAwait(false);

            var insertionRoot = PrepareTreeForMemberInsertion(memberContainingDocument, cancellationToken);
            var insertionText = GenerateInsertionText(memberContainingDocument, cancellationToken);

            var destinationSpan = ComputeDestinationSpan(insertionRoot, insertionText);

            var finalText = insertionRoot.GetText(sourceText.Encoding).Replace(destinationSpan, insertionText.Trim());

            document = document.WithText(finalText);
            var newRoot = document.GetSyntaxRootAsync(cancellationToken).WaitAndGetResult(cancellationToken);
            var declaration = GetSyntax(newRoot.FindToken(destinationSpan.End));

            document = document.WithSyntaxRoot(newRoot.ReplaceNode(declaration, declaration.WithAdditionalAnnotations(_annotation)));
            return Formatter.FormatAsync(document, _annotation, cancellationToken: cancellationToken).WaitAndGetResult(cancellationToken);
        }

        private async Task<Document> GenerateMemberAndUsingsAsync(
            Document document,
            MemberInsertionCompletionItem completionItem,
            ITextSnapshotLine line,
            CancellationToken cancellationToken)
        {
            var syntaxFactory = document.GetLanguageService<SyntaxGenerator>();
            var codeGenService = document.GetLanguageService<ICodeGenerationService>();

            // Resolve member and type in our new, forked, solution
            var semanticModel = document.GetSemanticModelAsync(cancellationToken).WaitAndGetResult(cancellationToken);
            var containingType = semanticModel.GetEnclosingSymbol<INamedTypeSymbol>(line.Start, cancellationToken);
            var resolution = completionItem.SymbolId.Resolve(semanticModel.Compilation, cancellationToken: cancellationToken);
            var overriddenMember = GetResolvedSymbol(resolution, line.Extent.Span.ToTextSpan());

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

        private string GenerateInsertionText(Document memberContainingDocument, CancellationToken cancellationToken)
        {
            memberContainingDocument = Simplifier.ReduceAsync(memberContainingDocument, Simplifier.Annotation, null, cancellationToken).WaitAndGetResult(cancellationToken);
            memberContainingDocument = Formatter.FormatAsync(memberContainingDocument, Formatter.Annotation, cancellationToken: cancellationToken).WaitAndGetResult(cancellationToken);

            var root = memberContainingDocument.GetSyntaxRootAsync(cancellationToken).WaitAndGetResult(cancellationToken);
            var members = root.GetAnnotatedNodesAndTokens(_annotation).AsImmutable().Select(nOrT => nOrT.AsNode().ToString().Trim());

            return string.Join("\r\n", members);
        }

        private SyntaxNode PrepareTreeForMemberInsertion(Document document, CancellationToken cancellationToken)
        {
            var root = document.GetSyntaxRootAsync(cancellationToken).WaitAndGetResult(cancellationToken);

            var members = root.GetAnnotatedNodesAndTokens(_annotation)
                              .AsImmutable()
                              .Select(m => m.AsNode());

            root = root.RemoveNodes(members, SyntaxRemoveOptions.KeepUnbalancedDirectives);

            var dismemberedDocument = document.WithSyntaxRoot(root);

            dismemberedDocument = Simplifier.ReduceAsync(dismemberedDocument, Simplifier.Annotation, null, cancellationToken).WaitAndGetResult(cancellationToken);
            dismemberedDocument = Formatter.FormatAsync(dismemberedDocument, Formatter.Annotation, cancellationToken: cancellationToken).WaitAndGetResult(cancellationToken);
            return dismemberedDocument.GetSyntaxRootAsync(cancellationToken).WaitAndGetResult(cancellationToken);
        }
    }
}
