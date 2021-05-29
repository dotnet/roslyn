﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Completion.Providers
{
    [ExportCompletionProvider(nameof(OverrideCompletionProvider), LanguageNames.CSharp)]
    [ExtensionOrder(After = nameof(PreprocessorCompletionProvider))]
    [Shared]
    internal partial class OverrideCompletionProvider : AbstractOverrideCompletionProvider
    {
        [ImportingConstructor]
        [SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
        public OverrideCompletionProvider()
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

        public override bool IsInsertionTrigger(SourceText text, int characterPosition, OptionSet options)
            => CompletionUtilities.IsTriggerAfterSpaceOrStartOfWordCharacter(text, characterPosition, options);

        public override ImmutableHashSet<char> TriggerCharacters { get; } = CompletionUtilities.SpaceTriggerCharacter;

        protected override SyntaxToken GetToken(CompletionItem completionItem, SyntaxTree tree, CancellationToken cancellationToken)
        {
            var tokenSpanEnd = MemberInsertionCompletionItem.GetTokenSpanEnd(completionItem);
            return tree.FindTokenOnLeftOfPosition(tokenSpanEnd, cancellationToken);
        }

        public override bool TryDetermineReturnType(SyntaxToken startToken, SemanticModel semanticModel, CancellationToken cancellationToken, out ITypeSymbol returnType, out SyntaxToken nextToken)
        {
            nextToken = startToken;
            returnType = null;
            if (startToken.Parent is TypeSyntax typeSyntax)
            {
                // 'partial' is actually an identifier.  If we see it just bail.  This does mean
                // we won't handle overrides that actually return a type called 'partial'.  And
                // not a single tear was shed.
                if (typeSyntax is IdentifierNameSyntax identifierName &&
                    identifierName.Identifier.IsKindOrHasMatchingText(SyntaxKind.PartialKeyword))
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
            var isUnsafe = false;
            var isSealed = false;
            var isAbstract = false;

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
                    case SyntaxKind.PrivateKeyword:
                        if (seenAccessibility == Accessibility.NotApplicable)
                        {
                            seenAccessibility = Accessibility.Private;
                        }

                        // If we see private AND protected, filter for private protected
                        else if (seenAccessibility == Accessibility.Protected)
                        {
                            seenAccessibility = Accessibility.ProtectedAndInternal;
                        }

                        break;
                    case SyntaxKind.InternalKeyword:
                        if (seenAccessibility == Accessibility.NotApplicable)
                        {
                            seenAccessibility = Accessibility.Internal;
                        }

                        // If we see internal AND protected, filter for protected internal
                        else if (seenAccessibility == Accessibility.Protected)
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
                        else if (seenAccessibility == Accessibility.Internal)
                        {
                            seenAccessibility = Accessibility.ProtectedOrInternal;
                        }

                        // If we see private AND protected, filter for private protected
                        else if (seenAccessibility == Accessibility.Private)
                        {
                            seenAccessibility = Accessibility.ProtectedAndInternal;
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

            modifiers = new DeclarationModifiers(isUnsafe: isUnsafe, isAbstract: isAbstract, isOverride: true, isSealed: isSealed);
            return overrideToken.IsKind(SyntaxKind.OverrideKeyword) && IsOnStartLine(overrideToken.Parent.SpanStart, text, startLine);
        }

        public override SyntaxToken FindStartingToken(SyntaxTree tree, int position, CancellationToken cancellationToken)
        {
            var token = tree.FindTokenOnLeftOfPosition(position, cancellationToken);
            return token.GetPreviousTokenIfTouchingWord(position);
        }

        public override ImmutableArray<ISymbol> FilterOverrides(ImmutableArray<ISymbol> members, ITypeSymbol returnType)
        {
            var filteredMembers = members.WhereAsArray(m =>
                SymbolEquivalenceComparer.Instance.Equals(GetReturnType(m), returnType));

            // Don't filter by return type if we would then have nothing to show.
            // This way, the user gets completion even if they speculatively typed the wrong return type
            return filteredMembers.Length > 0 ? filteredMembers : members;
        }

        protected override int GetTargetCaretPosition(SyntaxNode caretTarget)
        {
            // Inserted Event declarations are a single line, so move to the end of the line.
            if (caretTarget is EventFieldDeclarationSyntax)
            {
                return caretTarget.GetLocation().SourceSpan.End;
            }
            else if (caretTarget is MethodDeclarationSyntax methodDeclaration)
            {
                return CompletionUtilities.GetTargetCaretPositionForMethod(methodDeclaration);
            }
            else if (caretTarget is BasePropertyDeclarationSyntax propertyDeclaration)
            {
                // property: no accessors; move to the end of the declaration
                if (propertyDeclaration.AccessorList != null && propertyDeclaration.AccessorList.Accessors.Any())
                {
                    // move to the end of the last statement of the first accessor
                    var firstAccessor = propertyDeclaration.AccessorList.Accessors[0];
                    var firstAccessorStatement = (SyntaxNode)firstAccessor.Body?.Statements.LastOrDefault() ??
                        firstAccessor.ExpressionBody.Expression;
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
