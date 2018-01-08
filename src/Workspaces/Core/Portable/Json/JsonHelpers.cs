// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.VirtualChars;

namespace Microsoft.CodeAnalysis.Json
{
    internal static class JsonHelpers
    {
        public static TextSpan GetSpan(JsonToken token)
            => GetSpan(token.VirtualChars);

        public static TextSpan GetSpan(JsonToken token1, JsonToken token2)
            => GetSpan(token1.VirtualChars[0], token2.VirtualChars.Last());

        public static TextSpan GetSpan(ImmutableArray<VirtualChar> virtualChars)
            => GetSpan(virtualChars[0], virtualChars.Last());

        public static TextSpan GetSpan(VirtualChar firstChar, VirtualChar lastChar)
            => TextSpan.FromBounds(firstChar.Span.Start, lastChar.Span.End);

        public static TextSpan GetSpan(JsonNode node)
        {
            var start = int.MaxValue;
            var end = 0;

            GetSpan(node, ref start, ref end);

            return TextSpan.FromBounds(start, end);
        }

        private static void GetSpan(JsonNode node, ref int start, ref int end)
        {
            foreach (var child in node)
            {
                if (child.IsNode)
                {
                    GetSpan(child.Node, ref start, ref end);
                }
                else
                {
                    var token = child.Token;
                    if (!token.IsMissing)
                    {
                        start = Math.Min(token.VirtualChars[0].Span.Start, start);
                        end = Math.Max(token.VirtualChars.Last().Span.End, end);
                    }
                }
            }
        }

        public static bool Contains(JsonNode node, VirtualChar virtualChar)
        {
            foreach (var child in node)
            {
                if (child.IsNode)
                {
                    if (Contains(child.Node, virtualChar))
                    {
                        return true;
                    }
                }
                else
                {
                    if (child.Token.VirtualChars.Contains(virtualChar))
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
