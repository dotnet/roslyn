// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Microsoft.CodeAnalysis.CSharp.OrderModifiers
{
    internal static class OrderModifiersHelper
    {
        private static readonly char[] s_comma = { ',' };

        /// <remarks>
        /// Reference type so we can read/write atomically.
        /// </remarks>
        private static Tuple<string, Dictionary<int, int>> s_lastParsed;

        public static bool TryGetOrComputePreferredOrder(string value, out Dictionary<int, int> preferredOrder)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                preferredOrder = null;
                return false;
            }

            var lastParsed = Volatile.Read(ref s_lastParsed);
            if (lastParsed?.Item1 != value)
            {
                if (!TryParse(value, out var parsed))
                {
                    preferredOrder = null;
                    return false;
                }

                lastParsed = Tuple.Create(value, parsed);
                Volatile.Write(ref s_lastParsed, lastParsed);
            }

            preferredOrder = lastParsed.Item2;
            return true;
        }

        private static bool TryParse(string value, out Dictionary<int, int> parsed)
        {
            var result = new Dictionary<int, int>();

            var index = 0;
            foreach (var piece in value.Split(s_comma, StringSplitOptions.RemoveEmptyEntries))
            {
                var trimmed = piece.Trim();
                var kind = SyntaxFacts.GetKeywordKind(trimmed);
                kind = kind == SyntaxKind.None ? SyntaxFacts.GetContextualKeywordKind(trimmed) : kind;

                if (kind == SyntaxKind.None)
                {
                    parsed = null;
                    return false;
                }

                result[(int)kind] = index;
                index++;
            }

            if (result.Count == 0)
            {
                parsed = null;
                return false;
            }

            // 'partial' must always go at the end in C#.
            result[(int)SyntaxKind.PartialKeyword] = int.MaxValue;

            parsed = result;
            return true;
        }
    }
}