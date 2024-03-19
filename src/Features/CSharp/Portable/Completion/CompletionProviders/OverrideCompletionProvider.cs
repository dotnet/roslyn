// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Completion.Providers;

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

    internal override string Language => LanguageNames.CSharp;

    protected override SyntaxNode GetSyntax(SyntaxToken token)
    {
        return token.GetAncestor<EventFieldDeclarationSyntax>()
            ?? token.GetAncestor<EventDeclarationSyntax>()
            ?? token.GetAncestor<PropertyDeclarationSyntax>()
            ?? token.GetAncestor<IndexerDeclarationSyntax>()
            ?? (SyntaxNode?)token.GetAncestor<MethodDeclarationSyntax>()
            ?? throw ExceptionUtilities.UnexpectedValue(token);
    }

    public override bool IsInsertionTrigger(SourceText text, int characterPosition, CompletionOptions options)
        => CompletionUtilities.IsTriggerAfterSpaceOrStartOfWordCharacter(text, characterPosition, options);

    public override ImmutableHashSet<char> TriggerCharacters { get; } = CompletionUtilities.SpaceTriggerCharacter;

    protected override SyntaxToken GetToken(CompletionItem completionItem, SyntaxTree tree, CancellationToken cancellationToken)
    {
        var tokenSpanEnd = MemberInsertionCompletionItem.GetTokenSpanEnd(completionItem);
        return tree.FindTokenOnLeftOfPosition(tokenSpanEnd, cancellationToken);
    }

    public override bool TryDetermineReturnType(SyntaxToken startToken, SemanticModel semanticModel, CancellationToken cancellationToken, out ITypeSymbol? returnType, out SyntaxToken nextToken)
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

    public override bool TryDetermineModifiers(
        SyntaxToken startToken,
        SourceText text,
        int startLine,
        out Accessibility seenAccessibility,
        out DeclarationModifiers modifiers)
    {
        var token = startToken;
        var parentMember = token.Parent;
        modifiers = default;
        seenAccessibility = Accessibility.NotApplicable;

        if (parentMember is null)
            return false;

        // Keep walking backwards as long as we're still within our parent member.
        while (token != default)
        {
            if (token.SpanStart < parentMember.SpanStart)
            {
                // moved before the start of the member we're in.  If previous member's token is on the same line,
                // we bail out as our replacement will delete the entire line we're on.
                if (IsOnStartLine(token.SpanStart, text, startLine))
                    return false;

                break;
            }

            // Ok to hit a `]` if it's the end of attributes on this member.
            if (token.Kind() == SyntaxKind.CloseBracketToken)
            {
                if (token.Parent is not AttributeListSyntax)
                    return false;

                break;
            }

            // We only accept tokens that precede us on the same line.  Splitting across multiple lines is too niche
            // to want to support, and more likely indicates a case of broken code that the user is in the middle of
            // fixing up.
            if (!IsOnStartLine(token.SpanStart, text, startLine))
                return false;

            switch (token.Kind())
            {
                // Standard modifier cases we accept.

                case SyntaxKind.AbstractKeyword:
                    modifiers = modifiers.WithIsAbstract(true);
                    break;
                case SyntaxKind.ExternKeyword:
                    modifiers = modifiers.WithIsExtern(true);
                    break;
                case SyntaxKind.OverrideKeyword:
                    modifiers = modifiers.WithIsOverride(true);
                    break;
                case SyntaxKind.RequiredKeyword:
                    modifiers = modifiers.WithIsRequired(true);
                    break;
                case SyntaxKind.SealedKeyword:
                    modifiers = modifiers.WithIsSealed(true);
                    break;
                case SyntaxKind.UnsafeKeyword:
                    modifiers = modifiers.WithIsUnsafe(true);
                    break;

                // Accessibility modifiers we accept.

                case SyntaxKind.PublicKeyword:
                    seenAccessibility = seenAccessibility == Accessibility.NotApplicable
                        ? Accessibility.Public
                        : seenAccessibility;
                    break;
                case SyntaxKind.PrivateKeyword:
                    seenAccessibility = seenAccessibility switch
                    {
                        Accessibility.NotApplicable => Accessibility.Private,
                        // If we see private AND protected, filter for private protected
                        Accessibility.Protected => Accessibility.ProtectedAndInternal,
                        _ => seenAccessibility,
                    };
                    break;
                case SyntaxKind.InternalKeyword:
                    // If we see internal AND protected, filter for protected internal
                    seenAccessibility = seenAccessibility switch
                    {
                        Accessibility.NotApplicable => Accessibility.Internal,
                        Accessibility.Protected => Accessibility.ProtectedOrInternal,
                        _ => seenAccessibility,
                    };
                    break;
                case SyntaxKind.ProtectedKeyword:
                    seenAccessibility = seenAccessibility switch
                    {
                        Accessibility.NotApplicable => Accessibility.Protected,
                        // If we see protected AND internal, filter for protected internal.
                        Accessibility.Internal => Accessibility.ProtectedOrInternal,
                        // Or if we see private AND protected, filter for private protected
                        Accessibility.Private => Accessibility.ProtectedAndInternal,
                        _ => seenAccessibility,
                    };
                    break;
                default:
                    // If we hit anything else, then this token is not valid for override completions, and we can just bail here.
                    return false;
            }

            token = token.GetPreviousToken();
        }

        // Have to at least found the override token for us to offer override-completion.
        return modifiers.IsOverride;
    }

    public override SyntaxToken FindStartingToken(SyntaxTree tree, int position, CancellationToken cancellationToken)
    {
        var token = tree.FindTokenOnLeftOfPosition(position, cancellationToken);
        return token.GetPreviousTokenIfTouchingWord(position);
    }

    public override ImmutableArray<ISymbol> FilterOverrides(ImmutableArray<ISymbol> members, ITypeSymbol? returnType)
    {
        if (returnType == null)
        {
            return members;
        }

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
                var firstAccessorStatement = (SyntaxNode?)firstAccessor.Body?.Statements.LastOrDefault() ??
                    firstAccessor.ExpressionBody!.Expression;
                return firstAccessorStatement.GetLocation().SourceSpan.End;
            }
            else
            {
                return propertyDeclaration.GetLocation().SourceSpan.End;
            }
        }
        else
        {
            throw ExceptionUtilities.Unreachable();
        }
    }
}
