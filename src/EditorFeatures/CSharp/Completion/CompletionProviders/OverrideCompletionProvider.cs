// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Completion.Providers;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.Completion;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.CSharp.Completion.CompletionProviders
{
    [ExportCompletionProvider("OverrideCompletionProvider", LanguageNames.CSharp)]
    internal partial class OverrideCompletionProvider : AbstractOverrideCompletionProvider, ICustomCommitCompletionProvider
    {
        [ImportingConstructor]
        public OverrideCompletionProvider(
            IWaitIndicator waitIndicator)
            : base(waitIndicator)
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

        protected override TextSpan GetTextChangeSpan(SourceText text, int position)
        {
            return CompletionUtilities.GetTextChangeSpan(text, position);
        }

        public override bool IsTriggerCharacter(SourceText text, int characterPosition, OptionSet options)
        {
            return CompletionUtilities.IsTriggerAfterSpaceOrStartOfWordCharacter(text, characterPosition, options);
        }

        protected override SyntaxToken GetToken(MemberInsertionCompletionItem completionItem, SyntaxTree tree, CancellationToken cancellationToken)
        {
            var token = completionItem.Token;
            return tree.FindTokenOnLeftOfPosition(token.Span.End, cancellationToken);
        }

        public override bool TryDetermineReturnType(SyntaxToken startToken, SemanticModel semanticModel, CancellationToken cancellationToken, out ITypeSymbol returnType, out SyntaxToken nextToken)
        {
            nextToken = startToken;
            returnType = null;
            if (startToken.Parent is TypeSyntax)
            {
                var typeSyntax = (TypeSyntax)startToken.Parent;

                // 'partial' is actually an identifier.  If we see it just bail.  This does mean
                // we won't handle overrides that actually return a type called 'partial'.  And
                // not a single tear was shed.
                if (typeSyntax is IdentifierNameSyntax &&
                    ((IdentifierNameSyntax)typeSyntax).Identifier.IsKindOrHasMatchingText(SyntaxKind.PartialKeyword))
                {
                    return false;
                }

                returnType = semanticModel.GetTypeInfo(typeSyntax, cancellationToken).Type;
                nextToken = typeSyntax.GetFirstToken().GetPreviousToken();
            }

            return true;
        }

        public override bool TryDetermineModifiers(SyntaxToken startToken, SourceText text, int startLine, out Accessibility seenAccessibility,
            out DeclarationModifiers modifiers)
        {
            var token = startToken;
            modifiers = new DeclarationModifiers();
            seenAccessibility = Accessibility.NotApplicable;
            var overrideToken = default(SyntaxToken);
            bool isUnsafe = false;
            bool isSealed = false;
            bool isAbstract = false;

            while (IsOnStartLine(token.SpanStart, text, startLine) && !token.IsKind(SyntaxKind.None))
            {
                switch (token.Kind())
                {
                    case SyntaxKind.UnsafeKeyword:
                        isUnsafe = true;
                        break;
                    case SyntaxKind.OverrideKeyword:
                        overrideToken = token;
                        break;
                    case SyntaxKind.SealedKeyword:
                        isSealed = true;
                        break;
                    case SyntaxKind.AbstractKeyword:
                        isAbstract = true;
                        break;
                    case SyntaxKind.ExternKeyword:
                        break;

                    // Filter on the most recently typed accessibility; keep the first one we see

                    case SyntaxKind.PublicKeyword:
                        if (seenAccessibility == Accessibility.NotApplicable)
                        {
                            seenAccessibility = Accessibility.Public;
                        }

                        break;
                    case SyntaxKind.InternalKeyword:
                        if (seenAccessibility == Accessibility.NotApplicable)
                        {
                            seenAccessibility = Accessibility.Internal;
                        }

                        // If we see internal AND protected, filter for protected internal
                        if (seenAccessibility == Accessibility.Protected)
                        {
                            seenAccessibility = Accessibility.ProtectedOrInternal;
                        }

                        break;
                    case SyntaxKind.ProtectedKeyword:
                        if (seenAccessibility == Accessibility.NotApplicable)
                        {
                            seenAccessibility = Accessibility.Protected;
                        }

                        // If we see protected AND internal, filter for protected internal
                        if (seenAccessibility == Accessibility.Internal)
                        {
                            seenAccessibility = Accessibility.ProtectedOrInternal;
                        }

                        break;
                    default:
                        // Anything else and we bail.
                        return false;
                }

                var previousToken = token.GetPreviousToken();

                // We want only want to consume modifiers
                if (previousToken.IsKind(SyntaxKind.None) || !IsOnStartLine(previousToken.SpanStart, text, startLine))
                {
                    break;
                }

                token = previousToken;
            }

            startToken = token;
            modifiers = new DeclarationModifiers(isUnsafe: isUnsafe, isAbstract: isAbstract, isOverride: true, isSealed: isSealed);
            return overrideToken.IsKind(SyntaxKind.OverrideKeyword) && IsOnStartLine(overrideToken.Parent.SpanStart, text, startLine);
        }

        public override SyntaxToken FindStartingToken(SyntaxTree tree, int position, CancellationToken cancellationToken)
        {
            var token = tree.FindTokenOnLeftOfPosition(position, cancellationToken);
            return token.GetPreviousTokenIfTouchingWord(position);
        }

        public override ISet<ISymbol> FilterOverrides(ISet<ISymbol> members, ITypeSymbol returnType)
        {
            var filteredMembers = new HashSet<ISymbol>(
                from m in members
                where SymbolEquivalenceComparer.Instance.Equals(GetReturnType(m), returnType)
                select m);

            // Don't filter by return type if we would then have nothing to show.
            // This way, the user gets completion even if they speculatively typed the wrong return type
            if (filteredMembers.Count > 0)
            {
                members = filteredMembers;
            }

            return members;
        }

        protected override int GetTargetCaretPosition(SyntaxNode caretTarget)
        {
            // Inserted Event declarations are a single line, so move to the end of the line.
            if (caretTarget is EventFieldDeclarationSyntax)
            {
                return caretTarget.GetLocation().SourceSpan.End;
            }
            else if (caretTarget is MethodDeclarationSyntax)
            {
                var methodDeclaration = (MethodDeclarationSyntax)caretTarget;

                // abstract override blah(); : move to the end of the line
                if (methodDeclaration.Body == null)
                {
                    return methodDeclaration.GetLocation().SourceSpan.End;
                }
                else
                {
                    // move to the end of the last statement in the method
                    var lastStatement = methodDeclaration.Body.Statements.Last();
                    return lastStatement.GetLocation().SourceSpan.End;
                }
            }
            else if (caretTarget is BasePropertyDeclarationSyntax)
            {
                // property: no accessors; move to the end of the declaration
                var propertyDeclaration = (BasePropertyDeclarationSyntax)caretTarget;
                if (propertyDeclaration.AccessorList != null && propertyDeclaration.AccessorList.Accessors.Any())
                {
                    // move to the end of the last statement of the first accessor
                    var firstAccessorStatement = propertyDeclaration.AccessorList.Accessors.First().Body.Statements.Last();
                    return firstAccessorStatement.GetLocation().SourceSpan.End;
                }
                else
                {
                    return propertyDeclaration.GetLocation().SourceSpan.End;
                }
            }
            else
            {
                throw ExceptionUtilities.Unreachable;
            }
        }
    }
}
