// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Completion.Providers
{
    [ExportCompletionProvider(nameof(PartialMethodCompletionProvider), LanguageNames.CSharp)]
    [ExtensionOrder(After = nameof(OverrideCompletionProvider))]
    [Shared]
    internal partial class PartialMethodCompletionProvider : AbstractPartialMethodCompletionProvider
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public PartialMethodCompletionProvider()
        {
        }

        internal override string Language => LanguageNames.CSharp;

        protected override bool IncludeAccessibility(IMethodSymbol method, CancellationToken cancellationToken)
        {
            var declaration = (MethodDeclarationSyntax)method.DeclaringSyntaxReferences[0].GetSyntax(cancellationToken);
            foreach (var mod in declaration.Modifiers)
            {
                switch (mod.Kind())
                {
                    case SyntaxKind.PublicKeyword:
                    case SyntaxKind.ProtectedKeyword:
                    case SyntaxKind.InternalKeyword:
                    case SyntaxKind.PrivateKeyword:
                        return true;
                }
            }

            return false;
        }

        protected override SyntaxNode GetSyntax(SyntaxToken token)
        {
            return token.GetAncestor<EventFieldDeclarationSyntax>()
                ?? token.GetAncestor<EventDeclarationSyntax>()
                ?? token.GetAncestor<PropertyDeclarationSyntax>()
                ?? token.GetAncestor<IndexerDeclarationSyntax>()
                ?? (SyntaxNode?)token.GetAncestor<MethodDeclarationSyntax>()
                ?? throw ExceptionUtilities.UnexpectedValue(token);
        }

        protected override int GetTargetCaretPosition(SyntaxNode caretTarget)
        {
            var methodDeclaration = (MethodDeclarationSyntax)caretTarget;
            return CompletionUtilities.GetTargetCaretPositionForMethod(methodDeclaration);
        }

        protected override SyntaxToken GetToken(CompletionItem completionItem, SyntaxTree tree, CancellationToken cancellationToken)
        {
            var tokenSpanEnd = MemberInsertionCompletionItem.GetTokenSpanEnd(completionItem);
            return tree.FindTokenOnLeftOfPosition(tokenSpanEnd, cancellationToken);
        }

        public override bool IsInsertionTrigger(SourceText text, int characterPosition, CompletionOptions options)
            => text[characterPosition] == ' ' ||
               options.TriggerOnTypingLetters && CompletionUtilities.IsStartingNewWord(text, characterPosition);

        public override ImmutableHashSet<char> TriggerCharacters { get; } = CompletionUtilities.SpaceTriggerCharacter;

        protected override bool IsPartial(IMethodSymbol method)
        {
            var declarations = method.DeclaringSyntaxReferences.Select(r => r.GetSyntax()).OfType<MethodDeclarationSyntax>();
            return declarations.Any(d => d.Body == null && d.Modifiers.Any(SyntaxKind.PartialKeyword));
        }

        protected override bool IsPartialMethodCompletionContext(SyntaxTree tree, int position, CancellationToken cancellationToken, out DeclarationModifiers modifiers, out SyntaxToken token)
        {
            var touchingToken = tree.FindTokenOnLeftOfPosition(position, cancellationToken);
            var targetToken = touchingToken.GetPreviousTokenIfTouchingWord(position);
            var text = tree.GetText(cancellationToken);

            token = targetToken;

            modifiers = default;

            if (targetToken.Kind() is SyntaxKind.VoidKeyword or SyntaxKind.PartialKeyword ||
                (targetToken.Kind() == SyntaxKind.IdentifierToken && targetToken.HasMatchingText(SyntaxKind.PartialKeyword)))
            {
                return !IsOnSameLine(touchingToken.GetNextToken(), touchingToken, text) &&
                    VerifyModifiers(tree, position, cancellationToken, out modifiers);
            }

            return false;
        }

        private static bool VerifyModifiers(SyntaxTree tree, int position, CancellationToken cancellationToken, out DeclarationModifiers modifiers)
        {
            var touchingToken = tree.FindTokenOnLeftOfPosition(position, cancellationToken);
            var token = touchingToken.GetPreviousToken();

            var foundPartial = touchingToken.IsKindOrHasMatchingText(SyntaxKind.PartialKeyword);
            var foundAsync = false;

            while (IsOnSameLine(token, touchingToken, tree.GetText(cancellationToken)))
            {
                if (token.IsKindOrHasMatchingText(SyntaxKind.AsyncKeyword))
                {
                    foundAsync = true;
                }

                foundPartial = foundPartial || token.IsKindOrHasMatchingText(SyntaxKind.PartialKeyword);

                token = token.GetPreviousToken();
            }

            modifiers = new DeclarationModifiers(isAsync: foundAsync, isPartial: true);

            return foundPartial;
        }

        private static bool IsOnSameLine(SyntaxToken syntaxToken, SyntaxToken touchingToken, SourceText text)
        {
            return !syntaxToken.IsKind(SyntaxKind.None)
                && !touchingToken.IsKind(SyntaxKind.None)
                && text.Lines.IndexOf(syntaxToken.SpanStart) == text.Lines.IndexOf(touchingToken.SpanStart);
        }

        protected override string GetDisplayText(IMethodSymbol method, SemanticModel semanticModel, int position)
            => method.ToMinimalDisplayString(semanticModel, position, SignatureDisplayFormat);
    }
}
