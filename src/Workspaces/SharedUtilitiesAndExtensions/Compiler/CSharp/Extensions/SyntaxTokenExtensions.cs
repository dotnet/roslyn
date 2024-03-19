// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.LanguageService;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Extensions;

internal static partial class SyntaxTokenExtensions
{
    public static void Deconstruct(this SyntaxToken token, out SyntaxKind kind)
        => kind = token.Kind();

    public static bool IsLastTokenOfNode<T>(this SyntaxToken token) where T : SyntaxNode
        => token.IsLastTokenOfNode<T>(out _);

    public static bool IsLastTokenOfNode<T>(this SyntaxToken token, [NotNullWhen(true)] out T? node) where T : SyntaxNode
    {
        var ancestor = token.GetAncestor<T>();
        if (ancestor == null || token != ancestor.GetLastToken(includeZeroWidth: true))
        {
            node = null;
            return false;
        }

        node = ancestor;
        return true;
    }

    public static bool IsKindOrHasMatchingText(this SyntaxToken token, SyntaxKind kind)
        => token.Kind() == kind || token.HasMatchingText(kind);

    public static bool HasMatchingText(this SyntaxToken token, SyntaxKind kind)
        => token.ToString() == SyntaxFacts.GetText(kind);

    public static bool IsOpenBraceOrCommaOfObjectInitializer(this SyntaxToken token)
        => token.Kind() is SyntaxKind.OpenBraceToken or SyntaxKind.CommaToken &&
           token.Parent.IsKind(SyntaxKind.ObjectInitializerExpression);

    public static bool IsOpenBraceOfAccessorList(this SyntaxToken token)
        => token.IsKind(SyntaxKind.OpenBraceToken) && token.Parent.IsKind(SyntaxKind.AccessorList);

    /// <summary>
    /// Returns true if this token is something that looks like a C# keyword. This includes 
    /// actual keywords, contextual keywords, and even 'var' and 'dynamic'
    /// </summary>
    /// <param name="token"></param>
    /// <returns></returns>
    public static bool CouldBeKeyword(this SyntaxToken token)
    {
        if (token.IsKeyword())
        {
            return true;
        }

        if (token.Kind() == SyntaxKind.IdentifierToken)
        {
            var simpleNameText = token.ValueText;
            return simpleNameText == "var" ||
                   simpleNameText == "dynamic" ||
                   SyntaxFacts.GetContextualKeywordKind(simpleNameText) != SyntaxKind.None;
        }

        return false;
    }

    public static bool IsPotentialModifier(this SyntaxToken token, out SyntaxKind modifierKind)
    {
        var tokenKind = token.Kind();
        modifierKind = SyntaxKind.None;

        switch (tokenKind)
        {
            case SyntaxKind.PublicKeyword:
            case SyntaxKind.InternalKeyword:
            case SyntaxKind.ProtectedKeyword:
            case SyntaxKind.PrivateKeyword:
            case SyntaxKind.SealedKeyword:
            case SyntaxKind.AbstractKeyword:
            case SyntaxKind.StaticKeyword:
            case SyntaxKind.VirtualKeyword:
            case SyntaxKind.ExternKeyword:
            case SyntaxKind.NewKeyword:
            case SyntaxKind.OverrideKeyword:
            case SyntaxKind.ReadOnlyKeyword:
            case SyntaxKind.VolatileKeyword:
            case SyntaxKind.UnsafeKeyword:
            case SyntaxKind.AsyncKeyword:
            case SyntaxKind.RefKeyword:
            case SyntaxKind.OutKeyword:
            case SyntaxKind.InKeyword:
            case SyntaxKind.RequiredKeyword:
            case SyntaxKind.FileKeyword:
            case SyntaxKind.PartialKeyword:
                modifierKind = tokenKind;
                return true;
            case SyntaxKind.IdentifierToken:
                if (token.HasMatchingText(SyntaxKind.AsyncKeyword))
                {
                    modifierKind = SyntaxKind.AsyncKeyword;
                }
                if (token.HasMatchingText(SyntaxKind.FileKeyword))
                {
                    modifierKind = SyntaxKind.FileKeyword;
                }
                if (token.HasMatchingText(SyntaxKind.PartialKeyword))
                {
                    modifierKind = SyntaxKind.PartialKeyword;
                }
                return modifierKind != SyntaxKind.None;
            default:
                return false;
        }
    }

