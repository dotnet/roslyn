// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Completion.Providers
{
    internal partial class PartialMethodCompletionProvider : AbstractPartialMethodCompletionProvider
    {
        public PartialMethodCompletionProvider()
        {
        }

        protected override SyntaxNode GetSyntax(SyntaxToken token)
        {
            return token.GetAncestor<EventFieldDeclarationSyntax>()
                ?? token.GetAncestor<EventDeclarationSyntax>()
                ?? token.GetAncestor<PropertyDeclarationSyntax>()
                ?? token.GetAncestor<IndexerDeclarationSyntax>()
                ?? (SyntaxNode)token.GetAncestor<MethodDeclarationSyntax>();
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

        internal override bool IsInsertionTrigger(SourceText text, int characterPosition, OptionSet options)
        {
            var ch = text[characterPosition];
            return ch == ' ' || (CompletionUtilities.IsStartingNewWord(text, characterPosition) && options.GetOption(CompletionOptions.TriggerOnTypingLetters, LanguageNames.CSharp));
        }

        protected override bool IsPartial(IMethodSymbol method)
        {
            if (method.DeclaredAccessibility != Accessibility.NotApplicable &&
                method.DeclaredAccessibility != Accessibility.Private)
            {
                return false;
            }

            if (!method.ReturnsVoid)
            {
                return false;
            }

            if (method.IsVirtual)
            {
                return false;
            }

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

            if (targetToken.IsKind(SyntaxKind.VoidKeyword, SyntaxKind.PartialKeyword) ||
                (targetToken.Kind() == SyntaxKind.IdentifierToken && targetToken.HasMatchingText(SyntaxKind.PartialKeyword)))
            {
                return !IsOnSameLine(touchingToken.GetNextToken(), touchingToken, text) &&
                    VerifyModifiers(tree, position, cancellationToken, out modifiers);
            }

            return false;
        }

        private bool VerifyModifiers(SyntaxTree tree, int position, CancellationToken cancellationToken, out DeclarationModifiers modifiers)
        {
            var touchingToken = tree.FindTokenOnLeftOfPosition(position, cancellationToken);
            var token = touchingToken.GetPreviousToken();

            var foundPartial = touchingToken.IsKindOrHasMatchingText(SyntaxKind.PartialKeyword);
            var foundAsync = false;

            while (IsOnSameLine(token, touchingToken, tree.GetText(cancellationToken)))
            {
                if (token.IsKind(SyntaxKind.ExternKeyword, SyntaxKind.PublicKeyword, SyntaxKind.ProtectedKeyword, SyntaxKind.InternalKeyword))
                {
                    modifiers = default;
                    return false;
                }

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

        private bool IsOnSameLine(SyntaxToken syntaxToken, SyntaxToken touchingToken, SourceText text)
        {
            return !syntaxToken.IsKind(SyntaxKind.None)
                && !touchingToken.IsKind(SyntaxKind.None)
                && text.Lines.IndexOf(syntaxToken.SpanStart) == text.Lines.IndexOf(touchingToken.SpanStart);
        }

        protected override string GetDisplayText(IMethodSymbol method, SemanticModel semanticModel, int position)
        {
            return method.ToMinimalDisplayString(semanticModel, position, SignatureDisplayFormat);
        }
    }
}
