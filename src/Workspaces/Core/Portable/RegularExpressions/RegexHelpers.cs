// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis.EmbeddedLanguages.Common;
using Microsoft.CodeAnalysis.EmbeddedLanguages.VirtualChars;

namespace Microsoft.CodeAnalysis.RegularExpressions
{
    internal static class RegexHelpers
    {
        //public static TextSpan GetSpan(RegexToken token)
        //    => GetSpan(token.VirtualChars);

        //public static TextSpan GetSpan(RegexToken token1, RegexToken token2)
        //    => GetSpan(token1.VirtualChars[0], token2.VirtualChars.Last());

        //public static TextSpan GetSpan(ImmutableArray<VirtualChar> virtualChars)
        //    => GetSpan(virtualChars[0], virtualChars.Last());

        //public static TextSpan GetSpan(VirtualChar firstChar, VirtualChar lastChar)
        //    => TextSpan.FromBounds(firstChar.Span.Start, lastChar.Span.End);

        public static bool HasOption(RegexOptions options, RegexOptions val)
            => (options & val) != 0;

        public static EmbeddedSyntaxToken<RegexKind> CreateToken(RegexKind kind, ImmutableArray<EmbeddedSyntaxTrivia> leadingTrivia, ImmutableArray<VirtualChar> virtualChars)
            => CreateToken(kind, leadingTrivia, virtualChars, ImmutableArray<EmbeddedDiagnostic>.Empty);

        public static EmbeddedSyntaxToken<RegexKind> CreateToken(
            RegexKind kind, ImmutableArray<EmbeddedSyntaxTrivia> leadingTrivia,
            ImmutableArray<VirtualChar> virtualChars, ImmutableArray<EmbeddedDiagnostic> diagnostics)
            => CreateToken(kind, leadingTrivia, virtualChars, diagnostics, value: null);

        public static EmbeddedSyntaxToken<RegexKind> CreateToken(
            RegexKind kind, ImmutableArray<EmbeddedSyntaxTrivia> leadingTrivia, ImmutableArray<VirtualChar> virtualChars,
            ImmutableArray<EmbeddedDiagnostic> diagnostics, object value)
            => new EmbeddedSyntaxToken<RegexKind>(kind, leadingTrivia, virtualChars, ImmutableArray<EmbeddedSyntaxTrivia>.Empty, diagnostics, value);

        public static EmbeddedSyntaxToken<RegexKind> CreateMissingToken(RegexKind kind)
            => CreateToken(kind, ImmutableArray<EmbeddedSyntaxTrivia>.Empty, ImmutableArray<VirtualChar>.Empty);

        public static EmbeddedSyntaxTrivia CreateTrivia(RegexKind kind, ImmutableArray<VirtualChar> virtualChars)
            => CreateTrivia(kind, virtualChars, ImmutableArray<EmbeddedDiagnostic>.Empty);

        public static EmbeddedSyntaxTrivia CreateTrivia(RegexKind kind, ImmutableArray<VirtualChar> virtualChars, ImmutableArray<EmbeddedDiagnostic> diagnostics)
            => new EmbeddedSyntaxTrivia((int)kind, virtualChars, diagnostics);

        //public static TextSpan GetSpan(RegexEscapeNode node)
        //{
        //    var start = int.MaxValue;
        //    var end = 0;

        //    GetSpan(node, ref start, ref end);

        //    return TextSpan.FromBounds(start, end);
        //}

        //private static void GetSpan(RegexNode node, ref int start, ref int end)
        //{
        //    foreach (var child in node)
        //    {
        //        if (child.IsNode)
        //        {
        //            GetSpan(child.Node, ref start, ref end);
        //        }
        //        else
        //        {
        //            var token = child.Token;
        //            if (!token.IsMissing)
        //            {
        //                start = Math.Min(token.VirtualChars[0].Span.Start, start);
        //                end = Math.Max(token.VirtualChars.Last().Span.End, end);
        //            }
        //        }
        //    }
        //}

        //public static bool Contains(RegexNode node, VirtualChar virtualChar)
        //{
        //    foreach (var child in node)
        //    {
        //        if (child.IsNode)
        //        {
        //            if (Contains(child.Node, virtualChar))
        //            {
        //                return true;
        //            }
        //        }
        //        else
        //        {
        //            if (child.Token.VirtualChars.Contains(virtualChar))
        //            {
        //                return true;
        //            }
        //        }
        //    }

        //    return false;
        //}
    }
}