    public static bool IsLiteral(this SyntaxToken token)
    {
        switch (token.Kind())
        {
            case SyntaxKind.CharacterLiteralToken:
            case SyntaxKind.FalseKeyword:
            case SyntaxKind.NumericLiteralToken:
            case SyntaxKind.StringLiteralToken:
            case SyntaxKind.TrueKeyword:
                return true;

            default:
                return false;
        }
    }

    public static bool IntersectsWith(this SyntaxToken token, int position)
        => token.Span.IntersectsWith(position);

    public static SyntaxToken GetPreviousTokenIfTouchingWord(this SyntaxToken token, int position)
    {
        return token.IntersectsWith(position) && IsWord(token)
            ? token.GetPreviousToken(includeSkipped: true)
            : token;
    }

    private static bool IsWord(SyntaxToken token)
        => CSharpSyntaxFacts.Instance.IsWord(token);

    public static SyntaxToken GetNextNonZeroWidthTokenOrEndOfFile(this SyntaxToken token)
        => token.GetNextTokenOrEndOfFile();

    /// <summary>
    /// Determines whether the given SyntaxToken is the first token on a line in the specified SourceText.
    /// </summary>
    public static bool IsFirstTokenOnLine(this SyntaxToken token, SourceText text)
    {
        var previousToken = token.GetPreviousToken(includeSkipped: true, includeDirectives: true, includeDocumentationComments: true);
        if (previousToken.Kind() == SyntaxKind.None)
        {
            return true;
        }

        var tokenLine = text.Lines.IndexOf(token.SpanStart);
        var previousTokenLine = text.Lines.IndexOf(previousToken.SpanStart);
        return tokenLine > previousTokenLine;
    }

    public static bool SpansPreprocessorDirective(this IEnumerable<SyntaxToken> tokens)
        => CSharpSyntaxFacts.Instance.SpansPreprocessorDirective(tokens);

    /// <summary>
    /// Retrieves all trivia after this token, including it's trailing trivia and
    /// the leading trivia of the next token.
    /// </summary>
    public static IEnumerable<SyntaxTrivia> GetAllTrailingTrivia(this SyntaxToken token)
    {
        foreach (var trivia in token.TrailingTrivia)
        {
            yield return trivia;
        }

        var nextToken = token.GetNextTokenOrEndOfFile(includeZeroWidth: true, includeSkipped: true, includeDirectives: true, includeDocumentationComments: true);

        foreach (var trivia in nextToken.LeadingTrivia)
        {
            yield return trivia;
        }
    }

    public static bool IsRegularStringLiteral(this SyntaxToken token)
        => token.Kind() == SyntaxKind.StringLiteralToken && !token.IsVerbatimStringLiteral();

    public static bool IsValidAttributeTarget(this SyntaxToken token)
    {
        switch (token.Kind())
        {
            case SyntaxKind.AssemblyKeyword:
            case SyntaxKind.ModuleKeyword:
            case SyntaxKind.FieldKeyword:
            case SyntaxKind.EventKeyword:
            case SyntaxKind.MethodKeyword:
            case SyntaxKind.ParamKeyword:
            case SyntaxKind.PropertyKeyword:
            case SyntaxKind.ReturnKeyword:
            case SyntaxKind.TypeKeyword:
                return true;

            default:
                return false;
        }
    }

    public static SyntaxToken WithCommentsFrom(
        this SyntaxToken token,
        IEnumerable<SyntaxTrivia> leadingTrivia,
        IEnumerable<SyntaxTrivia> trailingTrivia,
        params SyntaxNodeOrToken[] trailingNodesOrTokens)
        => token
            .WithPrependedLeadingTrivia(leadingTrivia)
            .WithTrailingTrivia((
                token.TrailingTrivia.Concat(SyntaxNodeOrTokenExtensions.GetTrivia(trailingNodesOrTokens).Concat(trailingTrivia))).FilterComments(addElasticMarker: false));

    public static SyntaxToken KeepCommentsAndAddElasticMarkers(this SyntaxToken token)
        => token.WithTrailingTrivia(token.TrailingTrivia.FilterComments(addElasticMarker: true))
                .WithLeadingTrivia(token.LeadingTrivia.FilterComments(addElasticMarker: true));

    public static bool TryParseGenericName(this SyntaxToken genericIdentifier, CancellationToken cancellationToken, [NotNullWhen(true)] out GenericNameSyntax? genericName)
    {
        if (genericIdentifier.GetNextToken(includeSkipped: true).Kind() == SyntaxKind.LessThanToken)
        {
            var lastToken = genericIdentifier.FindLastTokenOfPartialGenericName();

            var syntaxTree = genericIdentifier.SyntaxTree!;
            var name = SyntaxFactory.ParseName(syntaxTree.GetText(cancellationToken).ToString(TextSpan.FromBounds(genericIdentifier.SpanStart, lastToken.Span.End)));

            genericName = name as GenericNameSyntax;
            return genericName != null;
        }

        genericName = null;
        return false;
    }

    /// <summary>
    /// Lexically, find the last token that looks like it's part of this generic name.
    /// </summary>
    /// <param name="genericIdentifier">The "name" of the generic identifier, last token before
    /// the "&amp;"</param>
    /// <returns>The last token in the name</returns>
    /// <remarks>This is related to the code in SyntaxTreeExtensions.IsInPartiallyWrittenGeneric</remarks>
    public static SyntaxToken FindLastTokenOfPartialGenericName(this SyntaxToken genericIdentifier)
    {
        Contract.ThrowIfFalse(genericIdentifier.Kind() == SyntaxKind.IdentifierToken);

        // advance to the "<" token
        var token = genericIdentifier.GetNextToken(includeSkipped: true);
        Contract.ThrowIfFalse(token.Kind() == SyntaxKind.LessThanToken);

        var stack = 0;

        do
        {
            // look forward one token
            {
                var next = token.GetNextToken(includeSkipped: true);
                if (next.Kind() == SyntaxKind.None)
                {
                    return token;
                }

                token = next;
            }

            if (token.Kind() == SyntaxKind.GreaterThanToken)
            {
                if (stack == 0)
                {
                    return token;
                }
                else
                {
                    stack--;
                    continue;
                }
            }

            switch (token.Kind())
            {
                case SyntaxKind.LessThanLessThanToken:
                    stack++;
                    goto case SyntaxKind.LessThanToken;

                // fall through
                case SyntaxKind.LessThanToken:
                    stack++;
                    break;

                case SyntaxKind.AsteriskToken:      // for int*
                case SyntaxKind.QuestionToken:      // for int?
                case SyntaxKind.ColonToken:         // for global::  (so we don't dismiss help as you type the first :)
                case SyntaxKind.ColonColonToken:    // for global::
                case SyntaxKind.CloseBracketToken:
                case SyntaxKind.OpenBracketToken:
                case SyntaxKind.DotToken:
                case SyntaxKind.IdentifierToken:
                case SyntaxKind.CommaToken:
                    break;

                // If we see a member declaration keyword, we know we've gone too far
                case SyntaxKind.ClassKeyword:
                case SyntaxKind.StructKeyword:
                case SyntaxKind.InterfaceKeyword:
                case SyntaxKind.DelegateKeyword:
                case SyntaxKind.EnumKeyword:
                case SyntaxKind.PrivateKeyword:
                case SyntaxKind.PublicKeyword:
                case SyntaxKind.InternalKeyword:
                case SyntaxKind.ProtectedKeyword:
                case SyntaxKind.VoidKeyword:
                    return token.GetPreviousToken(includeSkipped: true);

                default:
                    // user might have typed "in" on the way to typing "int"
                    // don't want to disregard this genericname because of that
                    if (SyntaxFacts.IsKeywordKind(token.Kind()))
                    {
                        break;
                    }

                    // anything else and we're sunk. Go back to the token before.
                    return token.GetPreviousToken(includeSkipped: true);
            }
        }
        while (true);
    }
}
